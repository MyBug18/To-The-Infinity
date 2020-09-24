﻿using System;
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

        public IReadOnlyDictionary<ResourceInfoHolder, int> Cost => _core.Cost;

        public bool IsVisible => _core.IsVisible(_owner);

        public bool IsAvailable => _core.IsAvailable(_owner);

        public IReadOnlyCollection<HexTileCoord> AvailableTiles => _core.GetAvailableTiles(_owner);

        public bool DoAction(HexTileCoord coord)
        {
            if (!IsVisible || !IsAvailable ||
                NeedCoordinate && !AvailableTiles.Contains(coord)) return false;
            if (!_owner.CheckSpecialActionCost(_core.Cost)) return false;

            _owner.ConsumeSpecialActionCost(_core.Cost);
            _core.DoAction(_owner, coord);

            return true;
        }
    }

    public sealed class SpecialActionCore
    {
        public string Name { get; }

        public bool NeedCoordinate { get; }

        public Dictionary<ResourceInfoHolder, int> Cost { get; }

        private readonly Func<ISpecialActionHolder, bool> _visibleChecker;

        private readonly Func<ISpecialActionHolder, bool> _availableChecker;

        private readonly Func<ISpecialActionHolder, HashSet<HexTileCoord>> _availableCoordsGetter;

        private readonly Action<ISpecialActionHolder, HexTileCoord> _doAction;

        public SpecialActionCore(string name, bool needCoordinate,
            Dictionary<ResourceInfoHolder, int> cost,
            Func<ISpecialActionHolder, bool> visibleChecker,
            Func<ISpecialActionHolder, bool> availableChecker,
            Func<ISpecialActionHolder, HashSet<HexTileCoord>> availableCoordsGetter,
            Action<ISpecialActionHolder, HexTileCoord> doAction)
        {
            Name = name;
            NeedCoordinate = needCoordinate;
            Cost = cost;
            _visibleChecker = visibleChecker;
            _availableChecker = availableChecker;
            _availableCoordsGetter = availableCoordsGetter;
            _doAction = doAction;
        }

        public bool IsVisible(ISpecialActionHolder owner) => _visibleChecker(owner);

        public bool IsAvailable(ISpecialActionHolder owner) => _availableChecker(owner);

        public IReadOnlyCollection<HexTileCoord> GetAvailableTiles(ISpecialActionHolder owner) => _availableCoordsGetter(owner);

        public void DoAction(ISpecialActionHolder owner, HexTileCoord coord) => _doAction(owner, coord);
    }
}
