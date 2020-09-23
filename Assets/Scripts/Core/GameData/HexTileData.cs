using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class HexTileData : IGameData
    {
        private readonly Dictionary<string, HexTilePrototype> _data = new Dictionary<string, HexTilePrototype>();

        private HexTilePrototype _default;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is HexTilePrototype htp)) return;

            if (_data.ContainsKey(htp.Name)) return;

            if (htp.Name == "Default")
            {
                _default = htp;
                return;
            }

            _data[htp.Name] = htp;
        }

        public void OnGameInitialized(Game game)
        {
        }

        public HexTilePrototype GetPrototype(string name) =>
            !_data.TryGetValue(name, out var result) ? _default : result;
    }
}
