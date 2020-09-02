using System.Collections.Generic;

namespace Core.GameData
{
    public class TileMapData : IGameData
    {
        private readonly Dictionary<string, TileMapPrototype> _data = new Dictionary<string, TileMapPrototype>();

        public void AddNewData(ILuaHolder luaHolder)
        {

            if (!(luaHolder is TileMapPrototype)) return;

            if (_data.ContainsKey(luaHolder.Name)) return;

            _data[luaHolder.Name] = (TileMapPrototype) luaHolder;
        }

        public void OnGameInitialized(Game _)
        {
        }

        public TileMapPrototype GetPrototype(string name) => !_data.TryGetValue(name, out var result) ? null : result;
    }
}