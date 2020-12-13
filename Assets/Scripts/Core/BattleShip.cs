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

        public string TypeName => nameof(BattleShip);

        public int Id { get; set; }

        public IPlayer OwnPlayer { get; }

        [MoonSharpHidden]
        public string CustomName { get; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public bool IsDestroyed { get; private set; }

        public void StartNewTurn(int month)
        {
            RemainMovePoint = MaxMovePoint;

            ReduceModifiersLeftMonth(month);
        }

        [MoonSharpHidden]
        public InfinityObjectData Save()
        {
            if (IsDestroyed) return null;

            var result = new Dictionary<string, object>
            {
                ["IdentifierName"] = IdentifierName,
                ["CustomName"] = CustomName,
                ["Storage"] = Storage.Data,
                ["OwnPlayer"] = OwnPlayer.Id,
                ["Modifiers"] = _modifiers.Values.Select(x => x.ToSaveData()).ToList(),
                ["SpecialActions"] = SpecialActions.Keys.ToArray(),
                ["RemainHp"] = _remainHp,
                ["RemainMovePoint"] = RemainMovePoint,
            };

            return new InfinityObjectData(Id, TypeName, result);
        }

        public void TeleportToTile(HexTile tile) => TeleportToTile(tile, false);

        public void DestroySelf()
        {
            // BeforeDestroyed(this, adderObject)
            foreach (var e in GetTriggerEvents(TriggerEventType.BeforeDestroyed))
                e.Invoke();

            IsDestroyed = true;
            CurrentTile.RemoveTileObject(TypeName);
            CurrentTile = null;
        }

        #region ProtoTypeData

        public string IdentifierName { get; }

        public string AttackDamageType { get; }

        public int BaseAttackPower { get; }

        public int BaseMaxHp { get; }

        public int BaseMaxMovePoint { get; }

        public IReadOnlyCollection<string> Properties { get; }

        #endregion

        #region Battle

        private int _remainHp;

        private int _attackPowerFromModifierAbsolute, _attackPowerFromModifierRelative;

        public int AttackPower => (BaseAttackPower + WaitCache(_attackPowerFromModifierAbsolute)) *
            (100 + _attackPowerFromModifierRelative) / 100;

        public int RemainHp
        {
            get => _remainHp;

            [MoonSharpHidden]
            set
            {
                _remainHp += value;

                if (_remainHp <= 0)
                {
                    _remainHp = 0;
                    DestroySelf();
                    return;
                }

                _remainHp = Math.Min(BaseMaxHp, _remainHp);
            }
        }

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

        public IReadOnlyDictionary<string, SpecialAction> SpecialActions { get; }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost) =>
            throw new NotImplementedException();

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
            => throw new NotImplementedException();

        #endregion

        #region Modifier

        private readonly Dictionary<string, Modifier> _modifiers = new Dictionary<string, Modifier>();

        [MoonSharpHidden]
        public IEnumerable<Modifier> GetModifiers()
        {
            foreach (var m in CurrentTile.TileMap.Holder.GetModifiers(OwnPlayer.PlayerName))
                yield return m;

            foreach (var m in _modifiers.Values)
                yield return m;
        }

        public IEnumerable<TiledModifier> AffectedTiledModifiers =>
            CurrentTile.TileMap.Holder.GetTiledModifiers(this);

        public void AddModifier(string modifierName, IInfinityObject adder, int leftMonth)
        {
            if (_modifiers.ContainsKey(modifierName))
            {
                Logger.Log(LogType.Warning, $"{nameof(BattleShip)}.{nameof(AddModifier)}",
                    $"Trying to add modifier \"{modifierName}\" which already exists, so it will be ignored.");
                return;
            }

            var core = GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName);

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

            var m = new Modifier(core, adder.Id, leftMonth);

            _modifiers.Add(modifierName, m);
            ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, false);
        }

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
            ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, true);
        }

        public bool HasModifier(string modifierName) => _modifiers.ContainsKey(modifierName);

        [MoonSharpHidden]
        public void ApplyModifierChangeToDownward(string targetPlayerName, IModifier m, bool isRemoving)
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
                m.OnAdded(this);

                RegisterTriggerEvent(m.Name, m.GetTriggerEvent(this));
            }
        }

        private void ReduceModifiersLeftMonth(int month)
        {
            var toRemoveList = new List<string>();

            foreach (var name in _modifiers.Keys)
            {
                var m = _modifiers[name];
                if (m.IsPermanent) continue;

                if (m.LeftMonth - month <= 0)
                {
                    toRemoveList.Add(name);
                    continue;
                }

                _modifiers[name].ReduceLeftMonth(month);
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
            _isCachingModifierEffect = false;
        }

        private IReadOnlyList<ModifierEffect> ApplyModifierEffects(IEnumerable<ModifierEffect> effects)
        {
            _maxMovePointFromModifier = 0;
            _attackPowerFromModifierAbsolute = 0;
            _attackPowerFromModifierRelative = 0;

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
                    // AttackPower_<A || R>
                    case ModifierEffectType.AttackPower:
                    {
                        if (infos.Count != 1)
                            continue;

                        switch (infos[1].ToLower())
                        {
                            case "a":
                                _attackPowerFromModifierAbsolute += amount;
                                break;
                            case "r":
                                _attackPowerFromModifierRelative += amount;
                                break;
                            default:
                                continue;
                        }

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
            var toAdd = new HashSet<IModifier>(destHolder.GetTiledModifiers(OwnPlayer.PlayerName));

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
                ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, true);

            if (!withoutTileMapAction)
                CurrentTile.RemoveTileObject(TypeName);

            CurrentTile = tile;

            // Add modifier after attaching
            if (!withoutTileMapAction)
                tile.AddTileObject(this);

            foreach (var m in toAdd)
                ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, false);
        }

        #endregion
    }
}