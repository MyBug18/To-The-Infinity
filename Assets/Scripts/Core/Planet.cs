using System.Collections.Generic;
using System.Threading.Tasks;
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

        private Dictionary<ResourceInfoHolder, int> _fromModifiers;

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

        private void RecalculateModifierYield()
        {
            var mutex = new object();
            var result = new Dictionary<ResourceInfoHolder, int>();

            Parallel.ForEach(_modifiers,
                () => new Dictionary<ResourceInfoHolder, int>(),
                (m, loop, acc) =>
                {
                    if (!m.CheckCondition())
                        return acc;

                    foreach (var info in m.Effect)
                    {
                        if (!acc.ContainsKey(info.ResourceInfo))
                            acc.Add(info.ResourceInfo, 0);

                        acc[info.ResourceInfo] += info.Amount;
                    }

                    return acc;
                },
                final =>
                {
                    lock (mutex)
                    {
                        foreach (var kv in final)
                        {
                            if (!result.ContainsKey(kv.Key))
                                result.Add(kv.Key, 0);

                            result[kv.Key] += kv.Value;
                        }
                    }
                });

            _fromModifiers = result;
        }
    }
}
