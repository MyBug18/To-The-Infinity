using Core.GameData;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core
{
    public class Planet : ITileMapHolder, IOnHexTileObject
    {
        public string TypeName => nameof(Planet);

        public string Owner { get; }

        public TileMap TileMap { get; }

        public IReadOnlyList<SpecialAction> SpecialActions { get; }

        public string Name { get; }

        public HexTile Tile { get; }

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

        public const float BasePopGrowth = 5.0f;

        private readonly Dictionary<ResourceInfoHolder, float> _planetaryResourceKeep =
            new Dictionary<ResourceInfoHolder, float>();

        public IReadOnlyDictionary<ResourceInfoHolder, float> PlanetaryResourceKeep => _planetaryResourceKeep;

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
            TileMap.StartNewTurn(month);
        }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<ResourceInfoHolder, int> cost)
        {
            throw new System.NotImplementedException();
        }

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<ResourceInfoHolder, int> cost)
        {
            throw new System.NotImplementedException();
        }

        private void ReduceModifiersLeftMonth(int month)
        {
            for (var i = _modifiers.Count - 1; i >= 0; i--)
            {
                var m = _modifiers[i];
                if (m.IsPermanent) continue;

                if (m.LeftMonth - month <= 0)
                {
                    _modifiers.RemoveAt(i);
                    continue;
                }

                _modifiers[i] = m.ReduceLeftMonth(month);
            }
        }

        public void AddModifier(string modifierName, string scopeName, int leftMonth = -1, IReadOnlyList<HexTileCoord> tiles = null)
        {
            _modifiers.Add(new Modifier(
                GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName), scopeName,
                leftMonth, tiles));
        }

        public void RecalculateModifierEffect() => ModifierEffect = this.GetModifierEffect();

        public void SubscribeEvent(string eventType, Closure luaFunction)
        {
            throw new System.NotImplementedException();
        }

        public void UnsubscribeEvent(string eventType, Closure luaFunction)
        {
            throw new System.NotImplementedException();
        }
    }
}
