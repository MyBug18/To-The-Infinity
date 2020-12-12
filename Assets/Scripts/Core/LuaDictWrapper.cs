using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core
{
    /// <summary>
    ///     Simple dictionary wrapper for lua
    /// </summary>
    [MoonSharpUserData]
    public sealed class LuaDictWrapper
    {
        public LuaDictWrapper(Dictionary<string, object> data) => Data = data;

        [MoonSharpHidden]
        public Dictionary<string, object> Data { get; }

        public object Get(string key, object defaultValue) =>
            Data.TryGetValue(key, out var result) ? result : defaultValue;

        public void Set(string key, object value)
        {
            if (!value.GetType().IsValueType || value.GetType() != typeof(string)) return;

            Data[key] = value;
        }
    }
}
