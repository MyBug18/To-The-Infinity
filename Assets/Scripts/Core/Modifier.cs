using System;
using System.Collections.Generic;

namespace Core
{
    public sealed class Modifier : IModifier
    {
        private readonly ModifierCore _core;

        public Modifier(ModifierCore core, int adderObjectId, int leftMonth = -1)
        {
            _core = core;
            AdderObjectId = adderObjectId;
            LeftMonth = leftMonth;
        }

        public int LeftMonth { get; private set; }

        public bool IsPermanent => LeftMonth != -1;

        public int AdderObjectId { get; }

        public string Name => _core.Name;

        public IReadOnlyDictionary<TriggerEventType, TriggerEvent> GetTriggerEvent(IModifierEffectHolder target)
        {
            var result = new Dictionary<TriggerEventType, TriggerEvent>();

            if (!_core.Scope.TryGetValue(target.TypeName, out var scope)) return result;

            foreach (var kv in scope.TriggerEvent)
            {
                if (!Enum.TryParse(kv.Key, out TriggerEventType type))
                {
                    Logger.Log(LogType.Warning, Name,
                        $"{kv.Key} is not a valid TriggerEvent type, so it will be ignored.");
                    continue;
                }

                if (type == TriggerEventType.OnAdded || type == TriggerEventType.OnRemoved) continue;

                var priority = scope.TriggerEventPriority.TryGetValue(kv.Key, out var value) ? value : 0;

                result[type] = new TriggerEvent(Name, type, kv.Value, target, AdderObjectId, priority);
            }

            return result;
        }

        public void OnRemoved(IModifierEffectHolder target) => _core.OnRemoved(target, AdderObjectId);

        public IReadOnlyList<ModifierEffect> GetEffects(IModifierEffectHolder target) =>
            _core.GetEffects(target, AdderObjectId);

        public object ToSaveData()
        {
            var result = new Dictionary<string, object>
            {
                ["Name"] = Name,
                ["AdderObjectId"] = AdderObjectId,
                ["LeftMonth"] = LeftMonth,
            };

            return result;
        }

        public void ReduceLeftMonth(int month)
        {
            if (LeftMonth == -1) return;

            LeftMonth -= month;
        }

        public override bool Equals(object obj) => obj is Modifier m && _core == m._core;

        public override int GetHashCode() => _core.GetHashCode();
    }
}
