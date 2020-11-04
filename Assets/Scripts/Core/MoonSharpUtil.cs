using System;
using MoonSharp.Interpreter;

namespace Core
{
    public readonly struct LogInfo
    {
        public string Context { get; }

        public LogType LogType { get; }

        public bool LockMutex { get; }

        public bool AllowNotDefined { get; }

        public LogInfo(string context, LogType logType, bool lockMutex, bool allowNotDefined)
        {
            Context = context;
            LogType = logType;
            LockMutex = lockMutex;
            AllowNotDefined = allowNotDefined;
        }
    }

    public static class MoonSharpUtil
    {
        public static LogInfo LoadingError(string fieldName, string filePath)
            => new LogInfo($"Field {fieldName} of " + filePath, LogType.Error, true, false);

        public static LogInfo AllowNotDefined(string fieldName, string filePath)
            => new LogInfo($"Field {fieldName} of " + filePath, LogType.Error, true, true);

        #region TryGet

        private static bool TryGetDynValue(this Table table, string key, out DynValue result, LogInfo info = default)
        {
            var dynValue = table.RawGet(key);
            result = dynValue;

            // no value
            if (dynValue == null)
            {
                if (info.Context != null && !info.AllowNotDefined)
                    Logger.Log(info.LogType, info.Context, "The value is not defined!", info.LockMutex);

                return false;
            }

            if (!dynValue.IsNil())
                return true;

            if (info.Context != null)
                Logger.Log(info.LogType, info.Context, "The value is nil!", info.LockMutex);

            return false;
        }

        public static bool TryGetString(this DynValue dynValue, out string result, LogInfo info = default)
        {
            if (dynValue.Type != DataType.String)
            {
                if (info.Context != null)
                    Logger.Log(info.LogType, info.Context, "Value is defined, but it's not a string!", info.LockMutex);

                result = null;
                return false;
            }

            result = dynValue.String;
            return true;
        }

        public static bool TryGetString(this Table table, string key, out string result, LogInfo info = default)
        {
            if (table.TryGetDynValue(key, out var dynValue, info))
                return dynValue.TryGetString(out result, info);

            result = null;
            return false;
        }

        public static bool TryGetInt(this DynValue dynValue, out int result, LogInfo info = default)
        {
            if (dynValue.Type != DataType.Number)
            {
                if (info.Context != null)
                    Logger.Log(info.LogType, info.Context, "Value is defined, but it's not a int!");

                result = 0;
                return false;
            }

            result = (int)dynValue.Number;
            return true;
        }

        public static bool TryGetInt(this Table table, string key, out int result, LogInfo info = default)
        {
            if (table.TryGetDynValue(key, out var dynValue, info))
                return dynValue.TryGetInt(out result, info);

            result = 0;
            return false;
        }

        public static bool TryGetFloat(this DynValue dynValue, out float result, LogInfo info = default)
        {
            if (dynValue.Type != DataType.Number)
            {
                if (info.Context != null)
                    Logger.Log(info.LogType, info.Context, "Value is defined, but it's not a float!");

                result = 0;
                return false;
            }

            result = (float)dynValue.Number;
            return true;
        }

        public static bool TryGetFloat(this Table table, string key, out float result, LogInfo info = default)
        {
            if (table.TryGetDynValue(key, out var dynValue, info))
                return dynValue.TryGetFloat(out result, info);

            result = 0;
            return false;
        }

        public static bool TryGetBool(this DynValue dynValue, out bool result, LogInfo info = default)
        {
            if (dynValue.Type != DataType.Boolean)
            {
                if (info.Context != null)
                    Logger.Log(info.LogType, info.Context, "Value is defined, but it's not a int!");

                result = false;
                return false;
            }

            result = dynValue.Boolean;
            return true;
        }

        public static bool TryGetBool(this Table table, string key, out bool result, LogInfo info = default)
        {
            if (table.TryGetDynValue(key, out var dynValue, info))
                return dynValue.TryGetBool(out result, info);

            result = false;
            return false;
        }

        public static bool TryGetTable(this DynValue dynValue, out Table result, LogInfo info = default)
        {
            if (dynValue.Type != DataType.Table)
            {
                if (info.Context != null)
                    Logger.Log(info.LogType, info.Context, "Value is defined, but it's not a table!", info.LockMutex);

                result = null;
                return false;
            }

            result = dynValue.Table;
            return true;
        }

        public static bool TryGetTable(this Table table, string key, out Table result, LogInfo info = default)
        {
            if (table.TryGetDynValue(key, out var dynValue, info))
                return dynValue.TryGetTable(out result, info);

            result = null;
            return false;
        }

        public static bool TryGetLuaFunc<T>(this DynValue dynValue, out ScriptFunctionDelegate<T> result,
            LogInfo info = default)
        {
            if (dynValue.Type != DataType.Function)
            {
                if (info.Context != null)
                    Logger.Log(info.LogType, info.Context, "Value is defined, but it's not a function!",
                        info.LockMutex);

                result = null;
                return false;
            }

            result = dynValue.Function.GetDelegate<T>();
            return true;
        }

        public static bool TryGetLuaFunc<T>(this Table table, string key, out ScriptFunctionDelegate<T> result,
            LogInfo info = default)
        {
            if (table.TryGetDynValue(key, out var dynValue, info))
                return dynValue.TryGetLuaFunc(out result, info);

            result = null;
            return false;
        }

        public static bool TryGetLuaAction(this DynValue dynValue, out ScriptFunctionDelegate result,
            LogInfo info = default)
        {
            if (dynValue.Type != DataType.Function)
            {
                if (info.Context != null)
                    Logger.Log(info.LogType, info.Context, "Value is defined, but it's not a function!",
                        info.LockMutex);

                result = null;
                return false;
            }

            result = dynValue.Function.GetDelegate();
            return true;
        }

        public static bool TryGetLuaAction(this Table table, string key, out ScriptFunctionDelegate result,
            LogInfo info = default)
        {
            if (table.TryGetDynValue(key, out var dynValue, info))
                return dynValue.TryGetLuaAction(out result, info);

            result = null;
            return false;
        }
        #endregion

        public static bool TryInvoke(this ScriptFunctionDelegate f, string funcName, string context,
            params object[] args)
        {
            try
            {
                f.Invoke(args);
                return true;
            }
            catch (Exception)
            {
                Logger.Log(LogType.Error, $"Function {funcName} of" + context,
                    "Function call failed! It may cause a serious problem, resulting game crash!");
                return false;
            }
        }

        public static bool TryInvoke<T>(this ScriptFunctionDelegate<T> f, string funcName, string context,
            out T result, params object[] args)
        {
            try
            {
                result = f.Invoke(args);
                return true;
            }
            catch (Exception)
            {
                Logger.Log(LogType.Error, $"Function {funcName} of" + context,
                    "Function call failed! It may cause a serious problem, resulting a game crash!");

                result = default;
                return false;
            }
        }
    }
}
