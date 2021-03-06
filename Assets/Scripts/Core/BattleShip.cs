﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    public sealed class BattleShip : IUnit, ISinglePlayerModifierHolder
    {
        private static readonly HashSet<TriggerEventType> RelativeTriggerEventTypes = new HashSet<TriggerEventType>
        {
            // BeforeDestroyed(this, adderObject)
            TriggerEventType.BeforeDestroyed,

            // BeforeDamaged(this, adderObject, damageInfo)
            TriggerEventType.BeforeDamaged,

            // AfterDamaged(this, adderObject, damageInfo)
            TriggerEventType.AfterDamaged,

            // BeforeMeleeAttack(this, adderObject, attackTarget)
            TriggerEventType.BeforeMeleeAttack,

            // AfterMeleeAttack(this, adderObject, damageInfo, attackTarget)
            TriggerEventType.AfterMeleeAttack,
        };

        private readonly Dictionary<TriggerEventType, Dictionary<string, TriggerEvent>> _triggerEvents =
            new Dictionary<TriggerEventType, Dictionary<string, TriggerEvent>>();

        public BattleShip(BattleShipPrototype prototype, IPlayer player, HexTile initialTile)
        {
            using var _ = Game.Instance.GetCacheLock();

            Game.Instance.RegisterInfinityObject(this, true);

            CurrentTile = initialTile;
            CurrentTile.AddTileObject(this);

            IdentifierName = prototype.IdentifierName;
            Properties = prototype.Properties;
            AttackDamageType = prototype.AttackDamageType;
            BaseAttackPower = prototype.BaseAttackPower;
            BaseMaxHp = prototype.BaseMaxHp;
            BaseMaxMovePoint = prototype.BaseMaxMovePoint;
            BaseResourceStorage = prototype.BaseResourceStorage;

            OwnPlayer = player;

            foreach (var s in prototype.BasicSpecialActions)
                AddSpecialAction(s);

            foreach (var m in prototype.BasicModifiers)
                AddModifier(m, Id, -1, true, false);
        }

        public BattleShip(InfinityObjectData data, HexTile initialTile)
        {
            var dict = data.Dict;

            Game.Instance.RegisterInfinityObject(this, true, dict.GetInt("Id"));

            CurrentTile = initialTile;
            CurrentTile.AddTileObject(this);

            IdentifierName = dict.GetString("IdentifierName");
            CustomName = dict.GetString("CustomName");

            foreach (var kv in dict.GetDict("Storage"))
                Storage.Set(kv.Key, kv.Value);

            OwnPlayer = Game.Instance.GetPlayer(dict.GetString("OwnPlayer"));

            _remainHp = dict.GetInt("RemainHp");
            RemainMovePoint = dict.GetInt("RemainMovePoint");
            foreach (var kv in dict.GetDict("RemainResourceStorage"))
                _remainResourceStorage[kv.Key] = Convert.ToInt32(kv.Value);

            var prototype = BattleShipData.Instance.GetPrototype(IdentifierName);

            Properties = prototype.Properties;
            AttackDamageType = prototype.AttackDamageType;
            BaseAttackPower = prototype.BaseAttackPower;
            BaseMaxHp = prototype.BaseMaxHp;
            BaseMaxMovePoint = prototype.BaseMaxMovePoint;
            BaseResourceStorage = prototype.BaseResourceStorage;

            foreach (var s in prototype.BasicSpecialActions)
                AddSpecialAction(s);

            foreach (var m in prototype.BasicModifiers)
                AddModifier(m, Id, -1, true, true);

            foreach (var m in dict.GetDictList("Modifiers"))
                AddModifier(m.GetString("Name"), m.GetInt("AdderObjectId"), m.GetInt("LeftWeek"), false, true);
        }

        public IReadOnlyDictionary<string, int> BaseResourceStorage { get; }

        public string TypeName => nameof(BattleShip);

        public string IdentifierName { get; }

        public int Id { get; set; }

        public IPlayer OwnPlayer { get; }

        [MoonSharpHidden]
        public string CustomName { get; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public bool IsDestroyed { get; private set; }

        public string AttackDamageType { get; }

        public int BaseAttackPower { get; }

        public int BaseMaxHp { get; }

        public int BaseMaxMovePoint { get; }

        public IReadOnlyCollection<string> Properties { get; }

        public void StartNewTurn(int week)
        {
            RemainMovePoint = MaxMovePoint;

            ReduceModifiersLeftWeek(week);
        }

        [MoonSharpHidden]
        public InfinityObjectData Save()
        {
            if (IsDestroyed) return null;

            var result = new Dictionary<string, object>
            {
                ["Id"] = Id,
                ["IdentifierName"] = IdentifierName,
                ["CustomName"] = CustomName,
                ["Storage"] = Storage.Data,
                ["OwnPlayer"] = OwnPlayer.PlayerName,
                ["RemainHp"] = _remainHp,
                ["RemainMovePoint"] = RemainMovePoint,
                ["RemainResourceStorage"] = _remainResourceStorage,
                ["Modifiers"] = _modifiers.Values.Select(x => x.ToSaveData()).ToList(),
            };

            return new InfinityObjectData(TypeName, result);
        }

        public void TeleportToTile(HexTile tile) => TeleportToTile(tile, false);

        public void DestroySelf()
        {
            // BeforeDestroyed(this, adderObject)
            foreach (var e in GetTriggerEvents(TriggerEventType.BeforeDestroyed))
                e.Invoke();

            IsDestroyed = true;
            CurrentTile?.RemoveTileObject(TypeName);
            CurrentTile = null;
        }

        #region ResourceStorage

        private readonly Dictionary<string, int> _remainResourceStorage = new Dictionary<string, int>();

        private readonly Dictionary<string, int> _resourceStorageFromModifier = new Dictionary<string, int>();

        public IReadOnlyDictionary<string, int> RemainResourceStorage => _remainResourceStorage;

        public IReadOnlyDictionary<string, int> ResourceStorageFromModifier => _resourceStorageFromModifier;

        public IReadOnlyDictionary<string, int> MaxResourceStorage
        {
            get
            {
                var result = new Dictionary<string, int>();

                foreach (var kv in BaseResourceStorage)
                {
                    var name = kv.Key;

                    if (!result.ContainsKey(name))
                        result[name] = 0;

                    result[name] += kv.Value;
                }

                foreach (var kv in _resourceStorageFromModifier)
                {
                    var name = kv.Key;

                    if (!result.ContainsKey(name))
                        result[name] = 0;

                    result[name] = Math.Max(0, result[name] + kv.Value);
                }

                return result;
            }
        }

        public int GetMaxStorableAmount(string resourceName)
        {
            var result = 0;

            if (BaseResourceStorage.TryGetValue(resourceName, out var value))
                result += value;

            if (_resourceStorageFromModifier.TryGetValue(resourceName, out value))
                result += value;

            return result;
        }

        public int GetStorableAmount(string resourceName)
        {
            if (!MaxResourceStorage.TryGetValue(resourceName, out var maxValue)) return 0;

            return _remainResourceStorage.TryGetValue(resourceName, out var remainValue)
                ? Math.Max(0, maxValue - remainValue)
                : maxValue;
        }

        public void ChangeResourceAmount(string resourceName, int changeAmount)
        {
            using var _ = Game.Instance.GetCacheLock();

            var currentAmount = _remainResourceStorage.TryGetValue(resourceName, out var value) ? value : 0;

            if (changeAmount <= 0)
            {
                _remainResourceStorage[resourceName] = Math.Max(0, currentAmount + changeAmount);
                return;
            }

            var maxAmount = GetMaxStorableAmount(resourceName);

            if (currentAmount >= maxAmount) return;

            var newAmount = currentAmount + changeAmount;

            _remainResourceStorage[resourceName] = Math.Min(newAmount, maxAmount);
        }

        public void GiveResource(IResourceStorageHolder target, string resourceName, int amount)
        {
            var remainAmount = _remainResourceStorage.TryGetValue(resourceName, out var value) ? value : 0;

            var targetRemainStorage = target.GetStorableAmount(resourceName);

            // Can't give more amount than remaining.
            amount = Math.Min(remainAmount, amount);

            // Can't give more amount than target can hold.
            amount = Math.Min(targetRemainStorage, amount);

            if (amount <= 0) return;

            _remainResourceStorage[resourceName] -= amount;
            target.ChangeResourceAmount(resourceName, amount);
        }

        #endregion

        #region Battle

        private int _remainHp;

        private int _maxHpFromModifier;

        private int _attackPowerFromModifier;

        public int AttackPower => BaseAttackPower + WaitCache(_attackPowerFromModifier);

        public int MaxHp => BaseMaxHp + WaitCache(_maxHpFromModifier);

        public int RemainHp
        {
            get => _remainHp;
            private set
            {
                _remainHp += value;

                if (_remainHp <= 0)
                {
                    _remainHp = 0;
                    DestroySelf();
                    return;
                }

                _remainHp = Math.Min(MaxHp, _remainHp);
            }
        }

        public void ChangeRemainHp(int amount) => RemainHp = amount;

        public void OnDamaged(DamageInfo damageInfo)
        {
            if (IsDestroyed) return;

            // BeforeDamaged(this, adderObject, damageInfo)
            foreach (var e in GetTriggerEvents(TriggerEventType.BeforeDamaged))
                e.Invoke(damageInfo);

            if (damageInfo.IsMelee && damageInfo.Inflicter is IUnit enemy)
            {
                var reAttack = new DamageInfo(this, AttackPower, AttackDamageType, false);

                enemy.OnDamaged(reAttack);
            }

            RemainHp -= damageInfo.Amount;

            if (IsDestroyed) return;

            // AfterDamaged(this, adderObject, damageInfo)
            foreach (var e in GetTriggerEvents(TriggerEventType.AfterDamaged))
                e.Invoke(damageInfo);
        }

        public bool IsMeleeAttackable(IUnit target)
        {
            if (CurrentTile == null || target.CurrentTile == null) return false;

            if (IsDestroyed) return false;

            return CurrentTile.TileMap.Holder.TypeName != nameof(StarSystem) &&
                   CurrentTile.TileMap.Holder.Id == target.CurrentTile.TileMap.Holder.Id &&
                   CurrentTile.Coord.IsAdjacent(target.CurrentTile.Coord, false);
        }

        public void MeleeAttack(IUnit target)
        {
            if (IsDestroyed) return;

            using var _ = Game.Instance.GetCacheLock();

            foreach (var e in GetTriggerEvents(TriggerEventType.BeforeMeleeAttack))
                e.Invoke(target);

            if (IsDestroyed)
                return;

            var damageInfo = new DamageInfo(this, AttackPower, AttackDamageType, true);

            target.OnDamaged(damageInfo);

            if (IsDestroyed)
                return;

            foreach (var e in GetTriggerEvents(TriggerEventType.AfterMeleeAttack))
                e.Invoke();
        }

        #endregion

        #region SpecialAction

        private readonly Dictionary<string, SpecialAction> _specialActions = new Dictionary<string, SpecialAction>();

        public IReadOnlyDictionary<string, SpecialAction> SpecialActions => _specialActions;

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost) =>
            throw new NotImplementedException();

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
            => throw new NotImplementedException();

        private void AddSpecialAction(string name)
        {
            if (_specialActions.ContainsKey(name)) return;

            _specialActions[name] = SpecialActionData.Instance.GetSpecialActionDirectly(this, name);
        }

        #endregion

        #region Modifier

        private readonly Dictionary<string, Modifier> _baseModifiers = new Dictionary<string, Modifier>();

        private readonly Dictionary<string, Modifier> _modifiers = new Dictionary<string, Modifier>();

        [MoonSharpHidden]
        public IEnumerable<Modifier> GetModifiers()
        {
            if (CurrentTile != null)
                foreach (var m in CurrentTile.TileMap.Holder.GetModifiers(OwnPlayer.PlayerName))
                    yield return m;

            foreach (var m in _baseModifiers.Values)
                yield return m;

            foreach (var m in _modifiers.Values)
                yield return m;
        }

        public IEnumerable<TiledModifier> AffectedTiledModifiers =>
            CurrentTile?.TileMap.Holder.GetTiledModifiersForTarget(this) ?? new TiledModifier[0];

        public void AddModifier(string modifierName, IInfinityObject adder, int leftWeek)
            => AddModifier(modifierName, adder?.Id ?? Id, leftWeek, false, false);

        public void RemoveModifier(string modifierName)
        {
            if (!_modifiers.ContainsKey(modifierName))
            {
                Logger.Log(LogType.Warning, $"{nameof(BattleShip)}.{nameof(RemoveModifier)}",
                    $"Trying to remove modifier \"{modifierName}\" which doesn't exist, so it will be ignored.");
                return;
            }

            var m = _modifiers[modifierName];
            _modifiers.Remove(modifierName);
            ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, true, false);
        }

        public bool HasModifier(string modifierName) => _modifiers.ContainsKey(modifierName);

        [MoonSharpHidden]
        public void ApplyModifierChangeToDownward(string targetPlayerName, IModifier m,
            bool isRemoving, bool isFromSaveData)
        {
            if (targetPlayerName.ToLower() != "global" && targetPlayerName != OwnPlayer.PlayerName)
                return;

            using var _ = Game.Instance.GetCacheLock();

            if (isRemoving)
            {
                m.OnRemoved(this);

                RemoveTriggerEvent(m.Name);
            }
            else
            {
                // Should not apply OnAdded effect when loading
                if (!isFromSaveData)
                    m.OnAdded(this);

                RegisterTriggerEvent(m.Name, m.GetTriggerEvent(this));
            }
        }

        private void AddModifier(string modifierName, int adderId, int leftWeek, bool isBase, bool isFromSaveData)
        {
            var dict = isBase ? _baseModifiers : _modifiers;

            if (dict.ContainsKey(modifierName))
            {
                Logger.Log(LogType.Warning, $"{nameof(BattleShip)}.{nameof(AddModifier)}",
                    $"Trying to add modifier \"{modifierName}\" which already exists, so it will be ignored.");
                return;
            }

            var core = ModifierData.Instance.GetModifierDirectly(modifierName);

            if (core.TargetType != TypeName)
            {
                Logger.Log(LogType.Warning, $"{nameof(BattleShip)}.{nameof(AddModifier)}",
                    $"Modifier \"{modifierName}\" is not for {TypeName}, but for {core.TargetType}, so it will be ignored.");
                return;
            }

            if (core.IsTileLimited)
            {
                Logger.Log(LogType.Warning, $"{nameof(BattleShip)}.{nameof(AddModifier)}",
                    $"Modifier \"{modifierName}\" is a tile limited modifier!, but tried to use as a tile unlimited modifier, so it will be ignored.");
                return;
            }

            var m = new Modifier(core, adderId, leftWeek);

            dict.Add(modifierName, m);
            ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, false, isFromSaveData);
        }

        private void ReduceModifiersLeftWeek(int week)
        {
            var toRemoveList = new List<string>();

            foreach (var name in _modifiers.Keys)
            {
                var m = _modifiers[name];
                if (m.IsPermanent) continue;

                if (m.LeftWeek - week <= 0)
                {
                    toRemoveList.Add(name);
                    continue;
                }

                _modifiers[name].ReduceLeftWeek(week);
            }

            foreach (var name in toRemoveList)
                RemoveModifier(name);
        }

        private void RegisterTriggerEvent(string modifierName,
            IReadOnlyDictionary<TriggerEventType, TriggerEvent> events)
        {
            foreach (var kv in events)
            {
                var type = kv.Key;

                if (!RelativeTriggerEventTypes.Contains(type))
                {
                    Logger.Log(LogType.Warning, $"{nameof(BattleShip)}.{nameof(RegisterTriggerEvent)}",
                        $"{kv.Key} is not a valid event name for the {nameof(BattleShip)}, so it will be ignored.");
                    continue;
                }

                if (!_triggerEvents.TryGetValue(type, out var value)) value = new Dictionary<string, TriggerEvent>();

                value[modifierName] = kv.Value;
            }
        }

        private void RemoveTriggerEvent(string modifierName)
        {
            var empty = new List<TriggerEventType>();

            foreach (var kv in _triggerEvents)
            {
                kv.Value.Remove(modifierName);
                if (kv.Value.Count == 0)
                    empty.Add(kv.Key);
            }

            foreach (var t in empty)
                _triggerEvents.Remove(t);
        }

        private IEnumerable<TriggerEvent> GetTriggerEvents(TriggerEventType type)
        {
            if (!_triggerEvents.TryGetValue(type, out var value))
                return new TriggerEvent[0];

            var result = value.Values.ToList();
            result.Sort((x, y) => y.Priority.CompareTo(x.Priority));
            return result;
        }

        #endregion

        #region ModifierEffectCaching

        private Task _cacheTask;

        private bool _cancelCaching;

        private bool _isCachingModifierEffect;

        private readonly Dictionary<string, IReadOnlyList<ModifierEffect>> _modifierEffectsMap =
            new Dictionary<string, IReadOnlyList<ModifierEffect>>();

        public IReadOnlyDictionary<string, IReadOnlyList<ModifierEffect>> ModifierEffectsMap => _modifierEffectsMap;

        [MoonSharpHidden]
        public void StartCachingModifierEffect()
        {
            // When cache request is accepted during cashing, abort ongoing caching immediately and restart.
            if (_isCachingModifierEffect)
            {
                _cancelCaching = true;
                _cacheTask.Wait();
            }

            _isCachingModifierEffect = true;
            _cacheTask = Task.Run(CacheModifierEffect);
        }

        private void CacheModifierEffect()
        {
            _modifierEffectsMap.Clear();

            foreach (var m in GetModifiers().Cast<IModifier>().Concat(AffectedTiledModifiers))
            {
                if (_cancelCaching)
                {
                    _cancelCaching = false;
                    return;
                }

                IReadOnlyList<ModifierEffect> effects = null;

                try
                {
                    effects = m.GetEffects(this);
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Error, $"{m.Name}.{nameof(m.GetEffects)}",
                        $"Error while calculating modifier effect! Error message: {e.Message}");
                }

                if (effects?.Count == 0) continue;

                _modifierEffectsMap[m.Name] = ApplyModifierEffects(effects);
            }

            CacheMovableTileInfo();

            // In case that MaxHp has changed
            RemainHp = RemainHp;
            _isCachingModifierEffect = false;
        }

        private IReadOnlyList<ModifierEffect> ApplyModifierEffects(IEnumerable<ModifierEffect> effects)
        {
            _maxMovePointFromModifier = 0;
            _attackPowerFromModifier = 0;
            _maxHpFromModifier = 0;

            var result = new List<ModifierEffect>();

            foreach (var e in effects)
            {
                var infos = e.AdditionalInfos;
                var amount = e.Amount;
                switch (e.EffectType)
                {
                    // MaxMovePoint
                    case ModifierEffectType.MaxMovePoint:
                    {
                        if (infos.Count != 0)
                            continue;

                        _maxMovePointFromModifier += amount;
                        break;
                    }
                    // AttackPower
                    case ModifierEffectType.AttackPower:
                    {
                        if (infos.Count != 0)
                            continue;

                        _attackPowerFromModifier += amount;
                        break;
                    }
                    // MaxHp
                    case ModifierEffectType.MaxHp:
                    {
                        if (infos.Count != 0)
                            continue;

                        _maxHpFromModifier += amount;
                        break;
                    }
                }

                result.Add(e);
            }

            return result;
        }

        private T WaitCache<T>(in T input)
        {
            while (_isCachingModifierEffect)
            {
                // Busy wait
            }

            return input;
        }

        #endregion

        #region MoveAction

        private Dictionary<HexTileCoord, MoveInfo> _movableTileInfoCache;

        private int _maxMovePointFromModifier;

        public HexTile CurrentTile { get; private set; }

        public int RemainMovePoint { get; private set; }

        public int MaxMovePoint => Math.Max(0, WaitCache(_maxMovePointFromModifier) + BaseMaxMovePoint);

        public IReadOnlyDictionary<HexTileCoord, MoveInfo> MovableTileInfo => WaitCache(_movableTileInfoCache);

        public void ChangeRemainMovePoint(int amount) => RemainMovePoint = Math.Max(0, amount);

        public void Move(HexTileCoord coord)
        {
            var route = new Stack<HexTileCoord>();

            var current = coord;

            var map = MovableTileInfo;

            while (true)
            {
                if (!map.TryGetValue(current, out var info))
                    break;

                route.Push(current);
                current = info.FromCoord;
            }

            CurrentTile.RemoveTileObject(TypeName);

            while (route.Count > 0)
            {
                if (IsDestroyed)
                    return;

                var nextMove = route.Pop();

                var nextTile = CurrentTile.TileMap.GetHexTile(nextMove);

                if (nextTile.UnitMoveCost > RemainMovePoint)
                {
                    // No sufficient move point to move to next tile (possibly due to modifier effects), so end move here.
                    CurrentTile.AddTileObject(this);
                    return;
                }

                RemainMovePoint -= nextTile.UnitMoveCost;
                TeleportToTile(nextTile, true);
            }

            CurrentTile.AddTileObject(this);
        }

        private void CacheMovableTileInfo()
        {
            if (CurrentTile == null)
            {
                _movableTileInfoCache = new Dictionary<HexTileCoord, MoveInfo>();
                return;
            }

            var currentCoord = CurrentTile.Coord;
            var tileMap = CurrentTile.TileMap;

            _movableTileInfoCache = new Dictionary<HexTileCoord, MoveInfo> {{currentCoord, default}};

            var plan = new Queue<HexTileCoord>();
            plan.Enqueue(currentCoord);

            while (plan.Count > 0)
            {
                var cur = plan.Dequeue();

                var costUntilHere = _movableTileInfoCache[cur].CostUntilHere;
                var around = tileMap.GetRing(1, cur);

                foreach (var c in around)
                {
                    if (_movableTileInfoCache.ContainsKey(c))
                        continue;

                    var tile = tileMap.GetHexTile(c);

                    var newCost = costUntilHere + tile.UnitMoveCost;

                    // Can't go any further due to the lack of the move point
                    if (newCost > RemainMovePoint)
                        continue;

                    plan.Enqueue(c);
                    _movableTileInfoCache[tile.Coord] = new MoveInfo(c, newCost);
                }
            }

            _movableTileInfoCache.Remove(currentCoord);
        }

        private void TeleportToTile(HexTile tile, bool withoutTileMapAction)
        {
            using var _ = Game.Instance.GetCacheLock();

            var destHolder = tile.TileMap.Holder;
            var curHolder = CurrentTile.TileMap.Holder;

            var toRemove = new HashSet<IModifier>();
            var toAdd = new HashSet<IModifier>(destHolder.GetAllTiledModifiers(OwnPlayer.PlayerName));

            foreach (var m in AffectedTiledModifiers)
            {
                if (toAdd.Contains(m))
                    toAdd.Remove(m);
                else
                    toRemove.Add(m);
            }

            // Should consider non-tiled modifiers when the tilemap holder changes
            if (curHolder.Id != destHolder.Id)
            {
                foreach (var m in destHolder.GetModifiers(OwnPlayer.PlayerName))
                    toAdd.Add(m);

                foreach (var m in curHolder.GetModifiers(OwnPlayer.PlayerName))
                {
                    if (toAdd.Contains(m))
                        toAdd.Remove(m);
                    else
                        toRemove.Add(m);
                }
            }

            // Remove modifier before detaching
            foreach (var m in toRemove)
                ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, true, false);

            if (!withoutTileMapAction)
                CurrentTile.RemoveTileObject(TypeName);

            CurrentTile = tile;

            // Add modifier after attaching
            if (!withoutTileMapAction)
                tile.AddTileObject(this);

            foreach (var m in toAdd)
                ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, false, false);
        }

        #endregion
    }
}
