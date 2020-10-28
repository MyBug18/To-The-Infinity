using Core.GameData;
using MoonSharp.Interpreter;
using System.Collections.Generic;

namespace Core
{
    public sealed class StarSystem : ITileMapHolder
    {
        public string TypeName => nameof(StarSystem);

        public string Guid { get; }

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

        private readonly Dictionary<string, object> _customValues = new Dictionary<string, object>();

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
                }
            }
        }

        public void AddModifier(string modifierName, string adderGuid, int leftMonth)
        {
            if (_modifiers.ContainsKey(modifierName)) return;

            var m = new Modifier(GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName),
                adderGuid, leftMonth);

            if (m.Core.TargetType != TypeName || m.Core.IsTileLimited)
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

        public object GetCustomValue(string key, object defaultValue) =>
            _customValues.TryGetValue(key, out var result) ? result : defaultValue;

        public void SetCustomValue(string key, object value)
        {
            if (!value.GetType().IsPrimitive && value.GetType() != typeof(string)) return;

            _customValues[key] = value;
        }

        public bool HasModifier(string modifierName) => _modifiers.ContainsKey(modifierName);

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
