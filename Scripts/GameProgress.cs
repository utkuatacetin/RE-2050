using System.Collections.Generic;

namespace ZamaninMektubu
{
    /// <summary>Oyun boyunca her bölümde kazanılan yıldızları tutar (sahne adına göre).</summary>
    public static class GameProgress
    {
        static readonly Dictionary<string, int> stars = new Dictionary<string, int>();

        public static void Record(string sceneName, int count)
        {
            if (!stars.TryGetValue(sceneName, out var old) || count > old) stars[sceneName] = count;
        }

        /// <returns>Kazanılan yıldız; bölüm oynanmadıysa -1.</returns>
        public static int Get(string sceneName) => stars.TryGetValue(sceneName, out var v) ? v : -1;

        public static int Total
        {
            get { int t = 0; foreach (var v in stars.Values) t += v; return t; }
        }

        public static void Reset() => stars.Clear();
    }
}
