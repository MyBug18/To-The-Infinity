using System.IO;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public class ScriptHolder
    {
        public readonly string Path;

        public readonly Script LuaScript;
    }

    public class GameDataStroage
    {
        public void Initialize()
        {
            // HardWireType.Initialize();

            var scriptList = new List<(string path, Script script)>();

            foreach (var f in 
                Directory.EnumerateFiles(Path.Combine(Application.dataPath, "Core"), 
                "*.lua", SearchOption.AllDirectories))
            {
                var script = new Script();
                scriptList.Add((f, script));
            }

            Parallel.ForEach(scriptList, value =>
            {
                value.script.LoadFile(value.path);
            });
        }
    }
}