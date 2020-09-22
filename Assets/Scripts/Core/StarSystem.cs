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

        public void AddModifier(string modifierName, int leftMonth = -1, IReadOnlyList<HexTileCoord> tiles = null)
        {
            var m = new Modifier(GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName),
                leftMonth, tiles);

            _modifiers.Add(m);

            // TODO: Also add modifiers to planets, etc.

            if (m.Core.TargetType == TypeName)
                m.Core.OnAdded(this);
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
