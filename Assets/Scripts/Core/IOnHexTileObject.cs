using System;

namespace Core
{
    public interface IOnHexTileObject
    {
        Type Type { get; }

        string Name { get; }

        HexTileCoord HexCoord { get; }
    }
}