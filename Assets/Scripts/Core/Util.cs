using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Core
{
    public static class Util
    {
        public static void CrashGame(string context)
        {
            Logger.Log(LogType.Error, context, "UNRECOVERABLE CRITICAL BUG DETECTED! FORCING GAME CRASH!");

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            return;
#endif

            Application.Quit();
        }

        public static int GetInt(this Dictionary<string, object> dict, string key)
            => dict.TryGetValue(key, out var value) ? Convert.ToInt32(value) : 0;

        public static string GetString(this Dictionary<string, object> dict, string key)
            => dict.TryGetValue(key, out var value) ? Convert.ToString(value) : "";

        public static Dictionary<string, object> GetDict(this Dictionary<string, object> dict, string key)
            => dict.TryGetValue(key, out var value)
                ? JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(value))
                : new Dictionary<string, object>();

        public static List<string> GetStringList(this Dictionary<string, object> dict, string key)
            => dict.TryGetValue(key, out var value)
                ? JsonConvert.DeserializeObject<List<string>>(JsonConvert.SerializeObject(value))
                : new List<string>();

        public static List<Dictionary<string, object>> GetDictList(this Dictionary<string, object> dict, string key)
            => dict.TryGetValue(key, out var value)
                ? JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(value))
                : new List<Dictionary<string, object>>();
    }
}
