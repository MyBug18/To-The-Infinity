using System;
using System.Collections.Generic;
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

        public HexTile CurrentTile { get; private set; }

        public int MeleeAttackPower { get; }

        public IReadOnlyDictionary<string, int> MeleeAttackPowerBonus { get; }

        public IReadOnlyCollection<string> Properties { get; }

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
        }

        public void TeleportToTile(HexTile tile) => TeleportToTile(tile, false);

        public void OnDamaged(IInfinityObject obj, int damage, DamageType damageType, bool isMelee)
            => throw new NotImplementedException();

        #region SpecialAction

        public IReadOnlyList<SpecialAction> SpecialActions { get; }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost) =>
            throw new NotImplementedException();

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
            => throw new NotImplementedException();

        #endregion

        #region Modifier

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

        #endregion

        #region MoveAction

        public int RemainMovePoint { get; private set; }

        public IReadOnlyDictionary<HexTileCoord, MoveInfo> GetMovableTileInfo()
        {
            var currentCoord = CurrentTile.Coord;
            var tileMap = CurrentTile.TileMap;

            var result = new Dictionary<HexTileCoord, MoveInfo> {{currentCoord, default}};

            var plan = new Queue<HexTileCoord>();
            plan.Enqueue(currentCoord);

            while (plan.Count > 0)
            {
                var cur = plan.Dequeue();

                var costUntilHere = result[cur].CostUntilHere;
                var around = tileMap.GetRing(1, cur);

                foreach (var c in around)
                {
                    if (result.ContainsKey(c))
                        continue;

                    var tile = tileMap.GetHexTile(c);

                    var newCost = costUntilHere + tile.StarShipMovePoint;

                    // Can't go any further due to the lack of the move point
                    if (newCost > RemainMovePoint)
                        continue;

                    plan.Enqueue(c);
                    result[tile.Coord] = new MoveInfo(c, newCost);
                }
            }

            result.Remove(currentCoord);
            return result;
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
