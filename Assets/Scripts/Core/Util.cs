using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public static class Util
    {
        public static TValue TryGetValueWithDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key,
            TValue defaultValue) => dict.TryGetValue(key, out var result) ? result : defaultValue;

        public static IReadOnlyDictionary<ResourceInfoHolder, int> GetModifierEffect(this IModifierHolder holder)
        {
            var mutex = new object();
            var result = new Dictionary<ResourceInfoHolder, int>();

            Parallel.ForEach(holder.Modifiers,
                () => new Dictionary<ResourceInfoHolder, int>(),
                (m, loop, acc) =>
                {
                    if (m.Core.TargetType != holder.TypeName || !m.Core.CheckCondition(holder))
                        return acc;

                    foreach (var info in m.Core.GetEffects(holder))
                    {
                        if (!acc.ContainsKey(info.ResourceInfo))
                            acc.Add(info.ResourceInfo, 0);

                        acc[info.ResourceInfo] += info.Amount;
                    }

                    return acc;
                },
                final =>
                {
                    lock (mutex)
                    {
                        foreach (var kv in final)
                        {
                            if (!result.ContainsKey(kv.Key))
                                result.Add(kv.Key, 0);

                            result[kv.Key] += kv.Value;
                        }
                    }
                });

            return result;
        }
    }
}