using System.Collections.Generic;

namespace Core
{
    public interface ISpecialActionHolder : IInfinityObject
    {
        IReadOnlyDictionary<string, SpecialAction> SpecialActions { get; }

        bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost);

        void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost);
    }
}
