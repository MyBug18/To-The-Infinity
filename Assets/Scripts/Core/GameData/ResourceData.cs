using System.Collections.Generic;

namespace Core.GameData
{
    public class ResourceData : IGameData
    {
        #region HardCodedFactors

        private readonly Dictionary<string, ResourceInfoHolder> _hardCodedFactors =
            new Dictionary<string, ResourceInfoHolder>
            {
                {"Housing", new ResourceInfoHolder("Housing", ResourceType.Factor)},
                {"Amenity", new ResourceInfoHolder("Amenity", ResourceType.Factor)},
                {"Crime", new ResourceInfoHolder("Crime", ResourceType.Factor)},
                {"Stability", new ResourceInfoHolder("Stability", ResourceType.Factor)},
            };

        #endregion

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

        public bool TryGetFactor(string name, out ResourceInfoHolder result) =>
            _hardCodedFactors.TryGetValue(name, out result);
    }
}