using System.Collections.Generic;

namespace Core
{
    public interface IMultiPlayerModifierHolder : IModifierEffectHolder
    {
        IEnumerable<Modifier> GetModifiers(string targetPlayerName);

        void AddModifier(string targetPlayerName, string modifierName, IInfinityObject adder, int leftMonth);

        void RemoveModifier(string targetPlayerName, string modifierName);

        bool HasModifier(string targetPlayerName, string modifierName);
    }
}
