using Core.GameData;
using MoonSharp.Interpreter;
using System.Collections.Generic;

namespace Core
{
    [MoonSharpUserData]
    public sealed class StarSystem : ITileMapHolder
    {
        public string TypeName => nameof(StarSystem);

        public string Guid { get; }

        public TileMap TileMap { get; }

        private readonly Dictionary<string, Modifier> _modifiers = new Dictionary<string, Modifier>();

        [MoonSharpHidden]
        public IEnumerable<Modifier> Modifiers
        {
            get
            {
                // TODO: Change this to Game Modifiers
                var upwardModifiers = new List<Modifier>();

                foreach (var m in upwardModifiers)
                    yield return m;

                foreach (var m in _modifiers.Values)
                    yield return m;
            }
        }

        private readonly Dictionary<string, TiledModifier> _tiledModifiers = new Dictionary<string, TiledModifier>();

        [MoonSharpHidden]
        public IEnumerable<TiledModifier> TiledModifiers => _tiledModifiers.Values;

        private readonly Dictionary<string, object> _customValues = new Dictionary<string, object>();

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
            TileMap.StartNewTurn(month);
        }

        public object GetCustomValue(string key, object defaultValue) =>
            _customValues.TryGetValue(key, out var result) ? result : defaultValue;

        public void SetCustomValue(string key, object value)
        {
            if (!value.GetType().IsPrimitive && value.GetType() != typeof(string)) return;

            _customValues[key] = value;
        }

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

        public void AddTiledModifierRange(string modifierName, string adderGuid, string rangeKeyName, List<HexTileCoord> tiles, int leftMonth)
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
