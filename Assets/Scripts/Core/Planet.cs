using System.Collections.Generic;
using Core.GameData;

namespace Core
{
    public class Planet : ITileMapHolder, IOnHexTileObject
    {
        public string HolderType => nameof(Planet);

        public TileMap TileMap { get; }

        public IReadOnlyList<SpecialAction> SpecialActions { get; }

        public string TypeName => nameof(Planet);

        public string Name { get; }

        public HexTileCoord HexCoord { get; }

        private readonly List<Modifier> _modifiers = new List<Modifier>();

        public IReadOnlyList<Modifier> Modifiers => _modifiers;

        public IReadOnlyDictionary<ResourceInfoHolder, int> ModifierEffect { get; private set; }

        /// <summary>
        /// 0 if totally uninhabitable,
        /// 1 if partially inhabitable with serious penalty,
        /// 2 if partially inhabitable with minor penalty,
        /// 3 if totally inhabitable without any penalty.
        /// </summary>
        public int InhabitableLevel { get; }

        public bool IsColonizing { get; private set; }

        private readonly List<Pop> _pops = new List<Pop>();

        public IReadOnlyList<Pop> Pops => _pops;

        private readonly List<Pop> _unemployedPops = new List<Pop>();

        public IReadOnlyList<Pop> UnemployedPops => _unemployedPops;

        private readonly Dictionary<ResourceInfoHolder, float> _planetaryResourceKeep =
            new Dictionary<ResourceInfoHolder, float>();

        public IReadOnlyDictionary<ResourceInfoHolder, float> PlanetaryResourceKeep => _planetaryResourceKeep;

        public const float BasePopGrowth = 5.0f;

        public bool CheckSpecialActionCost(IReadOnlyDictionary<ResourceInfoHolder, int> cost)
        {
            throw new System.NotImplementedException();
        }

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<ResourceInfoHolder, int> cost)
        {
            throw new System.NotImplementedException();
        }

        public void AddModifier(string modifierName, string scopeName, int leftMonth = -1, IReadOnlyList<HexTileCoord> tiles = null)
        {
            _modifiers.Add(new Modifier(
                GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName), scopeName,
                leftMonth, tiles));
        }

        public void RecalculateModifierEffect() => ModifierEffect = this.GetModifierEffect();
    }
}
