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
        private readonly Dictionary<string, Modifier> _modifiers = new Dictionary<string, Modifier>();
        public string TypeName => nameof(StarShip);

        public string Guid { get; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public IPlayer OwnPlayer { get; }

        public string IdentifierName { get; }

        public string CustomName { get; }

        public IReadOnlyCollection<string> Properties { get; }

        public HexTile CurrentTile { get; private set; }

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
        }

        public void TeleportToTile(HexTile tile) => TeleportToTile(tile, false);

        #region Damage

        public int BaseAttackPower { get; }

        public void OnDamaged(DamageInfo damageInfo)
        {

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

        private bool _isCachingModifierEffect;

        private readonly Dictionary<string, IReadOnlyList<ModifierEffect>> _modifierEffectsMap =
            new Dictionary<string, IReadOnlyList<ModifierEffect>>();

        public IReadOnlyDictionary<string, IReadOnlyList<ModifierEffect>> ModifierEffectsMap => _modifierEffectsMap;

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

        [MoonSharpHidden]
        public void StartCachingModifierEffect()
        {
            Task.Run(CacheModifierEffect);
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

        private void RegisterTriggerEvent(string modifierName, IReadOnlyDictionary<string, TriggerEvent> events)
        {
            foreach (var kv in events)
            {
                switch (kv.Key)
                {
                    default:
                        Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(RegisterTriggerEvent)}",
                            $"{kv.Key} is not a valid event name for the {nameof(Planet)}, so it will be ignored.");
                        break;
                }
            }
        }

        private void RemoveTriggerEvent(string modifierName)
        {
        }

        private void CacheModifierEffect()
        {
            _modifierEffectsMap.Clear();
            _isCachingModifierEffect = true;

            foreach (var m in GetModifiers().Cast<IModifier>().Concat(AffectedTiledModifiers))
            {
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
                _modifierEffectsMap[m.Name] = effects;
            }

            _isCachingModifierEffect = false;
        }
        private bool ApplyModifierEffects(IEnumerable<ModifierEffect> effects)
        {
            var result = true;

            foreach (var e in effects)
            {
            }

            return result;
        }


        // Possible modifier effect form:
        // MaxMovePoint
        private bool ApplyModifierEffect(ModifierEffect effect)
        {
            switch (effect.EffectType)
            {
                // ReduceDamage_<Target Damage Type>
                case ModifierEffectType.MaxMovePoint:
                {
                    var infos = effect.AdditionalInfos;
                    if (infos.Count != 0) return false;

                    var amount = effect.Amount;
                    break;
                }
            }

            return true;
        }

        #endregion

        #region MoveAction

        private Dictionary<HexTileCoord, MoveInfo> _movableTileInfoCache;

        private int _remainMovePoint;

        public int RemainMovePoint
        {
            get => _remainMovePoint;
            private set => _remainMovePoint = Math.Max(0, value);
        }

        public IReadOnlyDictionary<HexTileCoord, MoveInfo> MovableTileInfo
        {
            get
            {
                while (_isCachingModifierEffect)
                {
                    // Busy loop
                }

                return _movableTileInfoCache;
            }
        }

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

        private T WaitCache<T>(T input)
        {
            while (_isCachingModifierEffect)
            {
                // Busy wait
            }

            return input;
        }
    }
}
