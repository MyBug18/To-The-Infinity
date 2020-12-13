using MoonSharp.Interpreter;

namespace Core
{
    public enum TriggerEventType
    {
        OnAdded, // OnAdded(this, adderObject)
        OnRemoved, // OnRemoved(this, adderObject)
        BeforeDestroyed, // BeforeDestroyed(this, adderObject)

        BeforeDamaged, // BeforeDamaged(this, adderObject, damageInfo)
        AfterDamaged, // AfterDamaged(this, adderObject, damageInfo)
        BeforeMeleeAttack, // BeforeMeleeAttack(this, adderObject, attackTarget)
        AfterMeleeAttack, // AfterMeleeAttack(this, adderObject, damageInfo, attackTarget)

        OnPopBirth, // OnPopBirth(this, adderObject)
    }

    public sealed class TriggerEvent
    {
        private readonly int _adderObjectId;

        private readonly ScriptFunctionDelegate _f;
        private readonly string _modifierName;

        private readonly IInfinityObject _target;

        private readonly TriggerEventType _type;

        public TriggerEvent(string modifierName, TriggerEventType type, ScriptFunctionDelegate f,
            IInfinityObject target, int adderObjectId, int priority)
        {
            _modifierName = modifierName;
            _type = type;
            _f = f;
            _target = target;
            _adderObjectId = adderObjectId;
            Priority = priority;
        }

        public int Priority { get; }

        public void Invoke(params object[] args)
            => _f.TryInvoke($"Scope.{_target.TypeName}.{_type}", _modifierName, _target,
                Game.Instance.GetObject(_adderObjectId), args);
    }
}
