using System;
using Core.GameData;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core
{
    public sealed class Planet : ITileMapHolder, IOnHexTileObject
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

        #region Pop

        private readonly List<Pop> _pops = new List<Pop>();

        public IReadOnlyList<Pop> Pops => _pops;

        private readonly List<Pop> _unemployedPops = new List<Pop>();

        public IReadOnlyList<Pop> UnemployedPops => _unemployedPops;

        public const float BasePopGrowth = 5.0f;

        #endregion

        #region TriggerEvent

        private readonly Dictionary<string, Action> _onPopBirth = new Dictionary<string, Action>();

        #endregion

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

                    if (m.IsRelated(TypeName))
                    {
                        var scope = m.Core.Scope[TypeName];

                        scope.OnRemoved(this);

                        RegisterModifierEvent(m.Core.Name, scope.TriggerEvent, true);
                    }

                    continue;
                }

                _modifiers[i] = m.ReduceLeftMonth(month);
            }
        }

        private void RegisterModifierEvent(string modifierName,
            IReadOnlyDictionary<string, Action<IModifierHolder>> events, bool isRemoving = false)
        {
            foreach (var kv in events)
            {
                switch (kv.Key)
                {
                    case "OnPopBirth":
                        if (isRemoving)
                            _onPopBirth.Remove(modifierName);
                        else
                            _onPopBirth.Add(modifierName, () => kv.Value.Invoke(this));
                        break;
                }
            }
        }

        public void AddModifier(string modifierName, int leftMonth, IReadOnlyList<HexTileCoord> tiles, bool isDirect)
        {
            var m = new Modifier(GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName),
                leftMonth, tiles);

            if (isDirect && m.Core.TargetType != TypeName)
                return;

            _modifiers.Add(m);

            if (m.IsRelated(TypeName))
            {
                var scope = m.Core.Scope[TypeName];

                scope.OnAdded(this);

                RegisterModifierEvent(m.Core.Name, scope.TriggerEvent);
            }

            // TODO: Also add modifier to buildings, etc.
        }

        public void RemoveModifier(string modifierName, bool isDirect)
        {
            for (var i = 0; i < _modifiers.Count; i++)
            {
                var m = _modifiers[i];

                if (isDirect && m.Core.Name != modifierName) continue;
                if(m.Core.TargetType != TypeName) continue;

                _modifiers.RemoveAt(i);
                if (m.IsRelated(TypeName))
                {
                    var scope = m.Core.Scope[TypeName];

                    scope.OnRemoved(this);

                    RegisterModifierEvent(m.Core.Name, scope.TriggerEvent, true);
                }

                break;
            }

            // TODO: Also remove modifier to buildings, etc
        }

        public void RecalculateModifierEffect() => ModifierEffect = this.GetModifiersEffect();
    }
}
