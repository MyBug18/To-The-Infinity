using System.Collections.Generic;

namespace Core
{
    public interface ISpecialActionHolder : ITypeNameHolder
    {
        IReadOnlyList<SpecialAction> SpecialActions { get; }

        bool CheckSpecialActionCost(IReadOnlyDictionary<ResourceInfoHolder, int> cost);

        void ConsumeSpecialActionCost(IReadOnlyDictionary<ResourceInfoHolder, int> cost);
    }
}