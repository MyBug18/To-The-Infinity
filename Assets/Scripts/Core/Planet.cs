using System.Collections.Generic;
using Core.GameData;

namespace Core
{
    public class Planet : ITileMapHolder, IOnHexTileObject
    {
        public string TileMapHolderType => nameof(Planet);

        public TileMap TileMap { get; }

        public string TypeName => nameof(Planet);

        public string Name { get; }

        public HexTileCoord HexCoord { get; }

        private readonly List<Modifier> _modifiers = new List<Modifier>();

        public IReadOnlyList<Modifier> Modifiers => _modifiers;

        private readonly Dictionary<ResourceInfoHolder, int> _fromModifiers = new Dictionary<ResourceInfoHolder, int>();

        /// <summary>
        /// 0 if totally uninhabitable,
        /// 1 if partially inhabitable with serious penalty,
        /// 2 if partially inhabitable with minor penalty,
        /// 3 if totally inhabitable without any penalty.
        /// </summary>
        public int InhabitableLevel { get; }

        public bool IsColonizing { get; private set; }

        private readonly List<Pop> _pops = new List<Pop>();

        private readonly Dictionary<ResourceInfoHolder, float> _planetaryResourceKeep =
            new Dictionary<ResourceInfoHolder, float>();

        public void AddModifierToTarget(string modifierName)
        {
            var modifier = GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName, this);

            AddModifier(modifier);
        }

        public void AddModifier(Modifier modifier)
        {
            _modifiers.Add(modifier);
        }

        public void RemoveModifier(Modifier modifier)
        {
            _modifiers.Remove(modifier);
        }

        public void AddModifierToTiles(List<HexTileCoord> coords, Modifier modifier)
        {
            _modifiers.Remove(modifier);
        }
    }
}
