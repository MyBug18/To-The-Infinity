using System.Collections.Generic;
using Core.GameData;

namespace Core
{
    public class StarSystem : ITileMapHolder
    {
        public string HolderType => nameof(StarSystem);

        public TileMap TileMap { get; }

        private readonly List<Modifier> _modifiers = new List<Modifier>();

        public IReadOnlyList<Modifier> Modifiers => _modifiers;

        public void StartNewTurn(int month)
        {
            ReduceModifiersLeftMonth(month);
            TileMap.StartNewTurn(month);
        }

        public void AddModifier(string modifierName, string scopeName, int leftMonth = -1, IReadOnlyList<HexTileCoord> tiles = null)
        {
            _modifiers.Add(new Modifier(
                GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName), scopeName,
                leftMonth, tiles));
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
