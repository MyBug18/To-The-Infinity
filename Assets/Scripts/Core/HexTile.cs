namespace Core
{
    public class HexTile
    {
        public readonly HexTileCoord Coord;

        public string TileBaseType { get; private set; }

        public string TileAdditionalType { get; private set; }

        public string SpecialResourceType { get; private set; }

        public HexTile(HexTileCoord coord)
        {
            Coord = coord;
        }
    }
}