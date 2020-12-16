using System;
using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class TileSpecialResourceTypeData : IGameData
    {
        private readonly Dictionary<string, TileSpecialResourceTypePrototype> _data =
            new Dictionary<string, TileSpecialResourceTypePrototype>();

        private TileSpecialResourceTypePrototype _default;

        public TileSpecialResourceTypeData() => Instance = this;
        public static TileSpecialResourceTypeData Instance { get; private set; }

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is TileSpecialResourceTypePrototype tsrth)) return;

            if (_data.ContainsKey(tsrth.IdentifierName))
            {
                Logger.Log(LogType.Warning, tsrth.FilePath,
                    $"There is already data with the same name ({tsrth.IdentifierName}), so it will be ignored!");

                return;
            }

            if (tsrth.IdentifierName == "Default")
            {
                _default = tsrth;
                return;
            }

            _data[tsrth.IdentifierName] = tsrth;
        }

        public void OnGameInitialized(Game game)
        {
            throw new NotImplementedException();
        }

        public TileSpecialResourceType GetDirectly(string name)
        {
            if (_data.TryGetValue(name, out var result)) return result.Create();

            if (HasDefaultValue) return _default.Create();

            Util.CrashGame($"No default value in {nameof(TileSpecialResourceTypeData)}");
            return null;
        }
    }
}
