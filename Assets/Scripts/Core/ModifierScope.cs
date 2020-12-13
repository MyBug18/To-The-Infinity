using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core
{
    public sealed class ModifierScope
    {
        private readonly ScriptFunctionDelegate<List<ModifierEffect>> _getEffect;

        private readonly string _modifierName;

        public ModifierScope(string modifierName, string targetTypeName,
            ScriptFunctionDelegate<List<ModifierEffect>> getEffect,
            IReadOnlyDictionary<string, ScriptFunctionDelegate> triggerEvent,
            IReadOnlyDictionary<string, int> triggerEventPriority)
        {
            _modifierName = modifierName;
            TargetTypeName = targetTypeName;
            _getEffect = getEffect;
            TriggerEvent = triggerEvent;
            TriggerEventPriority = triggerEventPriority;
        }

        public string TargetTypeName { get; }

        public IReadOnlyDictionary<string, ScriptFunctionDelegate> TriggerEvent { get; }

        public IReadOnlyDictionary<string, int> TriggerEventPriority { get; }

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierEffectHolder target, int adderObjectId) =>
            _getEffect == null || !_getEffect.TryInvoke($"Scope.{TargetTypeName}.GetEffect", _modifierName,
                out var result, target, adderObjectId)
                ? new List<ModifierEffect>()
                : result;
    }
}
