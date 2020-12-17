using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.GameData;
using MoonSharp.Interpreter;

namespace Core
{
    [MoonSharpUserData]
    public sealed class Planet : ITileMapHolder, IOnHexTileObject
    {
        private static readonly HashSet<TriggerEventType> RelativeTriggerEventTypes = new HashSet<TriggerEventType>
        {
            TriggerEventType.OnPopBirth,
        };

        private readonly Dictionary<TriggerEventType, Dictionary<string, TriggerEvent>> _triggerEvents =
            new Dictionary<TriggerEventType, Dictionary<string, TriggerEvent>>();

        /// <summary>
        ///     0 if totally uninhabitable,
        ///     1 if partially inhabitable with serious penalty,
        ///     2 if partially inhabitable with minor penalty,
        ///     3 if totally inhabitable without any penalty.
        /// </summary>
        public int InhabitableLevel { get; }

        public bool IsColonizing { get; private set; }

        public string IdentifierName { get; }

        public string CustomName { get; private set; }

        public HexTile CurrentTile { get; private set; }

        public bool IsDestroyed { get; private set; }

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
            var toAdd = new HashSet<IModifier>(destHolder.GetAllTiledModifiers(OwnPlayer.PlayerName));

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

            // Remove modifier effect before detaching
            foreach (var m in toRemove)
                ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, true, false);
            CurrentTile.RemoveTileObject(TypeName);

            CurrentTile = tile;

            // Add modifier effect after attaching
            tile.AddTileObject(this);
            foreach (var m in toAdd)
                ApplyModifierChangeToDownward(OwnPlayer.PlayerName, m, false, false);
        }

        public void DestroySelf()
        {
            IsDestroyed = true;
            throw new NotImplementedException();
        }

        public string TypeName => nameof(Planet);

        public IPlayer OwnPlayer { get; }

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
            if (IsDestroyed) return null;

            var result = new Dictionary<string, object>
            {
                ["Id"] = Id,
                ["IdentifierName"] = IdentifierName,
                ["CustomName"] = CustomName,
                ["Storage"] = Storage.Data,
                ["OwnPlayer"] = OwnPlayer.Id,
                ["Modifiers"] = _playerModifierMap.ToDictionary(x => x.Key,
                    x => x.Value.Values.Select(y => y.ToSaveData()).ToList()),
                ["TiledModifiers"] = _playerTiledModifierMap.ToDictionary(x => x.Key,
                    x => x.Value.Values.Select(y => y.ToSaveData()).ToList()),
                ["SpecialActions"] = _specialActions.Keys.ToArray(),
                ["TileMap"] = TileMap.ToSaveData(),
            };

            return new InfinityObjectData(TypeName, result);
        }

        #region Resources

        private readonly Dictionary<string, float> _planetaryResourceKeep =
            new Dictionary<string, float>();

        public IReadOnlyDictionary<string, float> PlanetaryResourceKeep => _planetaryResourceKeep;

        #endregion

        #region Pop

        private readonly List<Pop> _pops = new List<Pop>();

        public IReadOnlyList<Pop> Pops => _pops;

        private readonly List<Pop> _unemployedPops = new List<Pop>();

        public IReadOnlyList<Pop> UnemployedPops => _unemployedPops;

        public const float BasePopGrowth = 5.0f;

        #endregion Pop

        #region SpecialAction

        private readonly Dictionary<string, SpecialAction> _specialActions = new Dictionary<string, SpecialAction>();

        public IEnumerable<SpecialAction> SpecialActions => _specialActions.Values;

        public void AddSpecialAction(string name)
        {
            if (_specialActions.ContainsKey(name)) return;

            _specialActions[name] = SpecialActionData.Instance
                .GetSpecialActionDirectly(this, name);
        }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost) =>
            throw new NotImplementedException();

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Modifier

        private readonly Dictionary<string, Dictionary<string, Modifier>> _playerModifierMap =
            new Dictionary<string, Dictionary<string, Modifier>>();

        private readonly Dictionary<string, Dictionary<string, TiledModifier>> _playerTiledModifierMap =
            new Dictionary<string, Dictionary<string, TiledModifier>>();

        public IEnumerable<TiledModifier> AffectedTiledModifiers =>
            CurrentTile.TileMap.Holder.GetTiledModifiersForTarget(this);

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
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(RemoveModifier)}",
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
        public void ApplyModifierChangeToDownward(string targetPlayerName, IModifier m,
            bool isRemoving, bool isFromSaveData)
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

        public bool HasModifier(string targetPlayerName, string modifierName) =>
            _playerModifierMap.TryGetValue(targetPlayerName, out var modifiers) && modifiers.ContainsKey(modifierName);

        public void AddTiledModifierRange(string targetPlayerName, string modifierName, IInfinityObject adder,
            string rangeKeyName, HashSet<HexTileCoord> tiles, int leftWeek)
            => AddTiledModifierRange(targetPlayerName, modifierName, adder, rangeKeyName, tiles, leftWeek, false);

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

            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, false, false, pureAdd);
            TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, true, false, pureRemove);
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
                m = new TiledModifier(core, adder.Id, rangeKeyName, tiles, leftWeek);
                TileMap.ApplyModifierChangeToTileObjects(targetPlayerName, m, false, isFromSaveData, tiles);
                return;
            }

            if (m.AdderObjectId != adder.Id)
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddTiledModifierRange)}",
                    $"Modifier \"{modifierName}\" has already added by different object : \"{m.AdderObjectId}\" for {targetPlayerName}, so it will be ignored.");
                return;
            }

            if (m.Infos.ContainsKey(rangeKeyName))
            {
                Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(AddTiledModifierRange)}",
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
                    Logger.Log(LogType.Warning, $"{nameof(Planet)}.{nameof(RegisterTriggerEvent)}",
                        $"{kv.Key} is not a valid event name for the {nameof(Planet)}, so it will be ignored.");
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

            foreach (var m in GetModifiers(OwnPlayer.PlayerName).Cast<IModifier>().Concat(AffectedTiledModifiers))
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
