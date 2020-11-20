using System.Collections.Generic;

namespace Core
{
    public interface ISinglePlayerModifierHolder : IModifierEffectHolder
    {
        IEnumerable<Modifier> GetModifiers();

        void AddModifier(string modifierName, string adderObjectGuid, int leftMonth);

        void RemoveModifier(string modifierName);

        bool HasModifier(string modifierName);
    }
}
