using System.Collections.Generic;

namespace Core
{
    public interface IModifier
    {
        string Name { get; }

        IReadOnlyDictionary<TriggerEventType, TriggerEvent> GetTriggerEvent(IModifierEffectHolder target);

        void OnRemoved(IModifierEffectHolder target);

        IReadOnlyList<ModifierEffect> GetEffects(IModifierEffectHolder target);

        object ToSaveData();
    }
}
