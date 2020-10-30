using MoonSharp.Interpreter;

namespace Core
{
    [MoonSharpUserData]
    public sealed class HexTile
    {
        public TileMap TileMap { get; }

        public HexTileCoord Coord { get; }

        public string Name { get; }

        public TileSpecialResourceType SpecialResource { get; }

        public int StarShipMovePoint => 1;

        public HexTile(TileMap tileMap, HexTileCoord coord, string name, TileSpecialResourceType specialResource)
        {
            TileMap = tileMap;
            Coord = coord;
            Name = name;
            SpecialResource = specialResource;
        }

        public bool HasTileObject(string typeName) => TileMap.IsTileObjectExists(typeName, Coord);

        public void AddTileObjectWithName(string typeName, string name) =>
            TileMap.AddTileObjectWithName(typeName, name, Coord);

        public void AddTileObject(IOnHexTileObject obj) => TileMap.AddTileObject(obj, Coord);

        public void RemoveTileObject(string typeName) => TileMap.RemoveTileObject(typeName, Coord);
    }
}
