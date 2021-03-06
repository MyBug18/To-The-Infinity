﻿using System.Collections.Generic;

namespace Core
{
    public interface ISinglePlayerModifierHolder : IModifierEffectHolder
    {
        IEnumerable<Modifier> GetModifiers();

        void AddModifier(string modifierName, IInfinityObject adder, int leftWeek);

        void RemoveModifier(string modifierName);

        bool HasModifier(string modifierName);
    }
}
