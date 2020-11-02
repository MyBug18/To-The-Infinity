using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core
{
    /// <summary>
    ///     Simple dictionary wrapper for lua
    /// </summary>
    [MoonSharpUserData]
    public class LuaDictWrapper
    {
        private readonly Dictionary<string, object> _v;

        public LuaDictWrapper(Dictionary<string, object> v) => _v = v;

        public object GetValue(string key, object defaultValue) =>
            _v.TryGetValue(key, out var result) ? result : defaultValue;

        public void SetValue(string key, object value)
        {
            if (!value.GetType().IsValueType || value.GetType() != typeof(string)) return;

            _v[key] = value;
        }
    }
}
