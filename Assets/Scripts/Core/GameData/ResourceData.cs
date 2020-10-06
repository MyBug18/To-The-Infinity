using System.Collections.Generic;

namespace Core.GameData
{
    public enum ResourceType
    {
        PlanetaryResource,
        GlobalResource,
        Research,
        Factor,
    }

    public sealed class ResourceData : IGameData
    {
        private readonly HashSet<string> _factors = new HashSet<string>
        {
            "All",
            "Housing",
            "Amenity",
            "Crime",
            "Stability",
            "PopGrowth",
            "Happiness",

            "MovePoint"
        };

        private readonly Dictionary<string, ResourcePrototype> _data = new Dictionary<string, ResourcePrototype>();

        private ResourcePrototype _default;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is ResourcePrototype rp)) return;

            if (_data.ContainsKey(rp.Name) || _factors.Contains(rp.Name)) return;

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
    }
}
