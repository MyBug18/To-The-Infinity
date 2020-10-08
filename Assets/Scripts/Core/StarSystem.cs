using System;
using Core.GameData;
using System.Collections.Generic;

namespace Core
{
    public sealed class StarSystem : ITileMapHolder
    {
        public readonly Game Game;

        public string TypeName => nameof(StarSystem);

        public TileMap TileMap { get; }

        private readonly List<Modifier> _modifiers = new List<Modifier>();

        public IEnumerable<Modifier> Modifiers
        {
            get
            {
                // TODO: Change this to Game Modifiers
                var upwardModifiers = new List<Modifier>();

                foreach (var m in upwardModifiers)
                    yield return m;

                foreach (var m in _modifiers)
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
            for (var i = _modifiers.Count - 1; i >= 0; i--)
            {
                var m = _modifiers[i];
                if (m.IsPermanent) continue;

                if (m.LeftMonth - month <= 0)
                {
                    _modifiers.RemoveAt(i);

                    if (m.IsRelated(TypeName, "All"))
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
            IReadOnlyDictionary<string, Action<IModifierHolder>> events, bool isRemoving)
        {
            foreach (var kv in events)
            {
                switch (kv.Key)
                {

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
