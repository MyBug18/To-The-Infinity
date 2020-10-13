using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public sealed class SpecialAction
    {
        private readonly SpecialActionCore _core;

        private readonly ISpecialActionHolder _owner;

        public SpecialAction(SpecialActionCore core, ISpecialActionHolder owner)
        {
            _core = core;
            _owner = owner;
        }

        public string Name => _core.Name;

        public bool NeedCoordinate => _core.NeedCoordinate;

        public IReadOnlyDictionary<string, int> GetCost(HexTileCoord coord) => _core.GetCost(_owner, coord);

        public bool IsAvailable => _core.IsAvailable(_owner);

        public IReadOnlyCollection<HexTileCoord> AvailableTiles => _core.GetAvailableTiles(_owner);

        public bool DoAction(HexTileCoord coord)
        {
            if (!IsAvailable) return false;
            if (NeedCoordinate && !AvailableTiles.Contains(coord)) return false;
            if (!_owner.CheckSpecialActionCost(GetCost(coord))) return false;

            _owner.ConsumeSpecialActionCost(GetCost(coord));
            _core.DoAction(_owner, coord);

            return true;
        }
    }

    public sealed class SpecialActionCore
    {
        public string Name { get; }

        public bool NeedCoordinate { get; }

        private readonly Func<ISpecialActionHolder, bool> _availableChecker;

        private readonly Func<ISpecialActionHolder, IReadOnlyCollection<HexTileCoord>> _availableCoordsGetter;

        private readonly Func<ISpecialActionHolder, HexTileCoord, IReadOnlyCollection<HexTileCoord>> _previewEffectRangeGetter;

        private readonly Func<ISpecialActionHolder, HexTileCoord, IReadOnlyDictionary<string, int>> _costGetter;

        private readonly Func<ISpecialActionHolder, HexTileCoord, bool> _doAction;

        public SpecialActionCore(string name, bool needCoordinate,
            Func<ISpecialActionHolder, bool> availableChecker,
            Func<ISpecialActionHolder, IReadOnlyCollection<HexTileCoord>> availableCoordsGetter,
            Func<ISpecialActionHolder, HexTileCoord, IReadOnlyCollection<HexTileCoord>> previewEffectRangeGetter,
            Func<ISpecialActionHolder, HexTileCoord, IReadOnlyDictionary<string, int>> costGetter,
            Func<ISpecialActionHolder, HexTileCoord, bool> doAction)
        {
            Name = name;
            NeedCoordinate = needCoordinate;
            _availableChecker = availableChecker;
            _availableCoordsGetter = availableCoordsGetter;
            _previewEffectRangeGetter = previewEffectRangeGetter;
            _costGetter = costGetter;
            _doAction = doAction;
        }

        public bool IsAvailable(ISpecialActionHolder owner) => _availableChecker(owner);

        public IReadOnlyCollection<HexTileCoord> GetAvailableTiles(ISpecialActionHolder owner) =>
            _availableCoordsGetter(owner);

        public IReadOnlyCollection<HexTileCoord> PreviewEffectRange(ISpecialActionHolder owner, HexTileCoord coord) =>
            _previewEffectRangeGetter(owner, coord);

        public IReadOnlyDictionary<string, int> GetCost(ISpecialActionHolder owner, HexTileCoord coord) =>
            _costGetter(owner, coord);

        public bool DoAction(ISpecialActionHolder owner, HexTileCoord coord) => _doAction(owner, coord);
    }
}
