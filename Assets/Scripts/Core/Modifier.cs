using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;

namespace Core
{
    public sealed class Modifier
    {
        public ModifierCore Core { get; }

        public int LeftMonth { get; private set; }

        public bool IsPermanent => LeftMonth != -1;

        public string AdderGuid { get; }

        public Modifier(ModifierCore core, string adderGuid, int leftMonth = -1)
        {
            Core = core;
            AdderGuid = adderGuid;
            Core.SetAdder(AdderGuid);
            LeftMonth = leftMonth;
        }

        public bool IsRelated(string typeName) => Core.Scope.ContainsKey(typeName);

        public void ReduceLeftMonth(int month)
        {
            if (LeftMonth == -1) return;

            LeftMonth -= month;
        }
    }

    public sealed class ModifierCore : IEquatable<ModifierCore>
    {
        public string Name { get; }

        public string TargetType { get; }

        public bool IsTileLimited { get; }

        public string AdditionalDesc { get; }

        public IReadOnlyDictionary<string, ModifierScope> Scope { get; }

        public ModifierCore(string name, string targetType, bool isTileLimited, string additionalDesc,
            IReadOnlyDictionary<string, ModifierScope> scope)
        {
            Name = name;
            TargetType = targetType;
            IsTileLimited = isTileLimited;
            AdditionalDesc = additionalDesc;
            Scope = scope;
        }

        public override bool Equals(object obj) => obj is ModifierCore m && Equals(m);

        public bool Equals(ModifierCore m) => m != null && Name == m.Name;

        public override int GetHashCode() => Name.GetHashCode();

        public void SetAdder(string adderGuid)
        {
            foreach (var s in Scope.Values)
                s.SetInfo(adderGuid);
        }
    }

    public sealed class ModifierScope
    {
        private static readonly Action<IModifierHolder> DummyFunction = _ => { };

        public string TargetTypeName { get; }

        private string _adderGuid;

        private readonly Func<IModifierHolder, string, List<ModifierEffect>> _getEffect;

        private readonly Func<IModifierHolder, string, bool> _conditionChecker;

        public IReadOnlyDictionary<string, ScriptFunctionDelegate> TriggerEvent { get; }

        public readonly Action<IModifierHolder> OnAdded, OnRemoved;

        public ModifierScope(string targetTypeName,
            Func<IModifierHolder, string, List<ModifierEffect>> getEffect,
            Func<IModifierHolder, string, bool> conditionChecker,
            IReadOnlyDictionary<string, ScriptFunctionDelegate> triggerEvent)
        {
            TargetTypeName = targetTypeName;
            _getEffect = getEffect;
            _conditionChecker = conditionChecker;
            TriggerEvent = triggerEvent;

            OnAdded = TriggerEvent.TryGetValue("OnAdded", out var onAdded)
                ? target => onAdded.Invoke(target, _adderGuid)
                : DummyFunction;

            OnRemoved = TriggerEvent.TryGetValue("OnRemoved", out var onRemoved)
                ? target => onRemoved.Invoke(target, _adderGuid)
                : DummyFunction;
        }

        public void SetInfo(string adderGuid)
        {
            _adderGuid = adderGuid;
        }

        public bool CheckCondition(IModifierHolder holder) => _conditionChecker(holder, _adderGuid);

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierHolder holder) => _getEffect(holder, _adderGuid);
    }

    public readonly struct ModifierEffect
    {
        public string EffectInfo { get; }

        public IReadOnlyList<string> AdditionalInfos { get; }

        public int Amount { get; }

        public ModifierEffect(string effectInfo, IReadOnlyList<string> additionalInfos, int amount)
        {
            EffectInfo = effectInfo;
            AdditionalInfos = additionalInfos;
            Amount = amount;
        }
    }
}
