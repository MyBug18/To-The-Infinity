using System.Collections.Generic;

namespace Core
{
    public interface IModifierEffectHolder : IInfinityObject
    {
        IReadOnlyDictionary<string, IReadOnlyList<ModifierEffect>> ModifierEffectsMap { get; }

        void ApplyModifierChangeToDownward(string targetPlayerName, IModifier m,
            bool isRemoving, bool isFromSaveData);

        void StartCachingModifierEffect();
    }
}
