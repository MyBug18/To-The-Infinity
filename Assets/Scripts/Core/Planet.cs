using System.Collections.Generic;

namespace Core
{
    public class Planet : ITileMapHolder, IOnHexTileObject
    {
        public string TileMapHolderType => nameof(Planet);

        public TileMap TileMap { get; }

        public string TypeName => nameof(Planet);

        public string Name { get; }

        public HexTileCoord HexCoord { get; }

        public IReadOnlyList<Modifier> Modifiers { get; }

        /// <summary>
        /// 0 if totally uninhabitable,
        /// 1 if partially inhabitable with serious penalty,
        /// 2 if partially inhabitable with minor penalty,
        /// 3 if totally inhabitable without any penalty.
        /// </summary>
        public int InhabitableLevel { get; }

        public bool IsColonizing { get; private set; }

        private readonly Dictionary<ResourceInfoHolder, float> _planetaryResourceKeep =
            new Dictionary<ResourceInfoHolder, float>();

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
