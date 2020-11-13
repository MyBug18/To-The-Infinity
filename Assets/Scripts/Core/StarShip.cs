using System;
using System.Collections.Generic;
using System.Linq;
using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    public sealed class StarShip : IUnit
    {
        private readonly Dictionary<string, Modifier> _modifiers = new Dictionary<string, Modifier>();
        public string TypeName => nameof(StarShip);

        public string Guid { get; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public IPlayer OwnPlayer { get; }

        public string IdentifierName { get; }

        public string CustomName { get; }

        public HexTile CurrentTile { get; private set; }

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
        }

        public void TeleportToTile(HexTile tile) => TeleportToTile(tile, false);

        public int MeleeAttackPower { get; }

        public IReadOnlyDictionary<string, int> MeleeAttackPowerBonus { get; }

        public IReadOnlyCollection<string> Properties { get; }

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
        public IEnumerable<Modifier> Modifiers
        {
            get
            {
                foreach (var m in CurrentTile.TileMap.Holder.Modifiers)
                    yield return m;

                foreach (var m in _modifiers.Values)
                    yield return m;
            }
        }

        public IEnumerable<TiledModifier> AffectedTiledModifiers =>
            CurrentTile.TileMap.Holder.TiledModifiers.Where(m => m.IsInRange(CurrentTile.Coord));

        public void AddModifier(string modifierName, string adderPlayerName, string adderObjectGuid, int leftMonth)
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

            var m = new Modifier(core, adderPlayerName, adderObjectGuid, leftMonth);

            _modifiers.Add(modifierName, m);
            ApplyModifierChangeToDownward(m.Core, false);
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
            ApplyModifierChangeToDownward(m.Core, true);
        }

        public bool HasModifier(string modifierName) => _modifiers.ContainsKey(modifierName);

        [MoonSharpHidden]
        public void ApplyModifierChangeToDownward(ModifierCore m, bool isRemoving)
        {
            if (!m.Scope.ContainsKey(TypeName)) return;

            var scope = m.Scope[TypeName];

            if (isRemoving)
            {
                scope.OnRemoved(this);

                RegisterModifierEvent(m.Name, scope.TriggerEvent, true);
            }
            else
            {
                scope.OnAdded(this);

                RegisterModifierEvent(m.Name, scope.TriggerEvent, false);
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

        private void RegisterModifierEvent(string modifierName,
            IReadOnlyDictionary<string, ScriptFunctionDelegate> events, bool isRemoving)
        {
            foreach (var kv in events)
            {
                switch (kv.Key)
                {
                    default:
                        Logger.Log(LogType.Warning, $"{nameof(StarShip)}.{nameof(RegisterModifierEvent)}",
                            $"{kv.Key} is not a valid event name for the {nameof(StarShip)}, so it will be ignored.");
                        break;
                }
            }
        }

        #endregion

        #region MoveAction

        public int RemainMovePoint { get; private set; }

        public IReadOnlyDictionary<HexTileCoord, MoveInfo> GetMovableTileInfo()
        {
            var result = new Dictionary<HexTileCoord, MoveInfo> {{CurrentTile.Coord, default}};

            var plan = new Queue<HexTileCoord>();
            plan.Enqueue(CurrentTile.Coord);

            var tileMap = CurrentTile.TileMap;

            while (plan.Count > 0)
            {
                var cur = plan.Dequeue();

                var around = CurrentTile.TileMap.GetRing(1, cur);

                foreach (var c in around)
                {
                    var tile = CurrentTile.TileMap.GetHexTile(c);
                }
            }


            return result;
        }

        private void TeleportToTile(HexTile tile, bool withoutTileMapAction)
        {
            var destHolder = tile.TileMap.Holder;

            var toRemove = new HashSet<ModifierCore>();
            var toAdd = new HashSet<ModifierCore>(destHolder.TiledModifiers.Select(x => x.Core));

            foreach (var m in AffectedTiledModifiers)
            {
                var core = m.Core;

                if (toAdd.Contains(core))
                    toAdd.Remove(core);
                else
                    toRemove.Add(core);
            }

            // Should consider not tiled modifier when the tilemap changes
            if (CurrentTile.TileMap.Holder != destHolder)
            {
                foreach (var m in destHolder.Modifiers)
                    toAdd.Add(m.Core);

                foreach (var m in Modifiers)
                {
                    var core = m.Core;

                    if (toAdd.Contains(core))
                        toAdd.Remove(core);
                    else
                        toRemove.Add(core);
                }
            }

            // Remove modifier before detaching
            foreach (var mc in toRemove)
                ApplyModifierChangeToDownward(mc, true);

            if (!withoutTileMapAction)
                CurrentTile.RemoveTileObject(TypeName);

            CurrentTile = tile;

            // Add modifier after attaching
            if (!withoutTileMapAction)
                tile.AddTileObject(this);

            foreach (var mc in toAdd)
                ApplyModifierChangeToDownward(mc, false);
        }

        #endregion
    }
}
