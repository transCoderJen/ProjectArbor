using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ShiftedSignal.Garden.Misc
{
    public static class LoadProfiler
    {
        public static Stopwatch Start(string name)
        {
            return Stopwatch.StartNew();
        }

        public static void End(string name, Stopwatch watch)
        {
            watch.Stop();
            Debug.Log($"[LoadProfiler] {name}: {watch.ElapsedMilliseconds} ms");
        }
    }
}