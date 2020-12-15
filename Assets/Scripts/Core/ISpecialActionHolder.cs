using System.Collections.Generic;

namespace Core
{
    public interface ISpecialActionHolder : IInfinityObject
    {
        IEnumerable<SpecialAction> SpecialActions { get; }

        void AddSpecialAction(string name);

        bool CheckSpecialActionCost(IReadOnlyDictionary<string, int> cost);

        void ConsumeSpecialActionCost(IReadOnlyDictionary<string, int> cost);
    }
}
