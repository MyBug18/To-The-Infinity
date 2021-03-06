﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    [MoonSharpUserData]
    public sealed class StarSystem : ITileMapHolder
    {
        private static readonly HashSet<TriggerEventType> RelativeTriggerEventTypes = new HashSet<TriggerEventType>();

        private readonly Dictionary<TriggerEventType, Dictionary<string, TriggerEvent>> _triggerEvents =
            new Dictionary<TriggerEventType, Dictionary<string, TriggerEvent>>();

        public string IdentifierName { get; }

        public string TypeName => nameof(StarSystem);

        // No one can own StarSystem
        public IPlayer OwnPlayer => NoPlayer.Instance;

        public int Id { get; set; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public TileMap TileMap { get; }

        public void StartNewTurn(int week)
        {
            ReduceModifiersLeftWeek(week);
            TileMap.StartNewTurn(week);
        }

        [MoonSharpHidden]
        public InfinityObjectData Save()
        {
            var result = new Dictionary<string, object>
            {
                ["Id"] = Id,
                ["IdentifierName"] = IdentifierName,
                ["Storage"] = Storage.Data,
                ["Modifiers"] = _playerModifierMap.ToDictionary(x => x.Key,
                    x => x.Value.Values.Select(y => y.ToSaveData()).ToList()),
                ["TiledModifiers"] = _playerTiledModifierMap.ToDictionary(x => x.Key,
                    x => x.Value.Values.Select(y => y.ToSaveData()).ToList()),
                ["TileMap"] = TileMap.ToSaveData(),
            };

            return new InfinityObjectData(TypeName, result);
        }

        #region Modifier

        private readonly Dictionary<string, Dictionary<string, Modifier>> _playerModifierMap =
            new Dictionary<string, Dictionary<string, Modifier>>();

        private readonly Dictionary<string, Dictionary<string, TiledModifier>> _playerTiledModifierMap =
            new Dictionary<string, Dictionary<string, TiledModifier>>();

        public bool HasModifier(string targetPlayerName, string modifierName) =>
            _playerModifierMap.TryGetValue(targetPlayerName, out var modifiers) && modifiers.ContainsKey(modifierName);

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
        public IEnumerable<TiledModifier> GetAllTiledModifiers(string targetPlayerName)
        {
            if (_playerTiledModifierMap.TryGetValue("Global", out var globalModifiers))
                foreach (var m in globalModifiers.Values)
                    yield return m;

            if (!_playerTiledModifierMap.TryGetValue(targetPlayerName, out var playerModifiers)) yield break;

            foreach (var m in playerModifiers.Values)
                yield return m;
        }

        [MoonSharpHidden]
        public IEnumerable<TiledModifier> GetTiledModifiersForTarget(IOnHexTileObject target)
            => GetAllTiledModifiers(target.OwnPlayer.PlayerName).Where(x => x.IsInRange(target.CurrentTile.Coord));

        public void AddModifier(string targetPlayerName, string modifierName, IInfinityObject adder, int leftWeek)
            => AddModifier(targetPlayerName, modifierName, adder, leftWeek, false);

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

            ApplyModifierChangeToDownward(targetPlayerName, m, true, false);
        }

        [MoonSharpHidden]
        public void ApplyModifierChangeToDownward(string targetPlayerName, IModifier m, bool isRemoving, bool isFromSaveData)
        {
            if (targetPlayerName.ToLower() != "global" && targetPlayerName != OwnPlayer.PlayerName)
            {
                TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, isRemoving, isFromSaveData, null);
                return;
            }

            // Tiled modifier cannot affect the target type itself.
            if (m is TiledModifier && m.TargetType == TypeName)
            {
                TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, isRemoving, isFromSaveData, null);
                return;
            }

            if (isRemoving)
            {
                m.OnRemoved(this);

                RemoveTriggerEvent(m.Name);
            }
            else
            {
                if (!isFromSaveData)
                    m.OnAdded(this);

                RegisterTriggerEvent(m.Name, m.GetTriggerEvent(this));
            }

            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, isRemoving, isFromSaveData, null);
        }

        public void AddTiledModifierRange(string targetPlayerName, string modifierName, IInfinityObject adder,
            string rangeKeyName, HashSet<HexTileCoord> tiles, int leftWeek)
            => AddTiledModifierRange(targetPlayerName, modifierName, adder, rangeKeyName, tiles, leftWeek, false);

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

            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, false, false, pureAdd);
            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, true, false, pureRemove);
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

            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, true, false, pureRemove);

            if (m.Infos.Count == 0)
                modifiers.Remove(modifierName);

            if (modifiers.Count == 0)
                _playerTiledModifierMap.Remove(targetPlayerName);
        }

        private void AddModifier(string targetPlayerName, string modifierName, IInfinityObject adder, int leftWeek, bool isFromSaveData)
        {
            var core = ModifierData.Instance.GetModifierDirectly(modifierName);

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

            var m = new Modifier(core, adder.Id, leftWeek);

            modifiers[modifierName] = m;

            ApplyModifierChangeToDownward(targetPlayerName, m, false, isFromSaveData);
        }

        private void AddTiledModifierRange(string targetPlayerName, string modifierName, IInfinityObject adder,
            string rangeKeyName, HashSet<HexTileCoord> tiles, int leftWeek, bool isFromSaveData)
        {
            var core = ModifierData.Instance.GetModifierDirectly(modifierName);

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
                m = new TiledModifier(core, adder.Id, rangeKeyName, tiles, leftWeek);
                TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, false, isFromSaveData, tiles);
                return;
            }

            if (m.AdderObjectId != adder.Id)
            {
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddTiledModifierRange)}",
                    $"Modifier \"{modifierName}\" has already added by different object : \"{m.AdderObjectId}\" for {targetPlayerName}, so it will be ignored.");
                return;
            }

            if (m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(AddTiledModifierRange)}",
                    $"Range key name \"{rangeKeyName}\" already exists in modifier \"{m.Name}\" for {targetPlayerName}, so it will be ignored.");
                return;
            }

            var pureAdd = m.AddTileInfo(rangeKeyName, tiles, leftWeek);

            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, false, isFromSaveData, pureAdd);
        }

        private void ReduceModifiersLeftWeek(int week)
        {
            var toRemoveList = new List<string>();

            foreach (var kv in _playerModifierMap)
            {
                var modifiers = kv.Value;

                foreach (var m in modifiers.Values.Where(m => !m.IsPermanent))
                {
                    if (m.LeftWeek - week <= 0)
                    {
                        toRemoveList.Add(m.Name);
                        continue;
                    }

                    m.ReduceLeftWeek(week);
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
                    var pureRemove = m.ReduceLeftWeek(week);

                    if (m.Infos.Count == 0)
                        toRemoveList.Add(m.Name);

                    TileMap.ApplyModifierChangeToTileObjects(kv.Key, m, true, false, pureRemove);
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

        private void RegisterTriggerEvent(string modifierName,
            IReadOnlyDictionary<TriggerEventType, TriggerEvent> events)
        {
            foreach (var kv in events)
            {
                var type = kv.Key;

                if (!RelativeTriggerEventTypes.Contains(type))
                {
                    Logger.Log(LogType.Warning, $"{nameof(StarSystem)}.{nameof(RegisterTriggerEvent)}",
                        $"{kv.Key} is not a valid event name for the {nameof(StarSystem)}, so it will be ignored.");
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

            TileMap.StartCachingModifierEffects();
        }

        private void CacheModifierEffect()
        {
            _modifierEffectsMap.Clear();

            foreach (var m in GetModifiers(OwnPlayer.PlayerName))
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

            _isCachingModifierEffect = false;
        }

        private IReadOnlyList<ModifierEffect> ApplyModifierEffects(IEnumerable<ModifierEffect> effects)
        {
            var result = new List<ModifierEffect>();

            foreach (var e in effects)
            {
                var infos = e.AdditionalInfos;
                var amount = e.Amount;
                switch (e.EffectType)
                {
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
    }
}
