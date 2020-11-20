using MoonSharp.Interpreter;

namespace Core
{
    public sealed class TriggerEvent
    {
        private readonly string _adderObjectGuid;

        private readonly ScriptFunctionDelegate _f;
        private readonly string _modifierName;

        private readonly string _name;

        private readonly IInfinityObject _target;

        public TriggerEvent(string modifierName, string name, ScriptFunctionDelegate f,
            IInfinityObject target, string adderObjectGuid)
        {
            _modifierName = modifierName;
            _name = name;
            _f = f;
            _target = target;
            _adderObjectGuid = adderObjectGuid;
        }

        public void Invoke(params object[] args)
        {
            _f.TryInvoke($"Scope.{_target.TypeName}.{_name}", _modifierName, _target, _adderObjectGuid, args);
        }
    }
}
