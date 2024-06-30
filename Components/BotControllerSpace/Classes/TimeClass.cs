using System;
using UnityEngine;

namespace SAIN.Components.BotController
{
    public class TimeClass : SAINControl
    {
        public TimeClass(SAINBotController botController) : base(botController)
        {
        }

        public void Update()
        {
            if (Bots == null || Bots.Count == 0)
            {
                return;
            }
            if (_visTime < Time.time)
            {
                _visTime = Time.time + 5f;
                TimeVisionDistanceModifier = Visibilty();
                TimeGainSightModifier = 2f - TimeVisionDistanceModifier;
            }
        }

        public DateTime GameDateTime { get; private set; }
        public float TimeVisionDistanceModifier { get; private set; } = 1f;
        public float TimeGainSightModifier { get; private set; } = 1f;
        public TimeOfDayEnum TimeOfDay { get; private set; }

        private float _visTime = 0f;

        private float Visibilty()
        {
            var nightSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
            GameDateTime = BotController.Bots.PickRandom().Value.BotOwner.GameDateTime.Calculate();
            float minutes = GameDateTime.Minute / 59f;
            float time = GameDateTime.Hour + minutes;

            float timemodifier = 1f;
            // SeenTime Check
            if (time >= nightSettings.HourDuskStart || time <= nightSettings.HourDawnEnd)
            {
                // Night
                if (time > nightSettings.HourDuskEnd || time < nightSettings.HourDawnStart)
                {
                    TimeOfDay = TimeOfDayEnum.Night;
                    timemodifier = nightSettings.NightTimeVisionModifier;
                }
                else
                {
                    float scalingA = 1f - nightSettings.NightTimeVisionModifier;
                    float scalingB = nightSettings.NightTimeVisionModifier;

                    if (GameWorldComponent.Instance.WinterActive)
                    {
                        scalingA = 1f - nightSettings.NightTimeVisionModifierSnow;
                        scalingB = nightSettings.NightTimeVisionModifierSnow;
                    }

                    // Dawn
                    if (time <= nightSettings.HourDawnEnd)
                    {
                        TimeOfDay = TimeOfDayEnum.Dawn;
                        float dawnDiff = nightSettings.HourDawnEnd - nightSettings.HourDawnStart;
                        float dawnHours = (time - nightSettings.HourDawnStart) / dawnDiff;
                        float scaledDawnHours = dawnHours * scalingA + scalingB;

                        // assigns modifier to our output
                        timemodifier = scaledDawnHours;
                    }
                    // Dusk
                    else if (time >= nightSettings.HourDuskStart)
                    {
                        TimeOfDay = TimeOfDayEnum.Dusk;
                        float duskDiff = nightSettings.HourDuskEnd - nightSettings.HourDuskStart;
                        float duskHours = (time - nightSettings.HourDuskStart) / duskDiff;
                        float scaledDuskHours = duskHours * scalingA + scalingB;

                        // Inverse Scale to reduce modifier as night falls
                        float inverseScaledDuskHours = 1f - scaledDuskHours;

                        // assigns modifier to our output
                        timemodifier = inverseScaledDuskHours;
                    }
                }
            }
            // Day
            else
            {
                TimeOfDay = TimeOfDayEnum.Day;
                timemodifier = 1f;
            }
            if (SAINPlugin.DebugMode || true)
            {
                Logger.LogInfo($"Time Vision Modifier: [{timemodifier}] at [{time}] with Config Settings VisionModifier: [{nightSettings.NightTimeVisionModifier}]");
            }
            return timemodifier;
        }
    }
}