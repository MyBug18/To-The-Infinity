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

        public readonly int LeftMonth;

        public readonly IReadOnlyList<HexTileCoord> Tiles;

        public Modifier(ModifierCore core, int leftMonth = -1, IReadOnlyList<HexTileCoord> tiles = null)
        {
            Core = core;
            LeftMonth = leftMonth;
            Tiles = tiles;
        }

        public bool IsTileLimited => Tiles != null;

        public bool IsPermanent => LeftMonth != -1;

        public Modifier ReduceLeftMonth(int month) =>
            new Modifier(Core, LeftMonth == -1 ? -1 : LeftMonth - month, Tiles);
    }

    public interface IModifierHolder
    {
        string TypeName { get; }

        IReadOnlyList<Modifier> Modifiers { get; }

        void AddModifier(string modifierName, int leftMonth, IReadOnlyList<HexTileCoord> tiles);
    }

    public class ModifierCore : IEquatable<ModifierCore>
    {
        public string Name { get; }

        public string TargetType { get; }

        public string AdditionalDesc { get; }

        private readonly Func<IModifierHolder, List<ModifierEffect>> _effectGetter;

        private readonly Func<IModifierHolder, bool> _conditionChecker;

        private readonly Action<IModifierHolder> _onAdded;

        private readonly Action<IModifierHolder> _onRemoved;

        public bool CheckCondition(IModifierHolder target) => _conditionChecker(target);

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierHolder target) => _effectGetter(target);

        public ModifierCore(string name, string targetType, string additionalDesc,
            Func<IModifierHolder, List<ModifierEffect>> effectGetter, Func<IModifierHolder, bool> conditionChecker,
            Action<IModifierHolder> onAdded, Action<IModifierHolder> onRemoved)
        {
            Name = name;
            TargetType = targetType;
            AdditionalDesc = additionalDesc;
            _effectGetter = effectGetter;
            _conditionChecker = conditionChecker;
            _onAdded = onAdded;
            _onRemoved = onRemoved;
        }

        public void OnAdded(IModifierHolder holder) => _onAdded(holder);

        public void OnRemoved(IModifierHolder holder) => _onRemoved(holder);

        public override bool Equals(object obj) => obj is ModifierCore m && Equals(m);

        public bool Equals(ModifierCore m) => m != null && Name == m.Name;

        public override int GetHashCode() => Name.GetHashCode();
    }
}
