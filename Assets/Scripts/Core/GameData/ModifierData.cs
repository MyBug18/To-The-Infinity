using System.Collections.Generic;

namespace Core.GameData
{
    public class ModifierData : IGameData
    {
        private readonly Dictionary<string, ModifierPrototype> _data = new Dictionary<string, ModifierPrototype>();

        private ModifierPrototype _default;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is ModifierPrototype mp)) return;

            if (_data.ContainsKey(mp.Name)) return;

            if (mp.Name == "Default")
            {
                _default = mp;
                return;
            }

            _data[mp.Name] = mp;
        }

        public void OnGameInitialized(Game _)
        {
        }

        public ModifierCore GetModifierDirectly(string name) => !_data.TryGetValue(name, out var proto)
            ? _default.Create()
            : proto.Create();
    }
}
