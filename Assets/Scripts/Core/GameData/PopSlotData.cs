using System.Collections.Generic;

namespace Core.GameData
{
    public class PopSlotData : IGameData
    {
        private readonly Dictionary<string, PopSlotPrototype> _data = new Dictionary<string, PopSlotPrototype>();

        public IReadOnlyDictionary<string, PopSlotPrototype> Data => _data;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is PopSlotPrototype)) return;

            if (_data.ContainsKey(luaHolder.Name)) return;

            _data[luaHolder.Name] = (PopSlotPrototype) luaHolder;
        }

        public void OnGameInitialized(Game _)
        {
        }
    }
}