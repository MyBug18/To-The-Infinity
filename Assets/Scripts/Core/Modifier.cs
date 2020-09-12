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
        string HolderType { get; }

        IReadOnlyList<Modifier> Modifiers { get; }

        void AddModifierInitial(string modifierName);

        void AddModifierSequential(Modifier modifier);

        void RemoveModifier(Modifier modifier);
    }

    public class Modifier : IEquatable<Modifier>
    {
        public string Name { get; }

        public string TargetType { get; }

        public string AdditionalInfo { get; }

        private readonly Func<List<ModifierInfoHolder>> _effectGetter;

        public IReadOnlyList<ModifierInfoHolder> Effect => _effectGetter();

        private readonly Func<bool> _conditionChecker;

        public bool CheckCondition() => _conditionChecker();

        public Modifier(string name, string targetType, string additionalInfo,
            Func<List<ModifierInfoHolder>> effectGetter, Func<bool> conditionChecker)
        {
            Name = name;
            TargetType = targetType;
            AdditionalInfo = additionalInfo;
            _effectGetter = effectGetter;
            _conditionChecker = conditionChecker;
        }

        public override bool Equals(object obj) => obj is Modifier m && Equals(m);

        public bool Equals(Modifier m) => m != null && Name == m.Name;

        public override int GetHashCode() => Name.GetHashCode();
    }
}
