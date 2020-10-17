using System.Collections.Generic;

namespace Core
{
    public interface ISpecialActionHolder : ITypeNameHolder
    {
        IReadOnlyList<SpecialAction> SpecialActions { get; }

        bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost);

        void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost);
    }
}
