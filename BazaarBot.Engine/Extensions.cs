using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BazaarBot.Engine
{
    public static class Extensions
    {
        public static Dictionary<TKey, TValue> CloneDictionary<TKey, TValue>(Dictionary<TKey, TValue> original) where TValue : ICloneable
        {
            Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>(original.Count,
                                                                    original.Comparer);
            foreach (KeyValuePair<TKey, TValue> entry in original)
            {
                ret.Add(entry.Key, (TValue)entry.Value.Clone());
            }
            return ret;
        }

        public static string RemoveAll(this string s, string toRemove)
        {
            var result = s;
            foreach ( var c in toRemove.AsEnumerable())
            {
                result = result.Replace(c.ToString(), "");
            }
            return result;
        }
    }
}
