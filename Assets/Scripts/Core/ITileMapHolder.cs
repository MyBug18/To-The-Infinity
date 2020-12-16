using System.Collections.Generic;

namespace Core
{
    public interface ITileMapHolder : IMultiPlayerModifierHolder
    {
        TileMap TileMap { get; }

        IEnumerable<TiledModifier> GetAllTiledModifiers(string targetPlayerName);

        IEnumerable<TiledModifier> GetTiledModifiersForTarget(IOnHexTileObject target);

        /// <summary>
        ///     Should not allow same modifier with different adder guid
        /// </summary>
        void AddTiledModifierRange(string targetPlayerName, string modifierName, IInfinityObject adder,
            string rangeKeyName, HashSet<HexTileCoord> tiles, int leftMonth);

        void MoveTiledModifierRange(string targetPlayerName, string modifierName, string rangeKeyName,
            HashSet<HexTileCoord> tiles);

        void RemoveTiledModifierRange(string targetPlayerName, string modifierName, string rangeKeyName);
    }
}
