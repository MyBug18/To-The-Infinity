using MoonSharp.Interpreter;

namespace Core
{
    public enum TriggerEventType
    {
        OnAdded, // OnAdded(this, adderObjectGuid)
        OnRemoved, // OnRemoved(this, adderObjectGuid)
        BeforeDestroyed, // BeforeDestroyed(this, adderObjectGuid)

        BeforeDamaged, // BeforeDamaged(this, adderObjectGuid, damageInfo)
        AfterDamaged, // AfterDamaged(this, adderObjectGuid, damageInfo)
        BeforeMeleeAttack, // BeforeMeleeAttack(this, adderObjectGuid, attackTarget)
        AfterMeleeAttack, // AfterMeleeAttack(this, adderObjectGuid, damageInfo, attackTarget)

        OnPopBirth, // OnPopBirth(this, adderObjectGuid)
    }

    public sealed class TriggerEvent
    {
        private readonly string _adderObjectGuid;

        private readonly ScriptFunctionDelegate _f;
        private readonly string _modifierName;

        private readonly IInfinityObject _target;

        private readonly TriggerEventType _type;

        public TriggerEvent(string modifierName, TriggerEventType type, ScriptFunctionDelegate f,
            IInfinityObject target, string adderObjectGuid, int priority)
        {
            _modifierName = modifierName;
            _type = type;
            _f = f;
            _target = target;
            _adderObjectGuid = adderObjectGuid;
            Priority = priority;
        }

        public int Priority { get; }

        public void Invoke(params object[] args)
        {
            _f.TryInvoke($"Scope.{_target.TypeName}.{_type}", _modifierName, _target, _adderObjectGuid, args);
        }
    }
}
