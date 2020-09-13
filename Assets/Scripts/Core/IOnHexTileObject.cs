using System.Collections.Generic;

namespace Core
{
    public interface IOnHexTileObject
    {
        string TypeName { get; }

        string Name { get; }

        HexTileCoord HexCoord { get; }

        TileMap TileMap { get; }
    }

    public interface IMovableObject : IOnHexTileObject
    {
        int MovePoint { get; }

        List<HexTileCoord> GetMovableTiles();

        bool Move(HexTileCoord coord);
    }
}
