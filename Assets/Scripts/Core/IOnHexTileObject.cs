namespace Core
{
    public interface IOnHexTileObject
    {
        string TypeName { get; }

        string Name { get; }

        HexTileCoord HexCoord { get; }

        TileMap TileMap { get; }
    }
}
