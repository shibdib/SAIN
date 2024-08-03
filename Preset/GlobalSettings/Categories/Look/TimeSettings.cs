using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class TimeSettings : SAINSettingsBase<TimeSettings>, ISAINSettings
    {
        [Name("Nighttime Vision Modifier")]
        [Description(
            "By how much to lower visible distance at nighttime. " +
            "at the default value of 0.2, bots will see 0.2 times as far, or 20% of " +
            "their base vision distance at night-time.")]
        [MinMax(0.01f, 1f, 100f)]
        public float NightTimeVisionModifier = 0.2f;

        [Name("Snow Nighttime Vision Modifier")]
        [Description(
            "By how much to lower visible distance at nighttime in the snow. " +
            "at the default value of 0.2, bots will see 0.2 times as far, or 20% of " +
            "their base vision distance at night-time.")]
        [MinMax(0.01f, 1f, 100f)]
        public float NightTimeVisionModifierSnow = 0.35f;

        [Name("Dawn Start Hour")]
        [MinMax(5f, 8f, 10f)]
        [Advanced]
        public float HourDawnStart = 6f;

        [Name("Dawn End Hour")]
        [MinMax(6f, 9f, 10f)]
        [Advanced]
        public float HourDawnEnd = 8f;

        [Name("Dusk Start Hour")]
        [MinMax(19f, 22f, 10f)]
        [Advanced]
        public float HourDuskStart = 20f;

        [Name("Dusk End Hour")]
        [MinMax(20f, 23f, 10f)]
        [Advanced]
        public float HourDuskEnd = 22f;
    }
}