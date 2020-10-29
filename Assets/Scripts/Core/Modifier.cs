using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public sealed class Modifier
    {
        public ModifierCore Core { get; }

        public string Name => Core.Name;

        public int LeftMonth { get; private set; }

        public bool IsPermanent => LeftMonth != -1;

        public string AdderGuid { get; }

        public Modifier(ModifierCore core, string adderGuid, int leftMonth = -1)
        {
            Core = core;
            AdderGuid = adderGuid;
            Core.SetAdder(AdderGuid);
            LeftMonth = leftMonth;
        }

        public bool IsRelated(string typeName) => Core.Scope.ContainsKey(typeName);

        public void ReduceLeftMonth(int month)
        {
            if (LeftMonth == -1) return;

            LeftMonth -= month;
        }
    }

    public sealed class TiledModifier
    {
        public ModifierCore Core { get; }

        public string Name => Core.Name;

        public string AdderGuid { get; }

        private readonly Dictionary<string, TiledModifierInfo> _infos = new Dictionary<string, TiledModifierInfo>();

        public IReadOnlyDictionary<string, TiledModifierInfo> Infos => _infos;

        public TiledModifier(ModifierCore core, string adderGuid, string rangeKey,
            HashSet<HexTileCoord> tiles, int leftMonth)
        {
            Core = core;
            AdderGuid = adderGuid;
            _infos[rangeKey] = new TiledModifierInfo(tiles, leftMonth);
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
        /// Returns pure added tile effect range
        /// </summary>
        public HashSet<HexTileCoord> AddTileInfo(string rangeKey,
            HashSet<HexTileCoord> tiles, int leftMonth)
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
        /// Returns removed range and newly affected range
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
        /// Returns pure removed tile effect range
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
    }

    public sealed class TiledModifierInfo
    {
        private readonly HashSet<HexTileCoord> _tiles;

        public IEnumerable<HexTileCoord> Tiles => _tiles;

        public int LeftMonth { get; private set; }

        public TiledModifierInfo(HashSet<HexTileCoord> tiles, int leftMonth)
        {
            _tiles = tiles;
            LeftMonth = leftMonth;
        }

        public bool IsPermanent => LeftMonth == -1;

        public void ReduceLeftMonth(int month)
        {
            if (IsPermanent) return;
            LeftMonth -= month;
        }
    }

    public sealed class ModifierCore : IEquatable<ModifierCore>
    {
        public string Name { get; }

        public string TargetType { get; }

        public bool IsTileLimited { get; }

        public string AdditionalDesc { get; }

        public IReadOnlyDictionary<string, ModifierScope> Scope { get; }

        public ModifierCore(string name, string targetType, bool isTileLimited, string additionalDesc,
            IReadOnlyDictionary<string, ModifierScope> scope)
        {
            Name = name;
            TargetType = targetType;
            IsTileLimited = isTileLimited;
            AdditionalDesc = additionalDesc;
            Scope = scope;
        }

        public override bool Equals(object obj) => obj is ModifierCore m && Equals(m);

        public bool Equals(ModifierCore m) => m != null && Name == m.Name;

        public override int GetHashCode() => Name.GetHashCode();

        public void SetAdder(string adderGuid)
        {
            foreach (var s in Scope.Values)
                s.SetInfo(adderGuid);
        }
    }

    public sealed class ModifierScope
    {
        private static readonly Action<IModifierHolder> DummyFunction = _ => { };

        public string TargetTypeName { get; }

        private string _adderGuid;

        private readonly Func<IModifierHolder, string, List<ModifierEffect>> _getEffect;

        private readonly Func<IModifierHolder, string, bool> _conditionChecker;

        public IReadOnlyDictionary<string, ScriptFunctionDelegate> TriggerEvent { get; }

        public readonly Action<IModifierHolder> OnAdded, OnRemoved;

        public ModifierScope(string targetTypeName,
            Func<IModifierHolder, string, List<ModifierEffect>> getEffect,
            Func<IModifierHolder, string, bool> conditionChecker,
            IReadOnlyDictionary<string, ScriptFunctionDelegate> triggerEvent)
        {
            TargetTypeName = targetTypeName;
            _getEffect = getEffect;
            _conditionChecker = conditionChecker;
            TriggerEvent = triggerEvent;

            OnAdded = TriggerEvent.TryGetValue("OnAdded", out var onAdded)
                ? target => onAdded.Invoke(target, _adderGuid)
                : DummyFunction;

            OnRemoved = TriggerEvent.TryGetValue("OnRemoved", out var onRemoved)
                ? target => onRemoved.Invoke(target, _adderGuid)
                : DummyFunction;
        }

        public void SetInfo(string adderGuid)
        {
            _adderGuid = adderGuid;
        }

        public bool CheckCondition(IModifierHolder holder) => _conditionChecker(holder, _adderGuid);

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierHolder holder) => _getEffect(holder, _adderGuid);
    }

    public readonly struct ModifierEffect
    {
        public string EffectInfo { get; }

        public IReadOnlyList<string> AdditionalInfos { get; }

        public int Amount { get; }

        public ModifierEffect(string effectInfo, IReadOnlyList<string> additionalInfos, int amount)
        {
            EffectInfo = effectInfo;
            AdditionalInfos = additionalInfos;
            Amount = amount;
        }
    }
}
