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

        public void AddModifier(string modifierName, string scopeName, int leftMonth = -1, IReadOnlyList<HexTileCoord> tiles = null)
        {
            _modifiers.Add(new Modifier(
                GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName), scopeName,
                leftMonth, tiles));
        }
    }
}
