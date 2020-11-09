using System.Collections.Generic;

namespace Core
{
    public interface IOnHexTileObject : IModifierHolder, ISpecialActionHolder
    {
        string IdentifierName { get; }

        string CustomName { get; }

        HexTile CurrentTile { get; }

        IEnumerable<TiledModifier> AffectedTiledModifiers { get; }

        void StartNewTurn(int month);

        void TeleportToTile(HexTile tile);
    }
}
