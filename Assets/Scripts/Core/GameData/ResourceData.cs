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

            if (_data.ContainsKey(rp.IdentifierName) || _factors.Contains(rp.IdentifierName)) return;

            if (rp.IdentifierName == "Default")
            {
                _default = rp;
                return;
            }

            _data[rp.IdentifierName] = rp;
        }

        public void OnGameInitialized(Game _)
        {
        }
    }
}
