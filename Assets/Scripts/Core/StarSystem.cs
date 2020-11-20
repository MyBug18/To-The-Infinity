using System.Collections.Generic;
using System.Linq;
using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    [MoonSharpUserData]
    public sealed class StarSystem : ITileMapHolder
    {
        private readonly Dictionary<string, Dictionary<string, Modifier>> _playerModifierMap =
            new Dictionary<string, Dictionary<string, Modifier>>();

        private readonly Dictionary<string, Dictionary<string, TiledModifier>> _playerTiledModifierMap =
            new Dictionary<string, Dictionary<string, TiledModifier>>();

        public string TypeName => nameof(StarSystem);

        // No one can own StarSystem
        public IPlayer OwnPlayer => NoPlayer.Instance;

        public string Guid { get; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public TileMap TileMap { get; }

        public void StartNewTurn(int month)
        {
        }

        #region Modifier

        [MoonSharpHidden]
        public IEnumerable<Modifier> GetModifiers(string targetPlayerName)
        {
            // Should add Game modifier

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
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddModifier)}",
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
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddModifier)}",
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
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(RemoveModifier)}",
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
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddTiledModifierRange)}",
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
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddTiledModifierRange)}",
                    $"Modifier \"{modifierName}\" has already added by different object : \"{m.AdderObjectGuid}\" for {targetPlayerName}, so it will be ignored.");
                return;
            }

            if (m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddTiledModifierRange)}",
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
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(MoveTiledModifierRange)}",
                    $"Trying to access modifier \"{modifierName}\" which doesn't exist, so it will be ignored.");
                return;
            }

            if (!m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(MoveTiledModifierRange)}",
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
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(RemoveTiledModifierRange)}",
                    $"Trying to remove modifier \"{modifierName}\" which doesn't exist, so it will be ignored.");
                return;
            }

            if (!m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(RemoveTiledModifierRange)}",
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
                    default:
                        Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(RegisterTriggerEvent)}",
                            $"{kv.Key} is not a valid event name for the {nameof(StarSystem)}, so it will be ignored.");
                        break;
                }
            }
        }

        private void RemoveTriggerEvent(string modifierName)
        {
        }

        private bool CommonCheckModifierCoreAddable(ModifierCore core, string targetPlayerName)
        {
            if (core.TargetType != TypeName)
            {
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddModifier)}",
                    $"Modifier \"{core.Name}\" is not for {TypeName}, but for {core.TargetType}, so it will be ignored.");
                return false;
            }

            if (targetPlayerName.ToLower() == "global" && core.IsPlayerExclusive)
            {
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddModifier)}",
                    $"Trying to add modifier \"{core.Name}\" as global which is player exclusive, so it will be ignored.");

                return false;
            }

            if (targetPlayerName.ToLower() == "global" || core.IsPlayerExclusive) return true;

            Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddModifier)}",
                $"Trying to add modifier \"{core.Name}\" as player exclusive which is global, so it will be ignored.");

            return false;
        }

        #endregion Modifier
    }
}
