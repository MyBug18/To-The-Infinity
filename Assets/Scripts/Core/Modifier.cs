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

        void RemoveModifier(Modifier modifier);
    }

    public class Modifier : IEquatable<Modifier>
    {
        public string Name { get; }

        public string TargetTypeName { get; }

        public string AdditionalInfo { get; }

        public IReadOnlyList<ModifierInfoHolder> Effect { get; }

        private readonly Func<bool> _conditionChecker;

        public bool CheckCondition() => _conditionChecker();

        public Modifier(string name, string targetTypeName, string additionalInfo, IReadOnlyList<ModifierInfoHolder> effect, Func<bool> conditionChecker)
        {
            Name = name;
            TargetTypeName = targetTypeName;
            AdditionalInfo = additionalInfo;
            Effect = effect;
            _conditionChecker = conditionChecker;
        }

        public override bool Equals(object obj) => obj is Modifier m && Equals(m);

        public bool Equals(Modifier m) => m != null && Name == m.Name;

        public override int GetHashCode() => Name.GetHashCode();
    }
}
