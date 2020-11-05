using UnityEditor;
using UnityEngine;

namespace Core
{
    public static class GameUtil
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
    }
}
