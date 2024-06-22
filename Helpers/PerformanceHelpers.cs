using SAIN.Components;
using System.Collections;
using System.Diagnostics;

namespace SAIN.Helpers
{
    internal static class PerformanceHelpers
    {
        public static float Loop(int iterations, System.Action action)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                action();
            }
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        public static IEnumerator Loop(int iterations, IEnumerator enumerator)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                yield return GameWorldComponent.Instance.StartCoroutine(enumerator);
            }
            sw.Stop();
        }
    }
}
