using System.Collections.Generic;

namespace Core
{
    public class StarSystem : ITileMapHolder
    {
        public string TileMapHolderType => nameof(StarSystem);

        public TileMap TileMap { get; }

        public IReadOnlyList<Modifier> Modifiers { get; }

        public void AddModifierToTarget(string modifierName)
        {
            throw new System.NotImplementedException();
        }

        public void AddModifier(Modifier modifier)
        {
            throw new System.NotImplementedException();
        }

        public void AddModifierToTiles(List<HexTileCoord> coords, Modifier modifier)
        {
            throw new System.NotImplementedException();
        }
    }
}
