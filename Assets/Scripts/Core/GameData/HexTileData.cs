using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class HexTileData : IGameData
    {
        private readonly Dictionary<string, HexTilePrototype> _data = new Dictionary<string, HexTilePrototype>();

        private HexTilePrototype _default;

        public HexTileData() => Instance = this;
        public static HexTileData Instance { get; private set; }

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is HexTilePrototype htp)) return;

            if (_data.ContainsKey(htp.IdentifierName))
            {
                Logger.Log(LogType.Warning, htp.FilePath,
                    $"There is already data with the same name ({htp.IdentifierName}), so it will be ignored!");

                return;
            }

            if (htp.IdentifierName == "Default")
            {
                _default = htp;
                return;
            }

            _data[htp.IdentifierName] = htp;
        }

        public void OnGameInitialized(Game game)
        {
        }

        public HexTilePrototype GetPrototype(string name)
        {
            if (_data.TryGetValue(name, out var result)) return result;

            if (HasDefaultValue) return _default;

            Util.CrashGame($"No default value in {nameof(HexTileData)}");
            return null;
        }
    }
}
