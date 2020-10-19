using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class Modifier
    {
        public ModifierCore Core { get; }

        public int LeftMonth { get; private set; }

        public bool IsPermanent => LeftMonth != -1;

        public IDictionary<string, object> Info { get; }

        public Modifier(ModifierCore core, IDictionary<string, object> info, int leftMonth = -1)
        {
            Core = core;

            // Cut off not primitive type
            var toRemoveList = (from kv in info where !kv.Value.GetType().IsPrimitive select kv.Key).ToList();

            foreach (var s in toRemoveList)
            {
                // TODO: Log Warning
                info.Remove(s);
            }

            Info = info;
            Core.SetInfo(info);
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

        public void SetInfo(IDictionary<string, object> info)
        {
            foreach (var s in Scope.Values)
                s.SetInfo(info);
        }
    }

    public sealed class ModifierScope
    {
        public string TargetTypeName { get; }

        private IDictionary<string, object> _info = new Dictionary<string, object>();

        private readonly Func<IModifierHolder, IDictionary<string, object>, List<ModifierEffect>> _getEffect;

        private readonly Func<IModifierHolder, IDictionary<string, object>, bool> _conditionChecker;

        private readonly Action<IModifierHolder, IDictionary<string, object>> _onAdded;

        private readonly Action<IModifierHolder, IDictionary<string, object>> _onRemoved;

        public IReadOnlyDictionary<string, ScriptFunctionDelegate> TriggerEvent { get; }

        public ModifierScope(string targetTypeName,
            Func<IModifierHolder, IDictionary<string, object>, List<ModifierEffect>> getEffect,
            Func<IModifierHolder, IDictionary<string, object>, bool> conditionChecker,
            Action<IModifierHolder, IDictionary<string, object>> onAdded,
            Action<IModifierHolder, IDictionary<string, object>> onRemoved,
            IReadOnlyDictionary<string, ScriptFunctionDelegate> triggerEvent)
        {
            TargetTypeName = targetTypeName;
            _getEffect = getEffect;
            _conditionChecker = conditionChecker;
            _onAdded = onAdded;
            _onRemoved = onRemoved;
            TriggerEvent = triggerEvent;
        }

        public void SetInfo(IDictionary<string, object> info) => _info = info;

        public bool CheckCondition(IModifierHolder holder) => _conditionChecker(holder, _info);

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierHolder holder) => _getEffect(holder, _info);

        public void OnAdded(IModifierHolder holder) => _onAdded(holder, _info);

        public void OnRemoved(IModifierHolder holder) => _onRemoved(holder, _info);
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
