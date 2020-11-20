using System;
using System.Collections.Generic;
using System.Linq;
using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    [MoonSharpUserData]
    public sealed class Planet : ITileMapHolder, IOnHexTileObject
    {
        #region TriggerEvent

        private readonly Dictionary<string, Action> _onPopBirth = new Dictionary<string, Action>();

        #endregion TriggerEvent

        private readonly Dictionary<string, float> _planetaryResourceKeep =
            new Dictionary<string, float>();

        private readonly Dictionary<string, Dictionary<string, Modifier>> _playerModifierMap =
            new Dictionary<string, Dictionary<string, Modifier>>();

        private readonly Dictionary<string, Dictionary<string, TiledModifier>> _playerTiledModifierMap =
            new Dictionary<string, Dictionary<string, TiledModifier>>();

        /// <summary>
        ///     0 if totally uninhabitable,
        ///     1 if partially inhabitable with serious penalty,
        ///     2 if partially inhabitable with minor penalty,
        ///     3 if totally inhabitable without any penalty.
        /// </summary>
        public int InhabitableLevel { get; }

        public bool IsColonizing { get; private set; }

        public IReadOnlyDictionary<string, float> PlanetaryResourceKeep => _planetaryResourceKeep;

        public string IdentifierName { get; }

        public string CustomName { get; private set; }

        public HexTile CurrentTile { get; private set; }

        public void TeleportToTile(HexTile tile)
        {
            var destHolder = tile.TileMap.Holder;
            var curHolder = CurrentTile.TileMap.Holder;

            if (destHolder.TypeName != nameof(StarSystem))
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(TeleportToTile)}",
                    "Planet can exist only on StarSystem!");
                return;
            }

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

            // Remove modifier effect before detaching
            foreach (var m in toRemove)
                ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, true);
            CurrentTile.RemoveTileObject(TypeName);

            CurrentTile = tile;

            // Add modifier effect after attaching
            tile.AddTileObject(this);
            foreach (var m in toAdd)
                ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, false);
        }

        public IPlayer OwnPlayer { get; }

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
            TileMap.StartNewTurn(month);
        }

        public string TypeName => nameof(Planet);

        public string Guid { get; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public TileMap TileMap { get; }

        #region Pop

        private readonly List<Pop> _pops = new List<Pop>();

        public IReadOnlyList<Pop> Pops => _pops;

        private readonly List<Pop> _unemployedPops = new List<Pop>();

        public IReadOnlyList<Pop> UnemployedPops => _unemployedPops;

        public const float BasePopGrowth = 5.0f;

        #endregion Pop

        #region SpecialAction

        public IReadOnlyList<SpecialAction> SpecialActions { get; }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost) =>
            throw new NotImplementedException();

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Modifier

        public IEnumerable<TiledModifier> AffectedTiledModifiers =>
            CurrentTile.TileMap.Holder.GetTiledModifiers(this);

        [MoonSharpHidden]
        public IEnumerable<Modifier> GetModifiers(string targetPlayerName)
        {
            foreach (var m in CurrentTile.TileMap.Holder.GetModifiers(targetPlayerName))
                yield return m;

            if (_playerModifierMap.TryGetValue("Global", out var globalModifiers))
                foreach (var m in globalModifiers.Values)
                    yield return m;

            if (!_playerModifierMap.TryGetValue(targetPlayerName, out var playerModifiers)) yield break;

            foreach (var m in playerModifiers.Values)
                yield return m;
        }

        [MoonSharpHidden]
        public IEnumerable<TiledModifier> GetTiledModifiers(string targetPlayerName)
        {
            if (_playerTiledModifierMap.TryGetValue("Global", out var globalModifiers))
                foreach (var m in globalModifiers.Values)
                    yield return m;

            if (!_playerTiledModifierMap.TryGetValue(targetPlayerName, out var playerModifiers)) yield break;

            foreach (var m in playerModifiers.Values)
                yield return m;
        }

        [MoonSharpHidden]
        public IEnumerable<TiledModifier> GetTiledModifiers(IOnHexTileObject target)
            => GetTiledModifiers(target.OwnPlayer.PlayerName).Where(x => x.IsInRange(target.CurrentTile.Coord));

        public void AddModifier(string targetPlayerName, string modifierName, string adderObjectGuid, int leftMonth)
        {
            var core = GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName);

            if (!CommonCheckModifierCoreAddable(core, targetPlayerName)) return;

            if (core.IsTileLimited)
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddModifier)}",
                    $"Modifier \"{core.Name}\" is a tile limited modifier but tried to use as a tile unlimited modifier, so it will be ignored.");
                return;
            }

            // Make one if nothing exist.
            if (!_playerModifierMap.TryGetValue(targetPlayerName, out var modifiers))
            {
                modifiers = new Dictionary<string, Modifier>();
                _playerModifierMap[targetPlayerName] = modifiers;
            }

            if (modifiers.ContainsKey(modifierName))
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddModifier)}",
                    $"Trying to add modifier \"{modifierName}\" which already exists for {targetPlayerName}, so it will be ignored.");
                return;
            }

            var m = new Modifier(core, adderObjectGuid, leftMonth);

            modifiers[modifierName] = m;

            ApplyModifierChangeToDownward(targetPlayerName, m, false);
        }

        public void RemoveModifier(string targetPlayerName, string modifierName)
        {
            if (!_playerModifierMap.TryGetValue(targetPlayerName, out var modifiers) ||
                !modifiers.ContainsKey(modifierName))
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(RemoveModifier)}",
                    $"Trying to remove modifier \"{modifierName}\" which doesn't exist for {targetPlayerName}, so it will be ignored.");
                return;
            }

            var m = modifiers[modifierName];
            modifiers.Remove(modifierName);

            if (modifiers.Count == 0)
                _playerModifierMap.Remove(targetPlayerName);

            ApplyModifierChangeToDownward(targetPlayerName, m, true);
        }

        [MoonSharpHidden]
        public void ApplyModifierChangeToDownward(string targetPlayerName, IModifier m, bool isRemoving)
        {
            if (targetPlayerName.ToLower() != "global" && targetPlayerName != OwnPlayer.PlayerName)
            {
                TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, isRemoving);
                return;
            }

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

            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, isRemoving);
        }

        public bool HasModifier(string targetPlayerName, string modifierName) =>
            _playerModifierMap.TryGetValue(targetPlayerName, out var modifiers) && modifiers.ContainsKey(modifierName);

        public void AddTiledModifierRange(string targetPlayerName, string modifierName, string adderObjectGuid,
            string rangeKeyName, HashSet<HexTileCoord> tiles, int leftMonth)
        {
            var core = GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName);

            if (!CommonCheckModifierCoreAddable(core, targetPlayerName)) return;

            if (!core.IsTileLimited)
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddTiledModifierRange)}",
                    $"Modifier \"{modifierName}\" is not a tile limited modifier, but tried to use as a tile limited modifier, so it will be ignored.");
                return;
            }

            if (!_playerTiledModifierMap.TryGetValue(targetPlayerName, out var modifiers))
            {
                modifiers = new Dictionary<string, TiledModifier>();
                _playerTiledModifierMap[modifierName] = modifiers;
            }

            if (!modifiers.TryGetValue(modifierName, out var m))
            {
                m = new TiledModifier(core, adderObjectGuid, rangeKeyName, tiles, leftMonth);
                TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, false, tiles);
                return;
            }

            if (m.AdderObjectGuid != adderObjectGuid)
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddTiledModifierRange)}",
                    $"Modifier \"{modifierName}\" has already added by different object : \"{m.AdderObjectGuid}\" for {targetPlayerName}, so it will be ignored.");
                return;
            }

            if (m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddTiledModifierRange)}",
                    $"Range key name \"{rangeKeyName}\" already exists in modifier \"{m.Name}\" for {targetPlayerName}, so it will be ignored.");
                return;
            }

            var pureAdd = m.AddTileInfo(rangeKeyName, tiles, leftMonth);

            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, false, pureAdd);
        }

        public void MoveTiledModifierRange(string targetPlayerName, string modifierName, string rangeKeyName,
            HashSet<HexTileCoord> tiles)
        {
            if (!_playerTiledModifierMap.TryGetValue(targetPlayerName, out var modifiers) ||
                !modifiers.TryGetValue(modifierName, out var m))
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(MoveTiledModifierRange)}",
                    $"Trying to access modifier \"{modifierName}\" which doesn't exist, so it will be ignored.");
                return;
            }

            if (!m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(MoveTiledModifierRange)}",
                    $"There is no range key \"{rangeKeyName}\" in modifier \"{modifierName}\", so it will be ignored.");
                return;
            }

            var (pureAdd, pureRemove) = m.MoveTileInfo(rangeKeyName, tiles);

            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, false, pureAdd);
            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, true, pureRemove);
        }

        public void RemoveTiledModifierRange(string targetPlayerName, string modifierName, string rangeKeyName)
        {
            if (!_playerTiledModifierMap.TryGetValue(targetPlayerName, out var modifiers) ||
                !modifiers.TryGetValue(modifierName, out var m))
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(RemoveTiledModifierRange)}",
                    $"Trying to remove modifier \"{modifierName}\" which doesn't exist, so it will be ignored.");
                return;
            }

            if (!m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(RemoveTiledModifierRange)}",
                    $"There is no range key \"{rangeKeyName}\" in modifier \"{modifierName}\", so it will be ignored.");
                return;
            }

            var pureRemove = m.RemoveTileInfo(rangeKeyName);

            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, true, pureRemove);

            if (m.Infos.Count == 0)
                modifiers.Remove(modifierName);

            if (modifiers.Count == 0)
                _playerTiledModifierMap.Remove(targetPlayerName);
        }

        private void ReduceModifiersLeftMonth(int month)
        {
            var toRemoveList = new List<string>();

            foreach (var kv in _playerModifierMap)
            {
                var modifiers = kv.Value;

                foreach (var m in modifiers.Values.Where(m => !m.IsPermanent))
                {
                    if (m.LeftMonth - month <= 0)
                    {
                        toRemoveList.Add(m.Name);
                        continue;
                    }

                    m.ReduceLeftMonth(month);
                }

                foreach (var n in toRemoveList)
                    RemoveModifier(kv.Key, n);

                toRemoveList.Clear();
            }

            var toRemovePlayerList = new List<string>();

            foreach (var kv in _playerTiledModifierMap)
            {
                var modifiers = kv.Value;

                foreach (var m in modifiers.Values)
                {
                    var pureRemove = m.ReduceLeftMonth(month);

                    if (m.Infos.Count == 0)
                        toRemoveList.Add(m.Name);

                    TileMap.ApplyModifierChangeToTileObjects(kv.Key, m, true, pureRemove);
                }

                foreach (var n in toRemoveList)
                    modifiers.Remove(n);

                if (modifiers.Count == 0)
                    toRemovePlayerList.Add(kv.Key);

                toRemoveList.Clear();
            }

            foreach (var n in toRemovePlayerList)
                _playerTiledModifierMap.Remove(n);
        }

        private void RegisterTriggerEvent(string modifierName, IReadOnlyDictionary<string, TriggerEvent> events)
        {
            foreach (var kv in events)
            {
                switch (kv.Key)
                {
                    case "OnPopBirth":
                        _onPopBirth.Add(modifierName, () => kv.Value.Invoke());
                        break;

                    default:
                        Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(RegisterTriggerEvent)}",
                            $"{kv.Key} is not a valid event name for the {nameof(Planet)}, so it will be ignored.");
                        break;
                }
            }
        }

        private void RemoveTriggerEvent(string modifierName)
        {
            _onPopBirth.Remove(modifierName);
        }

        private bool CommonCheckModifierCoreAddable(ModifierCore core, string targetPlayerName)
        {
            if (core.TargetType != TypeName)
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddModifier)}",
                    $"Modifier \"{core.Name}\" is not for {TypeName}, but for {core.TargetType}, so it will be ignored.");
                return false;
            }

            if (targetPlayerName.ToLower() == "global" && core.IsPlayerExclusive)
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddModifier)}",
                    $"Trying to add modifier \"{core.Name}\" as global which is player exclusive, so it will be ignored.");

                return false;
            }

            if (targetPlayerName.ToLower() == "global" || core.IsPlayerExclusive) return true;

            Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddModifier)}",
                $"Trying to add modifier \"{core.Name}\" as player exclusive which is global, so it will be ignored.");

            return false;
        }

        #endregion Modifier
    }
}
