using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public interface ISpecialActionHolder : ITypeNameHolder
    {
        IReadOnlyList<SpecialAction> SpecialActions { get; }

        bool CheckSpecialActionCost(IReadOnlyDictionary<ResourceInfoHolder, int> cost);

        void ConsumeSpecialActionCost(IReadOnlyDictionary<ResourceInfoHolder, int> cost);
    }

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

        public IReadOnlyDictionary<ResourceInfoHolder, int> GetCost(HexTileCoord coord) => _core.GetCost(_owner, coord);

        public bool IsVisible => _core.IsVisible(_owner);

        public bool IsAvailable => _core.IsAvailable(_owner);

        public IReadOnlyCollection<HexTileCoord> AvailableTiles => _core.GetAvailableTiles(_owner);

        public bool DoAction(HexTileCoord coord)
        {
            if (!IsVisible) return false;
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

        private readonly Func<ISpecialActionHolder, bool> _visibleChecker;

        private readonly Func<ISpecialActionHolder, bool> _availableChecker;

        private readonly Func<ISpecialActionHolder, IReadOnlyCollection<HexTileCoord>> _availableCoordsGetter;

        private readonly Func<ISpecialActionHolder, HexTileCoord, IReadOnlyCollection<HexTileCoord>> _effectRangeGetter;

        private readonly Func<ISpecialActionHolder, HexTileCoord, IReadOnlyDictionary<ResourceInfoHolder, int>> _costGetter;

        private readonly Action<ISpecialActionHolder, HexTileCoord> _doAction;

        public SpecialActionCore(string name, bool needCoordinate,
            Func<ISpecialActionHolder, bool> visibleChecker,
            Func<ISpecialActionHolder, bool> availableChecker,
            Func<ISpecialActionHolder, IReadOnlyCollection<HexTileCoord>> availableCoordsGetter,
            Func<ISpecialActionHolder, HexTileCoord, IReadOnlyCollection<HexTileCoord>> effectRangeGetter,
            Func<ISpecialActionHolder, HexTileCoord, IReadOnlyDictionary<ResourceInfoHolder, int>> costGetter,
            Action<ISpecialActionHolder, HexTileCoord> doAction)
        {
            Name = name;
            NeedCoordinate = needCoordinate;
            _visibleChecker = visibleChecker;
            _availableChecker = availableChecker;
            _availableCoordsGetter = availableCoordsGetter;
            _effectRangeGetter = effectRangeGetter;
            _costGetter = costGetter;
            _doAction = doAction;
        }

        public bool IsVisible(ISpecialActionHolder owner) => _visibleChecker(owner);

        public bool IsAvailable(ISpecialActionHolder owner) => _availableChecker(owner);

        public IReadOnlyCollection<HexTileCoord> GetAvailableTiles(ISpecialActionHolder owner) =>
            _availableCoordsGetter(owner);

        public IReadOnlyCollection<HexTileCoord> GetEffectRange(ISpecialActionHolder owner, HexTileCoord coord) =>
            _effectRangeGetter(owner, coord);

        public IReadOnlyDictionary<ResourceInfoHolder, int> GetCost(ISpecialActionHolder owner, HexTileCoord coord) =>
            _costGetter(owner, coord);

        public void DoAction(ISpecialActionHolder owner, HexTileCoord coord) => _doAction(owner, coord);
    }
}
