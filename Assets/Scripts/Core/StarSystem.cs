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

        public void AddModifierInitial(string modifierName)
        {
            var modifier = GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName, this);

            AddModifierSequential(modifier);
        }

        public void AddModifierSequential(Modifier modifier)
        {
            _modifiers.Add(modifier);
        }

        public void RemoveModifier(Modifier modifier)
        {
            _modifiers.Remove(modifier);
        }

        public void AddModifierToTiles(List<HexTileCoord> coords, Modifier modifier)
        {
            throw new System.NotImplementedException();
        }
    }
}
