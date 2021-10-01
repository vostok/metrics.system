using System;
using System.Collections.Generic;

namespace Vostok.Metrics.System.Helpers
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> factory)
        {
            if (dict.ContainsKey(key))
                return dict[key];
            var value = factory(key);
            dict[key] = value;
            return value;
        }
    }
}