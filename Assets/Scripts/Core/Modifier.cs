using System.Collections.Generic;

namespace Core
{
    public sealed class Modifier : IModifier
    {
        private readonly ModifierCore _core;

        public Modifier(ModifierCore core, string adderObjectGuid, int leftMonth = -1)
        {
            _core = core;
            AdderObjectGuid = adderObjectGuid;
            LeftMonth = leftMonth;
        }

        public int LeftMonth { get; private set; }

        public bool IsPermanent => LeftMonth != -1;

        public string AdderObjectGuid { get; }

        public string Name => _core.Name;

        public IReadOnlyDictionary<string, TriggerEvent> GetTriggerEvent(IModifierEffectHolder target)
        {
            var result = new Dictionary<string, TriggerEvent>();

            if (!_core.Scope.TryGetValue(target.TypeName, out var scope)) return result;

            foreach (var kv in scope.TriggerEvent)
            {
                var name = kv.Key;

                if (name == "OnAdded" || name == "OnRemoved") continue;

                result[name] = new TriggerEvent(Name, name, kv.Value, target, AdderObjectGuid);
            }

            return result;
        }

        public void OnAdded(IModifierEffectHolder target) => _core.OnAdded(target, AdderObjectGuid);

        public void OnRemoved(IModifierEffectHolder target) => _core.OnRemoved(target, AdderObjectGuid);

        public void ReduceLeftMonth(int month)
        {
            if (LeftMonth == -1) return;

            LeftMonth -= month;
        }

        public override bool Equals(object obj) => obj is Modifier m && _core == m._core;

        public override int GetHashCode() => _core.GetHashCode();
    }
}
