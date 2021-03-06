﻿using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class ModifierData : IGameData
    {
        private readonly Dictionary<string, ModifierPrototype> _data = new Dictionary<string, ModifierPrototype>();

        private ModifierPrototype _default;

        public ModifierData() => Instance = this;
        public static ModifierData Instance { get; private set; }

        public bool HasDefaultValue => _default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is ModifierPrototype mp)) return;

            if (_data.ContainsKey(mp.IdentifierName))
            {
                Logger.Log(LogType.Warning, mp.FilePath,
                    $"There is already data with the same name ({mp.IdentifierName}), so it will be ignored!");

                return;
            }

            if (mp.IdentifierName == "Default")
            {
                _default = mp;
                return;
            }

            _data[mp.IdentifierName] = mp;
        }

        public void OnGameInitialized(Game _)
        {
        }

        public ModifierCore GetModifierDirectly(string name)
        {
            if (_data.TryGetValue(name, out var proto)) return proto.Create();

            if (HasDefaultValue) return _default.Create();

            Util.CrashGame($"No default value in {nameof(ModifierData)}");
            return null;
        }
    }
}
