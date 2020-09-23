using System;
using System.Collections.Generic;

namespace Core
{
    public interface IModifierHolder : ITypeNameHolder
    {
        IReadOnlyList<Modifier> Modifiers { get; }

        void AddModifierDirectly(string modifierName, int leftMonth, IReadOnlyList<HexTileCoord> tiles);

        void AddModifier(Modifier m);

        void RemoveModifierDirectly(string modifierName);

        void RemoveModifierFromUpward(string modifierName);
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

        public bool IsRelated(string typeName) => Core.Scope.ContainsKey(typeName);

        public bool IsTileLimited => Tiles != null;

        public bool IsPermanent => LeftMonth != -1;

        public Modifier ReduceLeftMonth(int month) =>
            new Modifier(Core, LeftMonth == -1 ? -1 : LeftMonth - month, Tiles);

        public Modifier WithoutTileLimit() => new Modifier(Core, LeftMonth);
    }

    public class ModifierCore : IEquatable<ModifierCore>
    {
        public string Name { get; }

        public string TargetType { get; }

        public string AdditionalDesc { get; }

        public IReadOnlyDictionary<string, ModifierScope> Scope { get; }

        public ModifierCore(string name, string targetType, string additionalDesc,
            IReadOnlyDictionary<string, ModifierScope> scope)
        {
            Name = name;
            TargetType = targetType;
            AdditionalDesc = additionalDesc;
            Scope = scope;
        }

        public override bool Equals(object obj) => obj is ModifierCore m && Equals(m);

        public bool Equals(ModifierCore m) => m != null && Name == m.Name;

        public override int GetHashCode() => Name.GetHashCode();
    }

    public class ModifierScope
    {
        public string ScopeName { get; }

        private readonly Func<IModifierHolder, List<ModifierEffect>> _getEffect;

        private readonly Func<IModifierHolder, bool> _conditionChecker;

        private readonly Action<IModifierHolder> _onAdded;

        private readonly Action<IModifierHolder> _onRemoved;

        public ModifierScope(string scopeName,
            Func<IModifierHolder, List<ModifierEffect>> getEffect,
            Func<IModifierHolder, bool> conditionChecker,
            Action<IModifierHolder> onAdded, Action<IModifierHolder> onRemoved)
        {
            ScopeName = scopeName;
            _getEffect = getEffect;
            _conditionChecker = conditionChecker;
            _onAdded = onAdded;
            _onRemoved = onRemoved;
        }

        public bool CheckCondition(IModifierHolder target) => _conditionChecker(target);

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierHolder target) => _getEffect(target);

        public void OnAdded(IModifierHolder holder) => _onAdded(holder);

        public void OnRemoved(IModifierHolder holder) => _onRemoved(holder);
    }

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
}
