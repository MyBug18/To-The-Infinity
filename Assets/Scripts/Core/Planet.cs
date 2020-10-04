using System;
using Core.GameData;
using System.Collections.Generic;

namespace Core
{
    public sealed class Planet : ITileMapHolder, IOnHexTileObject
    {
        public string TypeName => nameof(Planet);

        public string Owner { get; }

        public TileMap TileMap { get; }

        public IReadOnlyList<SpecialAction> SpecialActions { get; }

        public string Name { get; }

        public HexTile CurrentTile { get; }

        private readonly List<Modifier> _modifiers = new List<Modifier>();

        public IEnumerable<Modifier> Modifiers
        {
            get
            {
                var upwardModifiers = CurrentTile.TileMap.Holder.Modifiers;

                foreach (var m in upwardModifiers)
                    yield return m;

                foreach (var m in _modifiers)
                    yield return m;
            }
        }

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
                    ApplyModifierChangeToDownward(m, true);

                    continue;
                }

                _modifiers[i] = m.ReduceLeftMonth(month);
            }
        }

        private void RegisterModifierEvent(string modifierName,
            IReadOnlyDictionary<string, Action<IModifierHolder>> events, bool isRemoving)
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

        public void AddModifier(string modifierName, int leftMonth, IReadOnlyList<HexTileCoord> tiles)
        {
            var m = new Modifier(GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName),
                leftMonth, tiles);

            if (m.Core.TargetType != TypeName)
                return;

            _modifiers.Add(m);
            ApplyModifierChangeToDownward(m, false);
        }

        public void RemoveModifier(string modifierName)
        {
            for (var i = 0; i < _modifiers.Count; i++)
            {
                var m = _modifiers[i];

                if (m.Core.Name != modifierName) continue;
                if(m.Core.TargetType != TypeName) continue;

                _modifiers.RemoveAt(i);
                ApplyModifierChangeToDownward(m, true);

                break;
            }
        }

        public void ApplyModifierChangeToDownward(Modifier m, bool isRemoving)
        {
            if (m.IsRelated(TypeName))
            {
                var scope = m.Core.Scope[TypeName];

                if (isRemoving)
                {
                    scope.OnRemoved(this);

                    RegisterModifierEvent(m.Core.Name, scope.TriggerEvent, true);
                }
                else
                {
                    scope.OnAdded(this);

                    RegisterModifierEvent(m.Core.Name, scope.TriggerEvent, false);
                }
            }

            TileMap.ApplyModifierChangeToTileObjects(m, isRemoving);
        }

        public void RecalculateModifierEffect() => ModifierEffect = this.GetModifiersEffect();
    }
}
