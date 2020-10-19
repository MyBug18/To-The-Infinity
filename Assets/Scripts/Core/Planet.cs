using Core.GameData;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;

namespace Core
{
    public sealed class Planet : ITileMapHolder, IOnHexTileObject
    {
        public string TypeName => nameof(Planet);

        public string Guid { get; }

        public string Owner { get; }

        public TileMap TileMap { get; }

        public IReadOnlyList<SpecialAction> SpecialActions { get; }

        public string IdentifierName { get; }

        public string CustomName { get; private set; }

        public HexTile CurrentTile { get; }

        private readonly Dictionary<string, Modifier> _modifiers = new Dictionary<string, Modifier>();

        public IEnumerable<Modifier> Modifiers
        {
            get
            {
                var upwardModifiers = CurrentTile.TileMap.Holder.Modifiers;

                foreach (var m in upwardModifiers)
                    yield return m;

                foreach (var m in _modifiers.Values)
                    yield return m;
            }
        }

        public IReadOnlyDictionary<string, int> ModifierEffect { get; private set; }

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

        #endregion Pop

        #region TriggerEvent

        private readonly Dictionary<string, Action> _onPopBirth = new Dictionary<string, Action>();

        #endregion TriggerEvent

        private readonly Dictionary<string, float> _planetaryResourceKeep =
            new Dictionary<string, float>();

        public IReadOnlyDictionary<string, float> PlanetaryResourceKeep => _planetaryResourceKeep;

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
            TileMap.StartNewTurn(month);
        }

        public bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost)
        {
            throw new NotImplementedException();
        }

        public void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost)
        {
            throw new NotImplementedException();
        }

        private void ReduceModifiersLeftMonth(int month)
        {
            var toRemoveList = new List<string>();

            foreach (var name in _modifiers.Keys)
            {
                var m = _modifiers[name];
                if (m.IsPermanent) continue;

                if (m.LeftMonth - month <= 0)
                {
                    toRemoveList.Add(name);
                    continue;
                }

                _modifiers[name].ReduceLeftMonth(month);
            }

            foreach (var name in toRemoveList)
            {
                var m = _modifiers[name];

                _modifiers.Remove(name);

                if (!m.IsRelated(TypeName)) continue;

                var scope = m.Core.Scope[TypeName];

                scope.OnRemoved(this);

                RegisterModifierEvent(m.Core.Name, scope.TriggerEvent, true);
            }
        }

        private void RegisterModifierEvent(string modifierName,
            IReadOnlyDictionary<string, ScriptFunctionDelegate> events, bool isRemoving)
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

        public void AddModifier(string modifierName, IDictionary<string, object> info, int leftMonth)
        {
            if (_modifiers.ContainsKey(modifierName)) return;

            var m = new Modifier(GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName),
                info, leftMonth);

            if (m.Core.TargetType != TypeName)
                return;

            _modifiers.Add(modifierName, m);
            ApplyModifierChangeToDownward(m, false);
        }

        public void RemoveModifier(string modifierName)
        {
            if (!_modifiers.ContainsKey(modifierName)) return;

            var m = _modifiers[modifierName];
            _modifiers.Remove(modifierName);
            ApplyModifierChangeToDownward(m, true);
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
    }
}
