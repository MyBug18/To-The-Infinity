using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class TileSpecialResourceTypeData : IGameData
    {
        private readonly Dictionary<string, TileSpecialResourceTypePrototype> _data =
            new Dictionary<string, TileSpecialResourceTypePrototype>();

        private TileSpecialResourceTypePrototype _default;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is TileSpecialResourceTypePrototype tsrth)) return;

            if (_data.ContainsKey(tsrth.Name)) return;

            if (tsrth.Name == "Default")
            {
                _default = tsrth;
                return;
            }

            _data[tsrth.Name] = tsrth;
        }

        public void OnGameInitialized(Game game)
        {
            throw new System.NotImplementedException();
        }

        public TileSpecialResourceTypePrototype GetPrototype(string name)
        {
            if (!_data.TryGetValue(name, out var result))
                return HasDefaultValue ? _default : null;

            return result;
        }
    }
}
