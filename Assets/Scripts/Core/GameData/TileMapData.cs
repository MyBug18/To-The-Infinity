using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class TileMapData : IGameData
    {
        private readonly Dictionary<string, TileMapPrototype> _data = new Dictionary<string, TileMapPrototype>();

        private TileMapPrototype _default;

        public TileMapData() => Instance = this;
        public static TileMapData Instance { get; private set; }

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is TileMapPrototype tmp)) return;

            if (_data.ContainsKey(tmp.IdentifierName))
            {
                Logger.Log(LogType.Warning, tmp.FilePath,
                    $"There is already data with the same name ({tmp.IdentifierName}), so it will be ignored!");

                return;
            }

            if (tmp.IdentifierName == "Default")
            {
                _default = tmp;
                return;
            }

            _data[tmp.IdentifierName] = tmp;
        }

        public void OnGameInitialized(Game _)
        {
        }

        public TileMap CreateDirectly(string name, ITileMapHolder holder, int radius, int? seed)
        {
            if (_data.TryGetValue(name, out var result)) return result.Create(holder, radius, seed);

            if (HasDefaultValue) return _default.Create(holder, radius, seed);

            Util.CrashGame($"No default value in {nameof(TileMapData)}");
            return null;
        }
    }
}
