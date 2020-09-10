using System;
using System.IO;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections.Generic;
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
                {"HexTile", path => new HexTilePrototype(path)},
                {"Modifier", path => new ModifierPrototype(path)},
                {"PopSlot", path => new PopSlotPrototype(path)},
                {"Resource", path => new ResourcePrototype(path)},
                {"TileMap", path => new TileMapPrototype(path)},
                {"TileSpecialResourceType", path => new TileSpecialResourceTypePrototype(path)},
            };

        private readonly IReadOnlyDictionary<string, IGameData> _allData =
            new Dictionary<string, IGameData>
            {
                {"HexTile", new HexTileData()},
                {"PopSlot", new PopSlotData()},
                {"Resource", new ResourceData()},
                {"TileMap", new TileMapData()},
                {"TileSpecialResourceType", new TileSpecialResourceTypeData()},
            };

        [MoonSharpHidden]
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
                // null means there were errors when loading lua script
                if (luaHolder == null) continue;

                _allData[luaHolder.TypeName].AddNewData(luaHolder);
            }

            foreach (var kv in _allData)
            {
                if (!kv.Value.HasDefaultValue)
                {
                    // TODO: Should log warning
                }
            }
        }

        [MoonSharpHidden]
        public T GetGameData<T>() where T : IGameData
        {
            const string typeName = nameof(T);

            var gameData = _allData[typeName.Substring(0, typeName.Length - 4)];

            return (T) gameData;
        }

        public IGameData GetGameData(string dataName) =>
            !_allData.TryGetValue(dataName, out var result) ? null : result;
    }
}