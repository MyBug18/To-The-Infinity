using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core
{
    public sealed class SpecialActionCore
    {
        private readonly ScriptFunctionDelegate<bool> _doAction;

        private readonly ScriptFunctionDelegate<HashSet<HexTileCoord>> _getAvailableTiles;

        private readonly ScriptFunctionDelegate<Dictionary<string, int>> _getCost;

        private readonly ScriptFunctionDelegate<bool> _isAvailable;

        private readonly ScriptFunctionDelegate<HashSet<HexTileCoord>> _previewEffectRange;

        public SpecialActionCore(string name, string targetType, bool needCoordinate,
            ScriptFunctionDelegate<bool> isAvailable,
            ScriptFunctionDelegate<HashSet<HexTileCoord>> getAvailableTiles,
            ScriptFunctionDelegate<HashSet<HexTileCoord>> previewEffectRange,
            ScriptFunctionDelegate<Dictionary<string, int>> getCost,
            ScriptFunctionDelegate<bool> doAction)
        {
            Name = name;
            TargetType = targetType;
            NeedCoordinate = needCoordinate;
            _isAvailable = isAvailable;
            _getAvailableTiles = getAvailableTiles;
            _previewEffectRange = previewEffectRange;
            _getCost = getCost;
            _doAction = doAction;
        }

        public string Name { get; }

        public string TargetType { get; }

        public bool NeedCoordinate { get; }

        public bool IsAvailable(ISpecialActionHolder owner) =>
            _isAvailable.TryInvoke("AvailableChecker", Name, out var result, owner) && result;

        public IReadOnlyCollection<HexTileCoord> GetAvailableTiles(ISpecialActionHolder owner)
            => _getAvailableTiles.TryInvoke("GetAvailableTiles", Name, out var result, owner)
                ? result
                : new HashSet<HexTileCoord>();

        public IReadOnlyCollection<HexTileCoord> PreviewEffectRange(ISpecialActionHolder owner, HexTileCoord coord)
            => _previewEffectRange.TryInvoke("PreviewEffectRange", Name, out var result, owner, coord)
                ? result
                : new HashSet<HexTileCoord>();

        public IReadOnlyDictionary<string, int> GetCost(ISpecialActionHolder owner, HexTileCoord coord)
            => _getCost.TryInvoke("GetCost", Name, out var result, owner, coord)
                ? result
                : new Dictionary<string, int>();

        public bool DoAction(ISpecialActionHolder owner, HexTileCoord coord)
            => _doAction.TryInvoke("DoAction", Name, out var result, owner, coord) && result;
    }
}
