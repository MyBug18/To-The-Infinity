using System;
using System.IO;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.GameData;

namespace Core
{
    public class GameDataStorage
    {
        private static GameDataStorage _instance;

        public static GameDataStorage Instance => _instance ??= new GameDataStorage();

        private static readonly IReadOnlyDictionary<string, Func<string, ILuaHolder>> LuaHolderMaker =
            new Dictionary<string, Func<string, ILuaHolder>>
            {
                {"PopSlot", path => new PopSlotPrototype(path)},
            };

        private readonly IReadOnlyDictionary<string, Dictionary<string, ILuaHolder>> _allData =
            new Dictionary<string, Dictionary<string, ILuaHolder>>
            {
                {"PopSlot", new Dictionary<string, ILuaHolder>()},
            };

        public void Initialize()
        {
            // HardWireType.Initialize();

            var pathList = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "CoreData"),
                    "*.lua", SearchOption.AllDirectories);

            var luaHolderList = new ILuaHolder[pathList.Length];

            Parallel.For(0, luaHolderList.Length, i =>
            {
                var path = pathList[i];

                luaHolderList[i] = null;

                var script = new Script();

                script.DoString(File.ReadAllText(path));

                var type = script.Globals.Get("Type");

                // TODO: Throw exception or log warning
                if (type.IsNil()) return;
                var typeName = type.String;

                if (!LuaHolderMaker.TryGetValue(typeName, out var maker)) return;

                var luaHolder = maker(path);

                if (!luaHolder.Load(script)) return;

                luaHolderList[i] = luaHolder;
            });

            foreach (var luaHolder in luaHolderList)
            {
                if (luaHolder == null) continue;

                _allData[luaHolder.TypeName].Add(luaHolder.Name, luaHolder);
            }

            foreach (var luaHolders in _allData.Values)
            {
                var invalidList = (
                    from luaHolder in luaHolders
                    where !luaHolder.Value.IsValid
                    select luaHolder.Key).ToList();

                foreach (var s in invalidList)
                    luaHolders.Remove(s);
            }
        }

        [MoonSharpHidden]
        public T GetData<T>(string name) where T : ILuaHolder
        {
            const string typeName = nameof(T);

            var luaHolders = _allData[typeName.Substring(0, typeName.Length - 4)];

            if (!luaHolders.ContainsKey(name)) return default;
            return (T) luaHolders[name];
        }

        [MoonSharpHidden]
        public bool DataExist<T>(string name) where T : ILuaHolder
        {
            const string typeName = nameof(T);

            var luaHolders = _allData[typeName.Substring(0, typeName.Length - 4)];

            return luaHolders.ContainsKey(name);
        }
    }
}