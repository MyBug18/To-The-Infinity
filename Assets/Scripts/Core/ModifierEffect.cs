using System.Collections.Generic;

namespace Core
{
    public readonly struct ModifierEffect
    {
        public string EffectInfo { get; }

        public IReadOnlyList<string> AdditionalInfos { get; }

        public int Amount { get; }

        public ModifierEffect(string effectInfo, IReadOnlyList<string> additionalInfos, int amount)
        {
            EffectInfo = effectInfo;
            AdditionalInfos = additionalInfos;
            Amount = amount;
        }
    }
}
