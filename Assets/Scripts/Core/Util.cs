using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public static class Util
    {
        public static TValue TryGetValueWithDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key,
            TValue defaultValue) => dict.TryGetValue(key, out var result) ? result : defaultValue;

        public static IReadOnlyDictionary<string, int> GetModifiersEffect(this IModifierHolder holder)
        {
            var mutex = new object();
            var result = new Dictionary<string, int>();

            Parallel.ForEach(holder.Modifiers,
                () => new Dictionary<string, int>(),
                (m, loop, acc) =>
                {
                    if (!m.IsRelated(holder.TypeName))
                        return acc;

                    var scope = m.Core.Scope[holder.TypeName];

                    if (!scope.CheckCondition(holder))
                        return acc;

                    foreach (var info in scope.GetEffects(holder))
                    {
                        if (!acc.ContainsKey(info.EffectInfo))
                            acc.Add(info.EffectInfo, 0);

                        acc[info.EffectInfo] += info.Amount;
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
