using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    public sealed class StarShip : IUnit, ISinglePlayerModifierHolder
    {
        private readonly HashSet<TriggerEventType> _relativeTriggerEventTypes = new HashSet<TriggerEventType>
        {
            TriggerEventType.BeforeDamaged,
            TriggerEventType.AfterDamaged,
            TriggerEventType.BeforeAttack,
            TriggerEventType.AfterAttack,
        };

        // BeforeDamaged(this, adderObjectGuid, damageInfo)
        // AfterDamaged(this, adderObjectGuid, damageInfo)
        // BeforeAttack(this, adderObjectGuid, damageInfo, attackTarget)
        // BeforeDamaged(this, adderObjectGuid, damageInfo, attackTarget)
        private readonly Dictionary<TriggerEventType, Dictionary<string, TriggerEvent>> _triggerEvents =
            new Dictionary<TriggerEventType, Dictionary<string, TriggerEvent>>();

        public string TypeName => nameof(StarShip);

        public string Guid { get; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public IPlayer OwnPlayer { get; }

        public string IdentifierName { get; }

        public string CustomName { get; }

        public IReadOnlyCollection<string> Properties { get; }

        public HexTile CurrentTile { get; private set; }

        public bool IsDestroyed { get; private set; }

        public void StartNewTurn(int month)
        {
            _remainMovePoint = MaxMovePoint;

            ReduceModifiersLeftMonth(month);
        }

        public void TeleportToTile(HexTile tile) => TeleportToTile(tile, false);

        public void DestroySelf()
        {
            throw new NotImplementedException();
        }

        #region Battle

        private int _remainHp;

        public string AttackDamageType { get; }

        public int BaseAttackPower { get; }

        public int BaseMaxHp { get; }

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
            if (_triggerEvents.TryGetValue(TriggerEventType.BeforeDamaged, out var value))
            {
                // BeforeDamaged(this, adderObjectGuid, damageInfo)
                var bd = value.Values.ToList();
                bd.Sort((x, y) => y.Priority.CompareTo(x.Priority));

                foreach (var e in bd)
                    e.Invoke(damageInfo);
            }

            RemainHp -= damageInfo.Amount;

            if (IsDestroyed) return;

            if (!_triggerEvents.TryGetValue(TriggerEventType.AfterDamaged, out value)) return;

            // AfterDamaged(this, adderObjectGuid, damageInfo)
            var ad = value.Values.ToList();
            ad.Sort((x, y) => y.Priority.CompareTo(x.Priority));

            foreach (var e in ad)
                e.Invoke(damageInfo);
        }


        #endregion

        #region SpecialAction

        public IReadOnlyList<SpecialAction> SpecialActions { get; }

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

        public void AddModifier(string modifierName, string adderObjectGuid, int leftMonth)
        {
            if (_modifiers.ContainsKey(modifierName))
            {
                Logger.Log(LogType.Warning, $"{nameof(StarShip)}.{nameof(AddModifier)}",
                    $"Trying to add modifier \"{modifierName}\" which already exists, so it will be ignored.");
                return;
            }

            var core = GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName);

            if (core.TargetType != TypeName)
            {
                Logger.Log(LogType.Warning, $"{nameof(StarShip)}.{nameof(AddModifier)}",
                    $"Modifier \"{modifierName}\" is not for {TypeName}, but for {core.TargetType}, so it will be ignored.");
                return;
            }

            if (core.IsTileLimited)
            {
                Logger.Log(LogType.Warning, $"{nameof(StarShip)}.{nameof(AddModifier)}",
                    $"Modifier \"{modifierName}\" is a tile limited modifier!, but tried to use as a tile unlimited modifier, so it will be ignored.");
                return;
            }

            var m = new Modifier(core, adderObjectGuid, leftMonth);

            _modifiers.Add(modifierName, m);
            ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, false);
        }

        public void RemoveModifier(string modifierName)
        {
            if (!_modifiers.ContainsKey(modifierName))
            {
                Logger.Log(LogType.Warning, $"{nameof(StarShip)}.{nameof(RemoveModifier)}",
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

            StartCachingModifierEffect();
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

        private void RegisterTriggerEvent(string modifierName, IReadOnlyDictionary<TriggerEventType, TriggerEvent> events)
        {
            foreach (var kv in events)
            {
                var type = kv.Key;

                if (!_relativeTriggerEventTypes.Contains(type))
                {
                    Logger.Log(LogType.Warning, $"{nameof(StarShip)}.{nameof(RegisterTriggerEvent)}",
                        $"{kv.Key} is not a valid event name for the {nameof(StarShip)}, so it will be ignored.");
                    continue;
                }

                if (!_triggerEvents.TryGetValue(type, out var value))
                {
                    value = new Dictionary<string, TriggerEvent>();
                }

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

        #endregion

        #region ModifierEffect Caching

        private Task _cacheTask;

        private bool _isCachingModifierEffect;

        private bool _cancelCaching;

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

            _cacheTask = Task.Run(CacheModifierEffect);
        }

        private void CacheModifierEffect()
        {
            _modifierEffectsMap.Clear();
            _isCachingModifierEffect = true;

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
                    Logger.Log(LogType.Error, "", $"Error while calculating modifier effect! Error message: {e.Message}");
                }

                if (effects?.Count == 0) continue;

                _modifierEffectsMap[m.Name] = ApplyModifierEffects(effects);
            }

            _isCachingModifierEffect = false;
        }

        // Possible modifier effect form:
        // MaxMovePoint
        private IReadOnlyList<ModifierEffect> ApplyModifierEffects(IEnumerable<ModifierEffect> effects)
        {
            _maxMovePointFromModifier = 0;

            var result = new List<ModifierEffect>();

            foreach (var e in effects)
            {
                switch (e.EffectType)
                {
                    // MaxMovePoint
                    case ModifierEffectType.MaxMovePoint:
                    {
                        var infos = e.AdditionalInfos;
                        if (infos.Count != 0)
                            continue;

                        var amount = e.Amount;
                        _maxMovePointFromModifier += amount;
                        result.Add(e);
                        break;
                    }
                }
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

        private int _remainMovePoint;

        private int _maxMovePointFromModifier;

        public int RemainMovePoint
        {
            get => _remainMovePoint;
            private set => _remainMovePoint = Math.Max(0, value);
        }

        public int BaseMaxMovePoint { get; }

        public int MaxMovePoint => WaitCache(_maxMovePointFromModifier) + BaseMaxMovePoint;

        public IReadOnlyDictionary<HexTileCoord, MoveInfo> MovableTileInfo => WaitCache(_movableTileInfoCache);

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
                var nextMove = route.Pop();

                var nextTile = CurrentTile.TileMap.GetHexTile(nextMove);

                if (nextTile.StarShipMovePoint > RemainMovePoint)
                {
                    // No sufficient move point to move to next tile (possibly due to modifier effects), so end move here.
                    CurrentTile.AddTileObject(this);
                    return;
                }

                RemainMovePoint -= nextTile.StarShipMovePoint;
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

                    var newCost = costUntilHere + tile.StarShipMovePoint;

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
            if (curHolder.Guid != destHolder.Guid)
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
