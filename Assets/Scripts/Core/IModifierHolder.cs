using MoonSharp.Interpreter;
using System.Collections.Generic;

namespace Core
{
    public interface IModifierHolder : ITypeNameHolder
    {
        IEnumerable<Modifier> Modifiers { get; }

        void AddModifier(string modifierName, IDictionary<string, object> info, int leftMonth);

        void RemoveModifier(string modifierName);

        [MoonSharpHidden]
        void ApplyModifierChangeToDownward(Modifier m, bool isRemoving);
    }
}
