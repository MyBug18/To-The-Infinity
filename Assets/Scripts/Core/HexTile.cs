namespace Core
{
    public class HexTile
    {
        public readonly HexTileCoord Coord;

        public string TileBase { get; private set; }

        public string SpecialResource { get; private set; }

        public HexTile(HexTileCoord coord)
        {
            Coord = coord;
        }
    }
}