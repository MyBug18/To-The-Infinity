using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public readonly struct Modifier
    {
        public readonly ModifierCore Core;

        public readonly int LeftMonth;

        public readonly IReadOnlyCollection<HexTileCoord> Tiles;

        public bool IsPermanent => LeftMonth != -1;

        public Modifier(ModifierCore core, int leftMonth = -1, IReadOnlyCollection<HexTileCoord> tiles = null)
        {
            Core = core;
            LeftMonth = leftMonth;
            Tiles = tiles;
        }

        public bool IsRelated(string typeName) => Core.Scope.ContainsKey(typeName);

        /// <summary>
        /// Returns true if modifier has no tile limit or the parameter is in it's effect range
        /// </summary>
        public bool IsInEffectRange(HexTileCoord coord) => Tiles == null || Tiles.Contains(coord);

        public Modifier ReduceLeftMonth(int month) =>
            new Modifier(Core, LeftMonth == -1 ? -1 : LeftMonth - month, Tiles);

        public Modifier WithoutTileLimit() => new Modifier(Core, LeftMonth);
    }

    public sealed class ModifierCore : IEquatable<ModifierCore>
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

    public sealed class ModifierScope
    {
        public string ScopeName { get; }

        private readonly Func<IModifierHolder, List<ModifierEffect>> _getEffect;

        private readonly Func<IModifierHolder, bool> _conditionChecker;

        private readonly Action<IModifierHolder> _onAdded;

        private readonly Action<IModifierHolder> _onRemoved;

        public IReadOnlyDictionary<string, Action<IModifierHolder>> TriggerEvent { get; }

        public ModifierScope(string scopeName,
            Func<IModifierHolder, List<ModifierEffect>> getEffect,
            Func<IModifierHolder, bool> conditionChecker,
            Action<IModifierHolder> onAdded, Action<IModifierHolder> onRemoved,
            IReadOnlyDictionary<string, Action<IModifierHolder>> triggerEvent)
        {
            ScopeName = scopeName;
            _getEffect = getEffect;
            _conditionChecker = conditionChecker;
            _onAdded = onAdded;
            _onRemoved = onRemoved;
            TriggerEvent = triggerEvent;
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
