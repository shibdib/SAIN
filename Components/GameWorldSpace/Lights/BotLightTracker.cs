using EFT;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components
{
    public class BotLightTracker
    {
        static BotLightTracker()
        {
            GameWorld.OnDispose += dispose;
        }

        public static void AddLight(Light light)
        {
            if (_trackedLights.ContainsKey(light)) {
                //Logger.LogWarning($"{light.GetInstanceID()} is already in light dictionary.");
                return;
            }
            var tracker = light.gameObject.AddComponent<LightComponent>();
            _trackedLights.Add(light, tracker);
        }

        private static void dispose()
        {
            foreach (var light in _trackedLights) {
                GameObject.Destroy(light.Value);
            }
            _trackedLights.Clear();
        }

        public static void LogDictionaryInfo()
        {
            if (_nextlogTime > Time.time) {
                return;
            }
            _nextlogTime = Time.time + 10f;
            Logger.LogDebug($"[{_trackedLights.Count}] lights being tracked currently.");
        }

        private static float _nextlogTime;

        private static readonly Dictionary<Light, LightComponent> _trackedLights = new Dictionary<Light, LightComponent>();
    }
}