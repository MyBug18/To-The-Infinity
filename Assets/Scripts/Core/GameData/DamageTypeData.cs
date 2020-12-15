using System.Collections.Generic;

namespace Core.GameData
{
    public class DamageTypeData : IGameData
    {
        public static DamageTypeData Instance { get; private set; }

        private readonly Dictionary<string, DamageTypePrototype> _data = new Dictionary<string, DamageTypePrototype>();

        private DamageTypePrototype _default;

        public DamageTypeData() => Instance = this;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is DamageTypePrototype dtp)) return;

            if (_data.ContainsKey(dtp.IdentifierName))
            {
                Logger.Log(LogType.Warning, dtp.FilePath,
                    $"There is already data with the same name ({dtp.IdentifierName}), so it will be ignored!");

                return;
            }

            if (dtp.IdentifierName == "Default")
            {
                _default = dtp;
                return;
            }

            _data[dtp.IdentifierName] = dtp;
        }

        public void OnGameInitialized(Game game)
        {
        }

        public bool ExistsDamageType(string typeName) => _data.ContainsKey(typeName);

        public DamageTypePrototype GetPrototype(string name)
        {
            if (_data.TryGetValue(name, out var result)) return result;

            if (HasDefaultValue) return _default;

            Util.CrashGame($"No default value in {nameof(DamageTypeData)}");
            return null;
        }
    }
}
