using MoonSharp.Interpreter;

namespace Core
{
    [MoonSharpUserData]
    public sealed class HexTile
    {
        public HexTile(TileMap tileMap, HexTileCoord coord, string name, TileSpecialResourceType specialResource)
        {
            TileMap = tileMap;
            Coord = coord;
            Name = name;
            SpecialResource = specialResource;
        }

        public TileMap TileMap { get; }

        public HexTileCoord Coord { get; }

        public string Name { get; }

        public TileSpecialResourceType SpecialResource { get; }

        public int UnitMoveCost => 1;

        public bool HasTileObject(string typeName) => TileMap.IsTileObjectExists(typeName, Coord);

        public IOnHexTileObject AddTileObjectWithName(string typeName, string name) =>
            TileMap.AddTileObjectWithName(typeName, name, Coord);

        public void AddTileObject(IOnHexTileObject obj)
        {
            TileMap.AddTileObject(obj, Coord);
        }

        public void RemoveTileObject(string typeName)
        {
            TileMap.RemoveTileObject(typeName, Coord);
        }
    }
}
