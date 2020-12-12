using System.Collections.Generic;

namespace Core
{
    public interface IOnHexTileObject : IModifierEffectHolder, ISpecialActionHolder
    {
        string IdentifierName { get; }

        string CustomName { get; }

        HexTile CurrentTile { get; }

        IEnumerable<TiledModifier> AffectedTiledModifiers { get; }

        bool IsDestroyed { get; }

        void TeleportToTile(HexTile tile);

        void DestroySelf();
    }
}
