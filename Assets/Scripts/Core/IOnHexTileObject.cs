using System.Collections.Generic;

namespace Core
{
    public interface IOnHexTileObject : IModifierHolder, ISpecialActionHolder, IEventHolder
    {
        string Owner { get; }

        string Name { get; }

        HexTile Tile { get; }

        void StartNewTurn(int month);
    }
}
