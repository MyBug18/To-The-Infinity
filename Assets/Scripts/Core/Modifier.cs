using System;
using System.Collections.Generic;

namespace Core
{
    public readonly struct ModifierInfoHolder
    {
        public ResourceInfoHolder ResourceInfo { get; }

        public int Amount { get; }

        public ModifierInfoHolder(ResourceInfoHolder resourceInfo, int amount)
        {
            ResourceInfo = resourceInfo;
            Amount = amount;
        }
    }

    public interface IModifierHolder
    {
        IReadOnlyList<Modifier> Modifiers { get; }

        void AddModifierToTarget(string modifierName);

        void AddModifier(Modifier modifier);
    }

    public class Modifier
    {
        public string Name { get; }

        public string HolderName { get; }

        public string TargetName { get; }

        public string AdditionalInfo { get; }

        public IReadOnlyList<ModifierInfoHolder> Infos { get; }

        private readonly object _target;

        private readonly Func<object, bool> _conditionChecker;

        public bool CheckCondition() => _conditionChecker(_target);
    }
}
