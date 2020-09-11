using System.Collections.Generic;
using Core.GameData;

namespace Core
{
    public class Pop : IModifierHolder
    {
        public string Name { get; }

        public Planet Planet { get; private set; }

        public string Aptitude { get; }

        public int Happiness { get; }

        private readonly List<Modifier> _modifiers = new List<Modifier>();

        public IReadOnlyList<Modifier> Modifiers => _modifiers;

        public void AddModifierToTarget(string modifierName)
        {
            var modifier = GameDataStorage.Instance.GetGameData<ModifierData>().GetModifierDirectly(modifierName, this);

            AddModifier(modifier);
        }

        public void AddModifier(Modifier modifier)
        {
            _modifiers.Add(modifier);
        }

        public void RemoveModifier(Modifier modifier)
        {
            _modifiers.Remove(modifier);
        }
    }
}