using System;
using System.Collections.Generic;

namespace Core
{
    public readonly struct SpecialAction
    {
        private readonly SpecialActionCore _core;

        private readonly IOnHexTileObject _owner;

        public string Name => _core.Name;

        public bool NeedCoordinate => _core.NeedCoordinate;

        public bool IsActivatedThisTurn { get; }

        public bool IsVisible => _core.IsVisible(_owner);

        public bool IsAvailable => !IsActivatedThisTurn && _core.IsAvailable(_owner);

        public List<HexTileCoord> GetAvailableTiles => _core.GetAvailableTiles(_owner);

        public bool DoAction(HexTileCoord coord)
        {
            if (!_core.IsVisible(_owner) || !IsAvailable || !GetAvailableTiles.Contains(coord)) return false;

            return _core.DoAction(_owner, coord);
        }
    }

    public class SpecialActionCore
    {
        public string Name { get; }

        public bool NeedCoordinate { get; }

        private readonly Func<IOnHexTileObject, bool> _visibleChecker;

        private readonly Func<IOnHexTileObject, bool> _availableChecker;

        private readonly Func<IOnHexTileObject, List<HexTileCoord>> _availableCoordsGetter;

        private readonly Func<IOnHexTileObject, HexTileCoord, bool> _doAction;

        public SpecialActionCore(string name, bool needCoordinate,
            Func<IOnHexTileObject, bool> visibleChecker,
            Func<IOnHexTileObject, bool> availableChecker,
            Func<IOnHexTileObject, List<HexTileCoord>> availableCoordsGetter,
            Func<IOnHexTileObject, HexTileCoord, bool> doAction)
        {
            Name = name;
            NeedCoordinate = needCoordinate;
            _visibleChecker = visibleChecker;
            _availableChecker = availableChecker;
            _availableCoordsGetter = availableCoordsGetter;
            _doAction = doAction;
        }

        public bool IsVisible(IOnHexTileObject owner) => _visibleChecker(owner);

        public bool IsAvailable(IOnHexTileObject owner) => _availableChecker(owner);

        public List<HexTileCoord> GetAvailableTiles(IOnHexTileObject owner) => _availableCoordsGetter(owner);

        public bool DoAction(IOnHexTileObject owner, HexTileCoord coord) => _doAction(owner, coord);
    }
}
