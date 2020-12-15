using System.Collections.Generic;

namespace Core.GameData
{
    public class BattleShipData : IGameData
    {
        private readonly Dictionary<string, BattleShipPrototype> _data = new Dictionary<string, BattleShipPrototype>();

        private BattleShipPrototype _default;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is BattleShipPrototype bsp)) return;

            if (_data.ContainsKey(bsp.IdentifierName))
            {
                Logger.Log(LogType.Warning, bsp.FilePath,
                    $"There is already data with the same name ({bsp.IdentifierName}), so it will be ignored!");

                return;
            }

            if (bsp.IdentifierName == "Default")
            {
                _default = bsp;
                return;
            }

            _data[bsp.IdentifierName] = bsp;
        }

        public void OnGameInitialized(Game game)
        {
        }

        public BattleShipPrototype GetPrototype(string name)
        {
            if (_data.TryGetValue(name, out var result)) return result;

            if (HasDefaultValue) return _default;

            Util.CrashGame($"No default value in {nameof(BattleShipPrototype)}");
            return null;
        }
    }
}
