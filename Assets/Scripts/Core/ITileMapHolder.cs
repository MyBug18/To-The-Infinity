using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core
{
    public interface ITileMapHolder : IModifierHolder
    {
        TileMap TileMap { get; }

        [MoonSharpHidden]
        IEnumerable<TiledModifier> TiledModifiers { get; }

        /// <summary>
        /// Should not allow same modifier with different adder guid
        /// </summary>
        void AddTiledModifierRange(string modifierName, string adderGuid, string rangeKeyName, List<HexTileCoord> tiles, int leftMonth);

        void MoveTiledModifierRange(string modifierName, string rangeKeyName, List<HexTileCoord> tiles);

        void RemoveTiledModifierRange(string modifierName, string rangeKeyName);
    }
}
