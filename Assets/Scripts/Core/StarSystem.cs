using System;
using Core.GameData;
using System.Collections.Generic;

namespace Core
{
    public sealed class StarSystem : ITileMapHolder
    {
        public string TypeName => nameof(StarSystem);

        public TileMap TileMap { get; }

        private readonly List<Modifier> _modifiers = new List<Modifier>();

        public IReadOnlyList<Modifier> Modifiers => _modifiers;

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
            TileMap.StartNewTurn(month);
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
    }
}
