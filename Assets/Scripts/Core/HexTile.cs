namespace Core
{
    public class HexTile
    {
        public readonly HexTileCoord Coord;

        public string Name { get; private set; }

        public string SpecialResource { get; private set; }

        public HexTile(HexTileCoord coord, string name, string specialResource)
        {
            Coord = coord;
            Name = name;
            SpecialResource = specialResource;
        }
    }
}