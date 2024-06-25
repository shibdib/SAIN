using System.Collections;
using UnityEngine;

namespace SAIN.SAINComponent
{
    public struct Routine
    {
        public Routine(IEnumerator enumerator, Coroutine coroutine, string name)
        {
            Enumerator = enumerator;
            Coroutine = coroutine;
            Name = name;
        }

        public string Name { get; }
        public IEnumerator Enumerator { get; }
        public Coroutine Coroutine { get; }
    }
}