using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class SpecialActionData : IGameData
    {
        private readonly Dictionary<string, SpecialActionPrototype> _data =
            new Dictionary<string, SpecialActionPrototype>();

        private SpecialActionPrototype _default;

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is SpecialActionPrototype sap)) return;

            if (_data.ContainsKey(sap.IdentifierName))
            {
                Logger.Log(LogType.Warning, sap.FilePath,
                    $"There is already data with the same name ({sap.IdentifierName}), so it will be ignored!");

                return;
            }

            if (sap.IdentifierName == "Default")
            {
                _default = sap;
                return;
            }

            _data[sap.IdentifierName] = sap;
        }

        public void OnGameInitialized(Game _)
        {
        }

        public SpecialAction GetSpecialActionDirectly(ISpecialActionHolder owner, string name)
        {
            if (_data.TryGetValue(name, out var result)) return result.Create(owner);

            if (HasDefaultValue) return _default.Create(owner);

            Util.CrashGame($"No default value in {nameof(SpecialAction)}");
            return null;
        }
    }
}
