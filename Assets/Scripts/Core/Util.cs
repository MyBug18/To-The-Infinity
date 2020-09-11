using System.Collections.Generic;

namespace Core
{
    public static class Util
    {
        public static TValue TryGetKeyWithDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key,
            TValue defaultValue) => dict.TryGetValue(key, out var result) ? result : defaultValue;
    }
}