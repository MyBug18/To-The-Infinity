using System;
using System.Collections.Generic;

namespace Core
{
    public readonly struct ModifierEffect
    {
        public ResourceInfoHolder ResourceInfo { get; }

        public int Amount { get; }

        public ModifierEffect(ResourceInfoHolder resourceInfo, int amount)
        {
            ResourceInfo = resourceInfo;
            Amount = amount;
        }
    }

    public readonly struct Modifier
    {
        public readonly ModifierCore Core;

        public readonly string ScopeTypeName;

        public readonly int LeftMonth;

        public readonly IReadOnlyList<HexTileCoord> Tiles;

        public Modifier(ModifierCore core, string scope, int leftMonth = -1, IReadOnlyList<HexTileCoord> tiles = null)
        {
            Core = core;
            ScopeTypeName = scope;
            LeftMonth = leftMonth;
            Tiles = tiles;
        }

        public bool IsTileLimited => Tiles != null;

        public bool IsPermanent => LeftMonth != -1;

        public Modifier ReduceLeftMonth(int month) =>
            new Modifier(Core, ScopeTypeName, LeftMonth == -1 ? -1 : LeftMonth - month, Tiles);
    }

    public interface IModifierHolder
    {
        string HolderType { get; }

        IReadOnlyList<Modifier> Modifiers { get; }

        void ReduceModifiersLeftMonth(int month);

        void AddModifier(string modifierName, string scopeName, int leftMonth, IReadOnlyList<HexTileCoord> tiles);
    }

    public class ModifierCore : IEquatable<ModifierCore>
    {
        public string Name { get; }

        public string TargetType { get; }

        public string AdditionalDesc { get; }

        private readonly Func<IModifierHolder, List<ModifierEffect>> _effectGetter;

        private readonly Func<IModifierHolder, bool> _conditionChecker;

        public bool CheckCondition(IModifierHolder target) => _conditionChecker(target);

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierHolder target) => _effectGetter(target);

        public ModifierCore(string name, string targetType, string additionalDesc,
            Func<IModifierHolder, List<ModifierEffect>> effectGetter, Func<IModifierHolder, bool> conditionChecker)
        {
            Name = name;
            TargetType = targetType;
            AdditionalDesc = additionalDesc;
            _effectGetter = effectGetter;
            _conditionChecker = conditionChecker;
        }

        public override bool Equals(object obj) => obj is ModifierCore m && Equals(m);

        public bool Equals(ModifierCore m) => m != null && Name == m.Name;

        public override int GetHashCode() => Name.GetHashCode();
    }
}
