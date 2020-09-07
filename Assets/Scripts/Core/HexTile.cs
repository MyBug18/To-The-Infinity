namespace Core
{
    public class HexTile
    {
        public HexTileCoord Coord { get; }

        public string Name { get; }

        public TileSpecialResourceType SpecialResource { get; }

        public HexTile(HexTileCoord coord, string name, TileSpecialResourceType specialResource)
        {
            Coord = coord;
            Name = name;
            SpecialResource = specialResource;
        }
    }
}