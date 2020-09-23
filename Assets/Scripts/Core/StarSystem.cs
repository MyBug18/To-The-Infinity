using Core.GameData;
using System.Collections.Generic;

namespace Core
{
    public class StarSystem : ITileMapHolder
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

        public void AddModifierDirectly(string modifierName, int leftMonth, IReadOnlyList<HexTileCoord> tiles)
        {
            var m = new Modifier(GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName),
                leftMonth, tiles);

            if (m.Core.TargetType != TypeName)
                return;

            AddModifier(m);
        }

        public void AddModifier(Modifier m)
        {
            _modifiers.Add(m);

            if (m.IsRelated(TypeName))
                m.Core.Scope[TypeName].OnAdded(this);

            // TODO: Also add modifier to buildings, etc.
        }

        public void RemoveModifierDirectly(string modifierName)
        {
            for (var i = 0; i < _modifiers.Count; i++)
            {
                var m = _modifiers[i];

                if (m.Core.Name != modifierName) continue;
                if(m.Core.TargetType != TypeName) continue;

                _modifiers.RemoveAt(i);
                if (m.IsRelated(TypeName))
                    m.Core.Scope[TypeName].OnRemoved(this);

                break;
            }

            // TODO: Also remove modifier to buildings, etc
        }

        public void RemoveModifierFromUpward(string modifierName)
        {
            for (var i = 0; i < _modifiers.Count; i++)
            {
                var m = _modifiers[i];

                if (m.Core.Name != modifierName) continue;

                _modifiers.RemoveAt(i);
                if (m.IsRelated(TypeName))
                    m.Core.Scope[TypeName].OnRemoved(this);

                break;
            }

            // TODO: Also remove modifier to buildings, etc
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
    }
}
