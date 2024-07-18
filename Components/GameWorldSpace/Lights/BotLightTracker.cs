using EFT;
using EFT.Interactive;
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

        public static void AddLight(Light light, LampController lampController = null)
        {
            if (_trackedLights.ContainsKey(light)) {
                return;
            }
            if (light.range < 0.1f || light.intensity < 0.6f) {
                return;
            }

            var gameObject = new GameObject($"LightComp_{_count++}");
            gameObject.layer = LayerMaskClass.TriggersLayer;

            var component = gameObject.AddComponent<LightComponent>();
            component.Init(light);
            if (lampController != null) {
                component.Init(lampController);
            }

            _trackedLights.Add(light, gameObject);
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
            //Logger.LogDebug($"[{_trackedLights.Count}] lights being tracked currently.");
        }

        private static float _nextlogTime;
        private static int _count;

        private static readonly Dictionary<Light, GameObject> _trackedLights = new Dictionary<Light, GameObject>();
    }
}