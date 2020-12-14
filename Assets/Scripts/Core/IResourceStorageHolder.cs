using System.Collections.Generic;

namespace Core
{
    public interface IResourceStorageHolder
    {
        IReadOnlyDictionary<string, int> MaxResourceStorage { get; }

        IReadOnlyDictionary<string, int> RemainResourceStorage { get; }

        int GetMaxStorableAmount(string resourceName);

        int GetStorableAmount(string resourceName);

        void ChangeResourceAmount(string resourceName, int changeAmount);

        void GiveResource(IResourceStorageHolder target, string resourceName, int amount);
    }
}
