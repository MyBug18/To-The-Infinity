using System.Collections.Generic;

namespace Core.GameData
{
    public class ResourceData : IGameData
    {
        private readonly Dictionary<string, ResourcePrototype> _data = new Dictionary<string, ResourcePrototype>();

        private ResourcePrototype _default;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is ResourcePrototype rp)) return;

            if (_data.ContainsKey(rp.Name)) return;

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