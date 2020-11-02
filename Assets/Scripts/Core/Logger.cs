using System;
using System.IO;
using System.Text;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Core
{
    [MoonSharpUserData]
    public class Logger : IDisposable
    {
        private static Logger _instance;

        private FileStream _fs;

        private StreamWriter _sw;

        [MoonSharpHidden]
        public static Logger Instance => _instance ??= new Logger();

        [MoonSharpHidden]
        public void Dispose()
        {
            _sw.Close();
            _fs.Close();
        }

        [MoonSharpHidden]
        public void Initialize()
        {
            var logDirectoryPath = Path.Combine(Application.dataPath, "logs");

            var logName = "Log " + DateTime.Now.ToString("yyyyMMdd") + ".txt";

            if (!Directory.Exists(logDirectoryPath))
                Directory.CreateDirectory(logDirectoryPath);

            _fs = new FileStream(Path.Combine(logDirectoryPath, logName),
                FileMode.Create);
            _sw = new StreamWriter(_fs, Encoding.UTF8);
        }

        public void Log(string l)
        {
#if UNITY_EDITOR
            Debug.Log(l);
#endif

            _sw.WriteLine(DateTime.Now.ToString("T") + " [LOG] " + l);
        }

        public void LogWarning(string nameFailed, string l)
        {
#if UNITY_EDITOR
            Debug.LogWarning(l);
#endif

            // _sw.WriteLine(DateTime.Now.ToString("T") + $" [WARNING] {nameFailed} failed: " + l);
        }

        public void LogError(string l)
        {
#if UNITY_EDITOR
            Debug.LogError(l);
#endif

            _sw.WriteLine(DateTime.Now.ToString("T") + " [ERROR] " + l);
        }
    }
}
