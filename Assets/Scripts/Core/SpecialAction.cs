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

        public bool IsAvailable => _core.IsAvailable(_owner);

        public IReadOnlyCollection<HexTileCoord> AvailableTiles => _core.GetAvailableTiles(_owner);

        public IReadOnlyDictionary<string, int> GetCost(HexTileCoord coord) => _core.GetCost(_owner, coord);

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
}
