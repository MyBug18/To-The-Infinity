using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public sealed class TiledModifier : IModifier
    {
        private readonly ModifierCore _core;

        private readonly Dictionary<string, TiledModifierInfo> _infos = new Dictionary<string, TiledModifierInfo>();

        public TiledModifier(ModifierCore core, int adderObjectId, string rangeKey,
            HashSet<HexTileCoord> tiles, int leftMonth)
        {
            _core = core;
            AdderObjectId = adderObjectId;
            _infos[rangeKey] = new TiledModifierInfo(tiles, leftMonth);
        }

        public int AdderObjectId { get; }

        public IReadOnlyDictionary<string, TiledModifierInfo> Infos => _infos;

        public string Name => _core.Name;

        public IReadOnlyDictionary<TriggerEventType, TriggerEvent> GetTriggerEvent(IModifierEffectHolder target)
        {
            var result = new Dictionary<TriggerEventType, TriggerEvent>();

            if (!_core.Scope.TryGetValue(target.TypeName, out var scope)) return result;

            foreach (var kv in scope.TriggerEvent)
            {
                if (!Enum.TryParse(kv.Key, out TriggerEventType type))
                {
                    Logger.Log(LogType.Warning, Name,
                        $"{kv.Key} is not a valid TriggerEvent type, so it will be ignored.");
                    continue;
                }

                if (type == TriggerEventType.OnAdded || type == TriggerEventType.OnRemoved) continue;

                var priority = scope.TriggerEventPriority.TryGetValue(kv.Key, out var value) ? value : 0;

                result[type] = new TriggerEvent(Name, type, kv.Value, target, AdderObjectId, priority);
            }

            return result;
        }

        public void OnRemoved(IModifierEffectHolder target) => _core.OnRemoved(target, AdderObjectId);

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierEffectHolder target) =>
            _core.GetEffects(target, AdderObjectId);

        public object ToSaveData()
        {
            var infos = _infos.Select(kv => new Dictionary<string, object>
            {
                ["RangeKey"] = kv.Key,
                ["LeftMonth"] = kv.Value.LeftMonth,
                ["Tiles"] = kv.Value.Tiles.ToList(),
            }).ToList();

            var result = new Dictionary<string, object>
            {
                ["Name"] = Name,
                ["AdderObjectId"] = AdderObjectId,
                ["Infos"] = infos,
            };

            return result;
        }

        public HashSet<HexTileCoord> ReduceLeftMonth(int month)
        {
            var removedRange = new HashSet<HexTileCoord>();
            var toRemove = new List<string>();

            foreach (var kv in _infos.Where(kv => !kv.Value.IsPermanent))
            {
                if (kv.Value.LeftMonth - month <= 0)
                {
                    toRemove.Add(kv.Key);
                    foreach (var t in kv.Value.Tiles)
                        removedRange.Add(t);
                    continue;
                }

                kv.Value.ReduceLeftMonth(month);
            }

            foreach (var n in toRemove)
                _infos.Remove(n);

            return removedRange;
        }

        /// <summary>
        ///     Returns pure added tile effect range
        /// </summary>
        public HashSet<HexTileCoord> AddTileInfo(string rangeKey, HashSet<HexTileCoord> tiles, int leftMonth)
        {
            if (_infos.ContainsKey(rangeKey)) return new HashSet<HexTileCoord>();

            var result = new HashSet<HexTileCoord>(
                from t in tiles
                where _infos.Values.All(info => !info.Tiles.Contains(t))
                select t);

            _infos[rangeKey] = new TiledModifierInfo(tiles, leftMonth);

            return result;
        }

        /// <summary>
        ///     Returns removed range and newly affected range
        /// </summary>
        /// <returns></returns>
        public (HashSet<HexTileCoord> removed, HashSet<HexTileCoord> added) MoveTileInfo(
            string rangeKey, HashSet<HexTileCoord> newTiles)
        {
            if (!_infos.ContainsKey(rangeKey)) return (new HashSet<HexTileCoord>(), new HashSet<HexTileCoord>());

            var pureAdded = new HashSet<HexTileCoord>(
                from t in newTiles
                where _infos.Values.All(info => !info.Tiles.Contains(t))
                select t);

            var originalTile = _infos[rangeKey].Tiles;
            _infos[rangeKey] = new TiledModifierInfo(newTiles, _infos[rangeKey].LeftMonth);

            var pureRemoved = new HashSet<HexTileCoord>(
                from t in originalTile
                where _infos.Values.All(info => !info.Tiles.Contains(t))
                select t);

            return (pureRemoved, pureAdded);
        }

        /// <summary>
        ///     Returns pure removed tile effect range
        /// </summary>
        public HashSet<HexTileCoord> RemoveTileInfo(string rangeKey)
        {
            if (!_infos.TryGetValue(rangeKey, out var toRemove)) return new HashSet<HexTileCoord>();

            var toRemoveTile = toRemove.Tiles;
            _infos.Remove(rangeKey);

            var result = new HashSet<HexTileCoord>(
                from t in toRemoveTile
                where _infos.Values.All(info => !info.Tiles.Contains(t))
                select t);

            return result;
        }

        public bool IsInRange(HexTileCoord coord) => _infos.Values.Any(info => info.Tiles.Contains(coord));

        public override bool Equals(object obj) => obj is TiledModifier m && _core == m._core;

        public override int GetHashCode() => _core.GetHashCode();
    }

    public sealed class TiledModifierInfo
    {
        private readonly HashSet<HexTileCoord> _tiles;

        public TiledModifierInfo(HashSet<HexTileCoord> tiles, int leftMonth)
        {
            _tiles = tiles;
            LeftMonth = leftMonth;
        }

        public IEnumerable<HexTileCoord> Tiles => _tiles;

        public int LeftMonth { get; private set; }

        public bool IsPermanent => LeftMonth == -1;

        public void ReduceLeftMonth(int month)
        {
            if (IsPermanent) return;
            LeftMonth -= month;
        }
    }
}
