using System.Collections.Generic;

namespace Core
{
    public interface IModifierHolder : IInfinityObject
    {
        IEnumerable<Modifier> Modifiers { get; }

        void AddModifier(string modifierName, string adderPlayerName, string adderObjectGuid, int leftMonth);

        void RemoveModifier(string modifierName);

        bool HasModifier(string modifierName);

        void ApplyModifierChangeToDownward(ModifierCore m, bool isRemoving);
    }
}
