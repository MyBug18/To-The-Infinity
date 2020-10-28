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

		[MoonSharpHidden]
		public static Logger Instance => _instance ??= new Logger();

		private FileStream _fs;

		private StreamWriter _sw;

		[MoonSharpHidden]
		public void Initialize()
		{
			var logDirectoryPath = Path.Combine(Application.dataPath, "log");

			if (!Directory.Exists(logDirectoryPath))
				Directory.CreateDirectory(logDirectoryPath);

			_fs = new FileStream(Path.Combine(logDirectoryPath, "Log-" + DateTime.Now.ToString("O") + ".txt"),
				FileMode.Create, FileAccess.Write);
			_sw = new StreamWriter(_fs, Encoding.UTF8);
		}

		public void Log(string l)
		{
#if UNITY_EDITOR
			Debug.Log(l);
#endif

			_sw.WriteLine(DateTime.Now.ToString("T") + " [LOG] " + l);
		}

		public void LogWarning(string l)
		{
#if UNITY_EDITOR
			Debug.LogWarning(l);
#endif

			_sw.WriteLine(DateTime.Now.ToString("T") + " [WARNING] " + l);
		}

		public void LogError(string l)
		{
#if UNITY_EDITOR
			Debug.LogError(l);
#endif

			_sw.WriteLine(DateTime.Now.ToString("T") + " [ERROR] " + l);
		}

		[MoonSharpHidden]
		public void Dispose()
		{
			_fs.Close();
			_sw.Close();
		}
	}
}