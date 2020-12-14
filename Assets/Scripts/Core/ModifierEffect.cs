using System.Collections.Generic;

namespace Core
{
    public enum ModifierEffectType
    {
        Default,

        MaxMovePoint, // MaxMovePoint
        AttackPower, // AttackPower
        MaxHp, // MaxHp
    }

    public readonly struct ModifierEffect
    {
        public ModifierEffectType EffectType { get; }

        public IReadOnlyList<string> AdditionalInfos { get; }

        public int Amount { get; }

        public ModifierEffect(ModifierEffectType effectType, IReadOnlyList<string> additionalInfos, int amount)
        {
            EffectType = effectType;
            AdditionalInfos = additionalInfos;
            Amount = amount;
        }
    }
}
