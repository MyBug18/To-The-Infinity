namespace Core
{
    public class HexTile
    {
        public readonly HexTileCoord Coord;

        public readonly string Name;

        public readonly TileSpecialResourceType SpecialResource;

        public HexTile(HexTileCoord coord, string name, TileSpecialResourceType specialResource)
        {
            Coord = coord;
            Name = name;
            SpecialResource = specialResource;
        }
    }
}