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

                var script = new Script();

                script.Globals["Logger"] = Logger.Instance;

                script.DoString(File.ReadAllText(path));

                if (!script.Globals.TryGetString("Type", out var typeName,
                    MoonSharpUtil.LoadingError("Type", path)))
                    return;

                if (!LuaHolderMaker.TryGetValue(typeName, out var maker)) return;

                var luaHolder = maker(path);

                if (!luaHolder.Load(script))
                {
                    Logger.Log(LogType.Error, path, "There was error on loading, so it will not be loaded!", true);

                    luaHolderList[i] = null;
                    return;
                }

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
                    Logger.Log(LogType.Warning, "", $"{kv.Key} has no default value. It may result a serious problem!");
            }
        }

        private static void InitializeMoonSharp()
        {
#if UNITY_EDITOR
            UserData.RegisterType<System.Random>();
            UserData.RegisterAssembly();
#endif

            // HardWireType.Initialize();

            SetCustomConverters();
        }

        private static void SetCustomConverters()
        {
            var customConverters = Script.GlobalOptions.CustomConverters;

            customConverters.SetScriptToClrCustomConversion(
                DataType.Table, typeof(HexTileCoord), v =>
                    new HexTileCoord((int)(double)v.Table[1], (int)(double)v.Table[2]));

            customConverters.SetScriptToClrCustomConversion(
                DataType.Tuple, typeof(List<ModifierEffect>), v =>
                {
                    var result = new List<ModifierEffect>();

                    foreach (var kv in v.Table.Pairs)
                    {
                        var additionalInfo = new List<string>();
                        var tokens = kv.Key.String.Split('_');
                        for (var i = 1; i < tokens.Length; i++)
                            additionalInfo.Add(tokens[i]);

                        result.Add(new ModifierEffect(tokens[0], additionalInfo, (int)kv.Value.Number));
                    }

                    return result;
                });
        }

        public T GetGameData<T>() where T : IGameData => (T)GetGameData(typeof(T).Name);

        public IGameData GetGameData(string dataName)
        {
            if (dataName.EndsWith("Data")) dataName = dataName.Substring(0, dataName.Length - 4);

            return !_allData.TryGetValue(dataName, out var result) ? null : result;
        }
    }
}
