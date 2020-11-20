using System.Collections.Generic;

namespace Core
{
    public interface IModifier
    {
        string Name { get; }

        IReadOnlyDictionary<string, TriggerEvent> GetTriggerEvent(IModifierEffectHolder target);

        void OnAdded(IModifierEffectHolder target);

        void OnRemoved(IModifierEffectHolder target);
    }
}
