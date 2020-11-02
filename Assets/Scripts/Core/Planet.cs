﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    [MoonSharpUserData]
    public sealed class Planet : ITileMapHolder, IOnHexTileObject
    {
        private readonly Dictionary<string, Modifier> _modifiers = new Dictionary<string, Modifier>();

        #region TriggerEvent

        private readonly Dictionary<string, Action> _onPopBirth = new Dictionary<string, Action>();

        #endregion TriggerEvent

        private readonly Dictionary<string, float> _planetaryResourceKeep =
            new Dictionary<string, float>();

        private readonly Dictionary<string, TiledModifier> _tiledModifiers = new Dictionary<string, TiledModifier>();

        /// <summary>
        ///     0 if totally uninhabitable,
        ///     1 if partially inhabitable with serious penalty,
        ///     2 if partially inhabitable with minor penalty,
        ///     3 if totally inhabitable without any penalty.
        /// </summary>
        public int InhabitableLevel { get; }

        public bool IsColonizing { get; private set; }

        public IReadOnlyDictionary<string, float> PlanetaryResourceKeep => _planetaryResourceKeep;

        public string Owner { get; }

        public IReadOnlyList<SpecialAction> SpecialActions { get; }

        public string IdentifierName { get; }

        public string CustomName { get; private set; }

        public HexTile CurrentTile { get; }

        public IEnumerable<TiledModifier> AffectedTiledModifiers =>
            CurrentTile.TileMap.Holder.TiledModifiers.Where(m => m.IsInRange(CurrentTile.Coord));

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
            TileMap.StartNewTurn(month);
        }

        public void TeleportToTile(HexTile tile)
        {
            var destHolder = tile.TileMap.Holder;

            if (destHolder.TypeName != nameof(StarSystem))
            {
                Logger.Instance.LogWarning(nameof(TeleportToTile), "Planet can exist only on StarSystem!");
                return;
            }

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
            CurrentTile.RemoveTileObject(TypeName);

            // Add modifier after attaching
            tile.AddTileObject(this);
            foreach (var mc in toAdd)
                ApplyModifierChangeToDownward(mc, false);
        }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost) =>
            throw new NotImplementedException();

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
        {
            throw new NotImplementedException();
        }

        public string TypeName => nameof(Planet);

        public string Guid { get; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public TileMap TileMap { get; }

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

        [MoonSharpHidden]
        public IEnumerable<TiledModifier> TiledModifiers => _tiledModifiers.Values;

        #region Pop

        private readonly List<Pop> _pops = new List<Pop>();

        public IReadOnlyList<Pop> Pops => _pops;

        private readonly List<Pop> _unemployedPops = new List<Pop>();

        public IReadOnlyList<Pop> UnemployedPops => _unemployedPops;

        public const float BasePopGrowth = 5.0f;

        #endregion Pop

        #region Modifier

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

            toRemoveList.Clear();

            foreach (var m in TiledModifiers)
            {
                var removedRange = m.ReduceLeftMonth(month);

                if (removedRange.Count == 0) continue;

                TileMap.ApplyModifierChangeToTileObjects(m.Core, true, removedRange);

                if (m.Infos.Count == 0)
                    toRemoveList.Add(m.Name);
            }

            foreach (var name in toRemoveList)
                _tiledModifiers.Remove(name);
        }

        private void RegisterModifierEvent(string modifierName,
            IReadOnlyDictionary<string, ScriptFunctionDelegate> events, bool isRemoving)
        {
            foreach (var kv in events)
            {
                switch (kv.Key)
                {
                    case "OnPopBirth":
                        if (isRemoving)
                            _onPopBirth.Remove(modifierName);
                        else
                            _onPopBirth.Add(modifierName, () => kv.Value.Invoke(this));
                        break;
                }
            }
        }

        public void AddModifier(string modifierName, string adderGuid, int leftMonth)
        {
            if (_modifiers.ContainsKey(modifierName))
            {
                Logger.Instance.LogWarning(nameof(AddModifier),
                    $"Trying to add modifier \"{modifierName}\" which already exists!");
                return;
            }

            var core = GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName);

            if (core.TargetType != TypeName)
            {
                Logger.Instance.LogWarning(nameof(AddModifier),
                    $"Modifier \"{modifierName}\" is not for {TypeName}, but for {core.TargetType}!");
                return;
            }

            if (core.IsTileLimited)
            {
                Logger.Instance.LogWarning(nameof(AddModifier),
                    $"Modifier \"{modifierName}\" is a tile limited modifier!, but tried to use as a tile unlimited modifier!");
                return;
            }

            var m = new Modifier(core, adderGuid, leftMonth);

            _modifiers.Add(modifierName, m);
            ApplyModifierChangeToDownward(m.Core, false);
        }

        public void RemoveModifier(string modifierName)
        {
            if (!_modifiers.ContainsKey(modifierName))
            {
                Logger.Instance.LogWarning(nameof(RemoveModifier),
                    $"Trying to remove modifier \"{modifierName}\" which doesn't exist!");
                return;
            }

            var m = _modifiers[modifierName];
            _modifiers.Remove(modifierName);
            ApplyModifierChangeToDownward(m.Core, true);
        }

        [MoonSharpHidden]
        public void ApplyModifierChangeToDownward(ModifierCore m, bool isRemoving)
        {
            if (m.Scope.ContainsKey(TypeName))
            {
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

            TileMap.ApplyModifierChangeToTileObjects(m, isRemoving);
        }

        public bool HasModifier(string modifierName) => _modifiers.ContainsKey(modifierName);

        public void AddTiledModifierRange(string modifierName, string adderGuid, string rangeKeyName,
            List<HexTileCoord> tiles, int leftMonth)
        {
            if (!_tiledModifiers.TryGetValue(modifierName, out var m))
            {
                var core = GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName);

                if (core.TargetType != TypeName)
                {
                    Logger.Instance.LogWarning(nameof(AddTiledModifierRange),
                        $"Modifier \"{modifierName}\" is not for \"{TypeName}\", but for \"{core.TargetType}\"!");
                    return;
                }

                if (!core.IsTileLimited)
                {
                    Logger.Instance.LogWarning(nameof(AddTiledModifierRange),
                        $"Modifier \"{modifierName}\" is not a tile limited modifier, but tried to use as a tile limited modifier!");
                    return;
                }

                var tileSet = new HashSet<HexTileCoord>(tiles);

                m = new TiledModifier(core, adderGuid, rangeKeyName, tileSet, leftMonth);

                _tiledModifiers[modifierName] = m;
                TileMap.ApplyModifierChangeToTileObjects(m.Core, false, tileSet);
            }
            else
            {
                if (m.AdderGuid != adderGuid)
                {
                    Logger.Instance.LogWarning(nameof(AddTiledModifierRange),
                        $"Modifier \"{modifierName}\" has already added by different object : \"{m.AdderGuid}\"!");
                    return;
                }

                if (m.Infos.ContainsKey(rangeKeyName))
                {
                    Logger.Instance.LogWarning(nameof(AddTiledModifierRange),
                        $"Range key name \"{rangeKeyName}\" already exists in modifier \"{m.Name}\"!");
                    return;
                }

                var pureAdd = m.AddTileInfo(rangeKeyName, new HashSet<HexTileCoord>(tiles), leftMonth);

                TileMap.ApplyModifierChangeToTileObjects(m.Core, false, pureAdd);
            }
        }

        public void MoveTiledModifierRange(string modifierName, string rangeKeyName, List<HexTileCoord> tiles)
        {
            if (!_tiledModifiers.TryGetValue(modifierName, out var m))
            {
                Logger.Instance.LogWarning(nameof(MoveTiledModifierRange),
                    $"Trying to access modifier \"{modifierName}\" which doesn't exist!");
                return;
            }

            if (!m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Instance.LogWarning(nameof(MoveTiledModifierRange),
                    $"There is no range key \"{rangeKeyName}\" in modifier \"{modifierName}\"!");
                return;
            }

            var (pureAdd, pureRemove) = m.MoveTileInfo(rangeKeyName, new HashSet<HexTileCoord>(tiles));

            TileMap.ApplyModifierChangeToTileObjects(m.Core, false, pureAdd);
            TileMap.ApplyModifierChangeToTileObjects(m.Core, true, pureRemove);
        }

        public void RemoveTiledModifierRange(string modifierName, string rangeKeyName)
        {
            if (!_tiledModifiers.TryGetValue(modifierName, out var m))
            {
                Logger.Instance.LogWarning(nameof(RemoveTiledModifierRange),
                    $"Trying to remove modifier \"{modifierName}\" which doesn't exist!");
                return;
            }

            if (!m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Instance.LogWarning(nameof(RemoveTiledModifierRange),
                    $"There is no range key \"{rangeKeyName}\" in modifier \"{modifierName}\"!");
                return;
            }

            var pureRemove = m.RemoveTileInfo(rangeKeyName);

            TileMap.ApplyModifierChangeToTileObjects(m.Core, true, pureRemove);

            if (m.Infos.Count == 0)
                _tiledModifiers.Remove(modifierName);
        }

        #endregion Modifier
    }
}
