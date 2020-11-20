using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core
{
    public sealed class ModifierScope
    {
        private readonly ScriptFunctionDelegate<bool> _conditionChecker;

        private readonly ScriptFunctionDelegate<List<ModifierEffect>> _getEffect;

        private readonly string _modifierName;

        public ModifierScope(string modifierName, string targetTypeName,
            ScriptFunctionDelegate<List<ModifierEffect>> getEffect,
            ScriptFunctionDelegate<bool> conditionChecker,
            IReadOnlyDictionary<string, ScriptFunctionDelegate> triggerEvent)
        {
            _modifierName = modifierName;
            TargetTypeName = targetTypeName;
            _getEffect = getEffect;
            _conditionChecker = conditionChecker;
            TriggerEvent = triggerEvent;
        }

        public string TargetTypeName { get; }

        public IReadOnlyDictionary<string, ScriptFunctionDelegate> TriggerEvent { get; }

        public bool CheckCondition(IModifierEffectHolder target, string adderObjectGuid)
        {
            if (_conditionChecker == null) return true;

            return _conditionChecker.TryInvoke($"Scope.{TargetTypeName}.ConditionChecker", _modifierName,
                out var result, target, adderObjectGuid) && result;
        }

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierEffectHolder target, string adderObjectGuid) =>
            _getEffect == null || !_getEffect.TryInvoke($"Scope.{TargetTypeName}.GetEffect", _modifierName,
                out var result, target, adderObjectGuid)
                ? new List<ModifierEffect>()
                : result;
    }
}
