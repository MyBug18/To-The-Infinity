using System;
using Core.GameData;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

namespace Core
{
    public sealed class StarSystem : ITileMapHolder
    {
        public readonly Game Game;

        public string TypeName => nameof(StarSystem);

        public TileMap TileMap { get; }

        private readonly Dictionary<string, Modifier> _modifiers = new Dictionary<string, Modifier>();

        public IEnumerable<Modifier> Modifiers
        {
            get
            {
                // TODO: Change this to Game Modifiers
                var upwardModifiers = new List<Modifier>();

                foreach (var m in upwardModifiers)
                    yield return m;

                foreach (var m in _modifiers.Values)
                    yield return m;
            }
        }

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
            TileMap.StartNewTurn(month);
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

                _modifiers[name] = m.ReduceLeftMonth(month);
            }

            foreach (var name in toRemoveList)
            {
                var m = _modifiers[name];

                _modifiers.Remove(name);

                if (!m.IsRelated(TypeName, "All")) continue;

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

                }
            }
        }

        public void AddModifier(string modifierName, int leftMonth)
        {
            if (_modifiers.ContainsKey(modifierName)) return;

            var m = new Modifier(GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName),
                leftMonth);

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
            if (m.IsRelated(TypeName, "All"))
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
