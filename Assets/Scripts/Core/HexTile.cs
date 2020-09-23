namespace Core
{
    public sealed class HexTile
    {
        public TileMap TileMap { get; }

        public HexTileCoord Coord { get; }

        public string Name { get; }

        public TileSpecialResourceType SpecialResource { get; }

        public HexTile(TileMap tileMap, HexTileCoord coord, string name, TileSpecialResourceType specialResource)
        {
            TileMap = tileMap;
            Coord = coord;
            Name = name;
            SpecialResource = specialResource;
        }
    }
}
