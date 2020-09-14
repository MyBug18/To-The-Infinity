﻿using System.Collections.Generic;

namespace Core
{
    public interface IOnHexTileObject : IModifierHolder, ISpecialActionHolder
    {
        string TypeName { get; }

        string Name { get; }

        HexTile Tile { get; }

        void StartNewTurn(int month);
    }

    public interface IMovableObject : IOnHexTileObject
    {
        int MovePoint { get; }

        List<HexTileCoord> GetMovableTiles();

        bool Move(HexTileCoord coord);
    }
}
