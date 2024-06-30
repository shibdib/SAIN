using SAIN.Helpers;
using System;
using System.Text;
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
        public ETimeOfDay TimeOfDay { get; private set; }

        private float _visTime = 0f;

        private float Visibilty()
        {
            float time = calcTime();
            TimeOfDay = getTimeEnum(time);
            float timemodifier = getModifier(time, TimeOfDay);
            //if (_nextTestTime < Time.time)
            //{
            //    StringBuilder builder = new StringBuilder();
            //    _nextTestTime = Time.time + 10f;
            //    for (int i = 0; i < 24;  i++)
            //    {
            //        var timeOFDay = getTimeEnum(i + 1);
            //        float test = getModifier(i + 1, timeOFDay);
            //        builder.AppendLine($"{i + 1} {test} {timeOFDay}");
            //    }
            //    Logger.LogInfo(builder.ToString());
            //}
            return timemodifier;
        }

        private float calcTime()
        {
            var nightSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
            GameDateTime = BotController.Bots.PickRandom().Value.BotOwner.GameDateTime.Calculate();
            float minutes = GameDateTime.Minute / 59f;
            float time = GameDateTime.Hour + minutes;
            time = time.Round100();
            return time;
        }

        private static float getModifier(float time, ETimeOfDay timeOfDay)
        {
            var nightSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
            float max = 1f;
            bool snowActive = GameWorldComponent.Instance.WinterActive;
            float min = snowActive ? nightSettings.NightTimeVisionModifierSnow : nightSettings.NightTimeVisionModifier;
            float ratio;
            float difference;
            float current;
            switch (timeOfDay)
            {
                default:
                    return max;

                case ETimeOfDay.Night:
                    return min;

                case ETimeOfDay.Dawn:
                    difference = nightSettings.HourDawnEnd - nightSettings.HourDawnStart;
                    current = time - nightSettings.HourDawnStart;
                    ratio = current / difference;
                    break;

                case ETimeOfDay.Dusk:
                    difference = nightSettings.HourDuskEnd - nightSettings.HourDuskStart;
                    current = time - nightSettings.HourDuskStart;
                    ratio = 1f - current / difference;
                    break;
            }
            float result = Mathf.Lerp(min, max, ratio);
            return result;
        }

        private static ETimeOfDay getTimeEnum(float time)
        {
            var nightSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
            if (time <= nightSettings.HourDuskStart &&
                time >= nightSettings.HourDawnEnd)
            {
                return ETimeOfDay.Day;
            }
            if (time >= nightSettings.HourDuskEnd ||
                time <= nightSettings.HourDawnStart)
            {
                return ETimeOfDay.Night;
            }
            if (time < nightSettings.HourDawnEnd)
            {
                return ETimeOfDay.Dawn;
            }
            return ETimeOfDay.Dusk;
        }
    }
}