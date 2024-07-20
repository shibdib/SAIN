using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerIlluminationClass : PlayerComponentBase
    {
        public event Action<bool> OnPlayerIlluminationChanged;

        public bool Illuminated => TimeSinceIlluminated <= ILLUMINATED_BUFFER_PERIOD;
        public float Level { get; private set; }
        public float TimeSinceIlluminated => Time.time - _timeLastIlluminated;

        private const float ILLUMINATED_BUFFER_PERIOD = 0.5f;

        private float _timeLastIlluminated;

        public PlayerIlluminationClass(PlayerComponent playerComponent) : base(playerComponent)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            checkUpdateIllum();
        }

        public void Dispose()
        {
        }

        private void checkUpdateIllum()
        {
            if (_nextCheckRaycastTime < Time.time) {
                _nextCheckRaycastTime = Time.time + (GlobalSettingsClass.Instance.General.Performance.PerformanceMode ? RAYCAST_FREQ_PERF_MODE : RAYCAST_FREQ);

                bool illuminated = checkIfLightsInRange(out float illumLevel);
                bool lineOfSight = false;
                if (illuminated) {
                    Vector3 bodyPos = Player.MainParts[BodyPartType.body].Position;
                    foreach (var light in _lightsInRange) {
                        LightTrigger trigger = light.Key;
                        if (trigger == null || !trigger.LightActive) continue;

                        Vector3 lightPos = trigger.transform.position;
                        Vector3 direction = bodyPos - lightPos;
                        if (!Physics.Raycast(lightPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask)) {
                            lineOfSight = true;
                            DebugGizmos.Ray(lightPos, direction, Color.red, direction.magnitude, 0.05f, true, 1f);
                            if (light.Value >= illumLevel * 0.75f) {
                                break;
                            }
                        }
                        DebugGizmos.Ray(lightPos, direction, Color.white, direction.magnitude, 0.05f, true, 1f);
                    }
                }

                bool wasIlluminated = Illuminated;
                Level = illumLevel;
                if (illuminated && lineOfSight) {
                    _timeLastIlluminated = Time.time;
                    Logger.LogDebug("illuminated!");
                }

                if (wasIlluminated != Illuminated) {
                    OnPlayerIlluminationChanged?.Invoke(illuminated);
                }
            }
        }

        public void SetIllumination(bool value, float level, LightTrigger trigger, float sqrMagnitude)
        {
            updateLightsDictionary(value, level, trigger);
        }

        private float _nextCheckRaycastTime;
        private const float RAYCAST_FREQ = 0.25f;
        private const float RAYCAST_FREQ_PERF_MODE = 0.5f;

        private bool checkIfLightsInRange(out float illumLevel)
        {
            illumLevel = 0;
            if (_lightsInRange.Count == 0) {
                return false;
            }
            foreach (var light in _lightsInRange) {
                if (light.Value > illumLevel) {
                    illumLevel = light.Value;
                }
            }
            return true;
        }

        private void updateLightsDictionary(bool value, float level, LightTrigger trigger)
        {
            bool inList = _lightsInRange.ContainsKey(trigger);
            if (value) {
                if (inList) {
                    _lightsInRange[trigger] = level;
                }
                else {
                    _lightsInRange.Add(trigger, level);
                }
                return;
            }

            if (!value &&
                inList) {
                _lightsInRange.Remove(trigger);
            }
        }

        private readonly Dictionary<LightTrigger, float> _lightsInRange = new Dictionary<LightTrigger, float>();
    }
}