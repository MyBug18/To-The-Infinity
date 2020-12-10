using System.Collections.Generic;

namespace Core
{
    public interface IOnHexTileObject : IModifierEffectHolder, ISpecialActionHolder
    {
        string IdentifierName { get; }

        string CustomName { get; }

        HexTile CurrentTile { get; }

        IEnumerable<TiledModifier> AffectedTiledModifiers { get; }

        void TeleportToTile(HexTile tile);

        bool IsDestroyed { get; }

        void DestroySelf();
    }
}
