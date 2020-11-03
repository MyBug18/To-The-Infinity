using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.GameData;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Core
{
    public sealed class GameDataStorage
    {
        private static GameDataStorage _instance;

        private static readonly IReadOnlyDictionary<string, Func<string, ILuaHolder>> LuaHolderMaker =
            new Dictionary<string, Func<string, ILuaHolder>>
            {
                {"HexTile", path => new HexTilePrototype(path)},
                {"Modifier", path => new ModifierPrototype(path)},
                {"PopSlot", path => new PopSlotPrototype(path)},
                {"Resource", path => new ResourcePrototype(path)},
                {"SpecialAction", path => new SpecialActionPrototype(path)},
                {"TileMap", path => new TileMapPrototype(path)},
                {"TileSpecialResourceType", path => new TileSpecialResourceTypePrototype(path)},
            };

        private readonly IReadOnlyDictionary<string, IGameData> _allData =
            new Dictionary<string, IGameData>
            {
                {"HexTile", new HexTileData()},
                {"Modifier", new ModifierData()},
                {"PopSlot", new PopSlotData()},
                {"Resource", new ResourceData()},
                {"SpecialAction", new SpecialActionData()},
                {"TileMap", new TileMapData()},
                {"TileSpecialResourceType", new TileSpecialResourceTypeData()},
            };

        public static GameDataStorage Instance => _instance ??= new GameDataStorage();

        public void Initialize()
        {
            InitializeMoonSharp();
            Noise2d.InitializeGradSeed(null);
            Logger.Instance.Initialize();

            var pathList = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "CoreData"),
                "*.lua", SearchOption.AllDirectories);

            var luaHolderList = new ILuaHolder[pathList.Length];

            Parallel.For(0, luaHolderList.Length, i =>
            {
                var path = pathList[i];

                luaHolderList[i] = null;

                var script = new Script();

                script.Globals["Logger"] = Logger.Instance;

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

        private static void InitializeMoonSharp()
        {
#if UNITY_EDITOR
            UserData.RegisterType<System.Random>();
            UserData.RegisterAssembly();
#endif

            // HardWireType.Initialize();

            var customConverters = Script.GlobalOptions.CustomConverters;

            customConverters.SetScriptToClrCustomConversion(
                DataType.Table, typeof(HexTileCoord), v =>
                    new HexTileCoord((int)v.Table.Get("Q").Number, (int)v.Table.Get("R").Number));
        }

        public T GetGameData<T>() where T : IGameData => (T)GetGameData(typeof(T).Name);

        public IGameData GetGameData(string dataName)
        {
            if (dataName.EndsWith("Data")) dataName = dataName.Substring(0, dataName.Length - 4);

            return !_allData.TryGetValue(dataName, out var result) ? null : result;
        }
    }
}
