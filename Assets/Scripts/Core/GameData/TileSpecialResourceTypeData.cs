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

            if (_data.ContainsKey(tsrth.IdentifierName)) return;

            if (tsrth.IdentifierName == "Default")
            {
                _default = tsrth;
                return;
            }

            _data[tsrth.IdentifierName] = tsrth;
        }

        public void OnGameInitialized(Game game)
        {
            throw new System.NotImplementedException();
        }

        public TileSpecialResourceType GetDirectly(string name)
        {
            if (!_data.TryGetValue(name, out var result))
                return HasDefaultValue ? _default.Create() : null;

            return result.Create();
        }
    }
}
