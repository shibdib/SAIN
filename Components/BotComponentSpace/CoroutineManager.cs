using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent
{
    public class CoroutineManager<T> where T : MonoBehaviour
    {
        public CoroutineManager(T component)
        {
            Component = component;
        }

        public Coroutine Add(IEnumerator enumerator)
        {
            _enumerators.Add(enumerator);
            Coroutine coroutine = null;
            if (CoroutinesStarted)
            {
                coroutine = startCoroutine(enumerator);
                _coroutines.Add(enumerator, coroutine);
            }
            return coroutine;
        }

        public void Remove(IEnumerator enumerator)
        {
            _enumerators.Remove(enumerator);

            if (CoroutinesStarted)
            {
                if (Component == null)
                {
                    Logger.LogError($"Component is null, cannot stop Coroutine!");
                    return;
                }
                stopCoroutine(enumerator);
                _coroutines.Remove(enumerator);
            }
        }

        private bool stopCoroutine(IEnumerator enumerator)
        {
            if (_coroutines.TryGetValue(enumerator, out Coroutine coroutine))
            {
                Component.StopCoroutine(coroutine);
                return true;
            }
            return false;
        }

        private Coroutine startCoroutine(IEnumerator enumerator)
        {
            return Component.StartCoroutine(enumerator);
        }

        public bool CoroutinesStarted { get; private set; }

        public bool StartCoroutines()
        {
            if (CoroutinesStarted)
            {
                Logger.LogWarning($"Coroutines already started.");
                return false;
            }

            var component = Component;
            if (component == null)
            {
                Logger.LogError($"Component is null, cannot start Coroutines!");
                return false;
            }

            GameObject gameObject = component.gameObject;
            if (gameObject == null)
            {
                Logger.LogError($"Cannot Start Coroutines because gameobject is null.");
                return false;
            }

            if (!gameObject.activeInHierarchy)
            {
                Logger.LogError($"Cannot Start Coroutines because gameobject is inactive.");
                return false;
            }

            int started = 0;
            foreach (IEnumerator enumerator in _enumerators)
            {
                Coroutine coroutine = startCoroutine(enumerator);
                if (coroutine != null)
                {
                    started++;
                    _coroutines.Add(enumerator, coroutine);
                }
            }

            if (started > 0)
            {
                Logger.LogDebug($"Started [{started}] Coroutines.");
                CoroutinesStarted = true;
                return true;
            }
            Logger.LogDebug($"No Coroutines Started! {_enumerators.Count} enumerators in list.");
            return false;
        }

        public void Dispose()
        {
            if (CoroutinesStarted)
            {
                StopCoroutines();
            }
            Component?.StopAllCoroutines();
            _coroutines.Clear();
            _enumerators.Clear();
        }

        public void StopCoroutines()
        {
            if (!CoroutinesStarted)
            {
                Logger.LogWarning($"Coroutines have not been started.");
                return;
            }

            var component = Component;
            if (component == null)
            {
                Logger.LogError($"Component is null, cannot stop Coroutines!");
                return;
            }

            int stopped = 0;
            int alreadyStopped = 0;
            int notInDict = 0;

            foreach (var enumerator in _enumerators)
            {
                if (!_coroutines.TryGetValue(enumerator, out Coroutine coroutine))
                {
                    notInDict++;
                    continue;
                }
                if (coroutine == null)
                {
                    alreadyStopped++;
                    continue;
                }
                component.StopCoroutine(coroutine);
                stopped++;
            }

            Logger.LogDebug($"Stopped [{stopped}] Coroutines. " +
                $"[{alreadyStopped}] Coroutines were already stopped or null, and " +
                $"[{notInDict}] enumerators not present in dictionary");

            _coroutines.Clear();
        }

        private readonly List<IEnumerator> _enumerators = new List<IEnumerator>();

        private readonly Dictionary<IEnumerator, Coroutine> _coroutines = new Dictionary<IEnumerator, Coroutine>();

        private readonly T Component;
    }
}