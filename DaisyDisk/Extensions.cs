using System.Collections.Generic;
using Newtonsoft.Json;

namespace DaisyDisk
{
    public static class Extensions
    {
        #region For Human Readable

        public const long K = 1024;
        public const long M = 1048576;
        public const long G = 1073741824;
        public const long T = 1099511627776;
        public const long P = 1125899906842624;

        private static readonly Dictionary<long, string> s_sizeDict = new()
        {
            {P, "P"},
            {T, "T"},
            {G, "G"},
            {M, "M"},
            {K, "K"},
        };

        public static string ToHumanReadable(this long size)
        {
            foreach (var item in s_sizeDict)
            {
                if (size >= item.Key) return $"{(double)size/item.Key:F1} {item.Value}";
            }

            return $"{size} B";
        }
        
        #endregion

        public static string ToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}