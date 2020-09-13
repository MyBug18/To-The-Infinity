using System.Collections.Generic;

namespace Core.GameData
{
    public class ResourceData : IGameData
    {
        #region HardCodedFactors

        private readonly Dictionary<string, ResourceInfoHolder> _hardCodedFactors =
            new Dictionary<string, ResourceInfoHolder>
            {
                {"All", new ResourceInfoHolder("All", ResourceType.GlobalResource)},

                {"Housing", new ResourceInfoHolder("Housing", ResourceType.Factor)},
                {"Amenity", new ResourceInfoHolder("Amenity", ResourceType.Factor)},
                {"Crime", new ResourceInfoHolder("Crime", ResourceType.Factor)},
                {"Stability", new ResourceInfoHolder("Stability", ResourceType.Factor)},
                {"PopGrowth", new ResourceInfoHolder("PopGrowth", ResourceType.Factor)},

                {"Happiness", new ResourceInfoHolder("Happiness", ResourceType.Factor)},

                {"MovePoint", new ResourceInfoHolder("MovePoint", ResourceType.Factor)},
            };

        #endregion HardCodedFactors

        private readonly Dictionary<string, ResourcePrototype> _data = new Dictionary<string, ResourcePrototype>();

        private ResourcePrototype _default;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is ResourcePrototype rp)) return;

            if (_data.ContainsKey(rp.Name) || _hardCodedFactors.ContainsKey(rp.Name)) return;

            if (rp.Name == "Default")
            {
                _default = rp;
                return;
            }

            _data[rp.Name] = rp;
        }

        public void OnGameInitialized(Game _)
        {
        }

        public ResourceInfoHolder GetResourceDirectly(string name)
        {
            if (_hardCodedFactors.TryGetValue(name, out var result))
                return result;

            return _data.TryGetValue(name, out var result2) ? result2.Create() : _default.Create();
        }
    }
}
