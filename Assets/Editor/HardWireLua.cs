using Core;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Serialization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class HardWireLua : MonoBehaviour
    {
        [MenuItem("MoonSharp/Generate lua type info file")]
        private static void GenerateLuaFromInfo()
        {
            UserData.RegisterType<Game>();
            UserData.RegisterType<TileMap>();
            UserData.RegisterType<HexTile>();
            UserData.RegisterType<HexTileCoord>();

            File.WriteAllText(Path.Combine(Application.dataPath, "HardWireInfo.lua"),
                UserData.GetDescriptionOfRegisteredTypes(true).Serialize());
        }
    }
}
