using System.Collections.Generic;

namespace Core.GameData
{
    public class TileMapData : IGameData
    {
        private readonly Dictionary<string, TileMapPrototype> _data = new Dictionary<string, TileMapPrototype>();

        private TileMapPrototype _default;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is TileMapPrototype tmp)) return;

            if (_data.ContainsKey(tmp.Name)) return;

            if (tmp.Name == "Default")
            {
                _default = tmp;
                return;
            }

            _data[tmp.Name] = tmp;
        }

        public void OnGameInitialized(Game _)
        {
        }

        public TileMapPrototype GetPrototype(string name) => !_data.TryGetValue(name, out var result) ? null : result;
    }
}
