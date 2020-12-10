using System;
using System.IO;
using System.Text;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Core
{
    public enum LogType
    {
        Log,
        Warning,
        Error,
    }

    [MoonSharpUserData]
    public sealed class Logger : IDisposable
    {
        private static Logger _instance;

        private static readonly object Mutex = new object();

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

        public static void Log(LogType logType, string context, string l)
        {
            if (Instance == null) return;

            lock (Mutex)
            {
                switch (logType)
                {
                    case LogType.Log:
                        Instance.Log(context, l);
                        break;
                    case LogType.Warning:
                        Instance.LogWarning(context, l);
                        break;
                    case LogType.Error:
                        Instance.LogError(context, l);
                        break;
                    default:
                        throw new NotImplementedException("Logger type not implemented! :" + logType);
                }
            }
        }

        public void Log(string context, string l)
        {
#if UNITY_EDITOR
            Debug.Log(context + ": " + l);
#endif

            _sw.WriteLine(DateTime.Now.ToString("T") + $" [LOG] {context}: " + l);
        }

        public void LogWarning(string context, string l)
        {
#if UNITY_EDITOR
            Debug.LogWarning(context + ": " + l);
#endif

            _sw.WriteLine(DateTime.Now.ToString("T") + $" [WARNING] {context}: " + l);
        }

        public void LogError(string context, string l)
        {
#if UNITY_EDITOR
            Debug.LogError(context + ": " + l);
#endif

            _sw.WriteLine(DateTime.Now.ToString("T") + $" [ERROR] {context}: " + l);
        }
    }
}
