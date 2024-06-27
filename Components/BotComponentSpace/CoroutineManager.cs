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

        public Coroutine Add(IEnumerator enumerator, string name)
        {
            if (!_enumerators.ContainsKey(name))
            {
                _enumerators.Add(name, enumerator);
            }
            if (!CoroutinesStarted)
            {
                return null;
            }

            if (_coroutines.TryGetValue(name, out Routine routine))
            {
                if (routine.Coroutine != null)
                {
                    Logger.LogDebug($"Coroutine [{name}] already exits and is active");
                    return routine.Coroutine;
                }
                Logger.LogDebug($"Coroutine [{name}] already but is inactive");
                _coroutines.Remove(name);
            }

            Coroutine coroutine = Component.StartCoroutine(enumerator);
            if (coroutine == null)
            {
                Logger.LogDebug($"Coroutine Null. Not Started [{name}]");
                return null;
            }

            Logger.LogDebug($"Coroutine Started. [{name}]");

            routine = new Routine(enumerator, coroutine, name);
            _coroutines.Add(name, routine);
            return coroutine;
        }

        public void Remove(string name)
        {
            if (!_enumerators.Remove(name))
            {
                Logger.LogDebug($"Enumerator not in list, couldn't remove. [{name}]");
            }
            if (!CoroutinesStarted)
            {
                return;
            }

            if (Component == null)
            {
                Logger.LogError($"Component is null, cannot stop Coroutine!");
                return;
            }
            if (!_coroutines.TryGetValue(name, out Routine routine))
            {
                Logger.LogDebug($"Coroutine Not In Dictionary. [{name}]");
                return;
            }

            if (routine.Coroutine != null)
            {
                Logger.LogDebug($"Coroutine stopped. [{routine.Enumerator.ToString()}] : [{name}]");
                Component.StopCoroutine(routine.Coroutine);
            }
            else
            {
                Logger.LogDebug($"Coroutine already stopped. [{routine.Enumerator.ToString()}] : [{name}]");
            }
            _coroutines.Remove(name);
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
            foreach (var kvp in _enumerators)
            {
                string name = kvp.Key;
                IEnumerator enumerator = kvp.Value;
                Coroutine coroutine = Component.StartCoroutine(enumerator);
                if (coroutine != null)
                {
                    started++;
                    _coroutines.Add(name, new Routine(enumerator, coroutine, name));
                }
            }

            CoroutinesStarted = true;
            if (started > 0)
            {
                Logger.LogDebug($"Started [{started}] Coroutines.");
            }
            else
            {
                Logger.LogDebug($"No Coroutines Started! {_enumerators.Count} enumerators in list.");
            }
            return true;
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

            foreach (var routine in _coroutines.Values)
            {
                if (routine.Coroutine == null)
                {
                    alreadyStopped++;
                    continue;
                }
                component.StopCoroutine(routine.Coroutine);
                stopped++;
            }

            Logger.LogDebug($"Stopped [{stopped}] Coroutines. " +
                $"[{alreadyStopped}] Coroutines were already stopped or null");

            _coroutines.Clear();
        }

        private readonly Dictionary<string, IEnumerator> _enumerators = new Dictionary<string, IEnumerator>();
        private readonly Dictionary<string, Routine> _coroutines = new Dictionary<string, Routine>();
        private readonly T Component;
    }
}