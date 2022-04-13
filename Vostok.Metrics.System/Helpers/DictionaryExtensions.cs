using System.Collections.Generic;
using System.Linq;

namespace Vostok.Metrics.System.Helpers
{
    internal static class DictionaryExtensions
    {
        public static IEnumerable<KeyValuePair<TKey, TValue>> OrEmptyIfNull<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            return source ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>();
        }
    }
}