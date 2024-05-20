using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class LookSettings
    {
        [Name("Global Vision Distance Multiplier")]
        [Description(
            "Multiplies whatever a bot's visible distance is set to. " +
            "Higher is further visible distance, so 1.5 would result in bots seeing 1.5 times further. " +
            "Or if their visible distance is set to 100 meters, they will see at 150 meters instead.")]
        [Default(1f)]
        [MinMax(0.1f, 5f, 100f)]
        public float GlobalVisionDistanceMultiplier = 1;

        [Name("Global Vision Speed Multiplier")]
        [Description(
            "The Base vision speed multiplier, applies to all bots equally, affects all ranges to enemy. " +
            "Bots will see this much faster, or slower, at any range. " +
            "Higher is slower speed, so 1.5 would result in bots taking 1.5 times longer to spot an enemy")]
        [Default(1f)]
        [MinMax(0.01f, 5f, 100f)]
        public float GlobalVisionSpeedModifier = 1;

        [Name("Sprinting Vision Modifier")]
        [Description(
            "Bots will see sprinting players this much faster, at any range." +
            "Higher is slower speed, so 0.66 would result in bots taking 0.66 times longer to spot an sprinting enemy")]
        [Default(0.66f)]
        [MinMax(0.01f, 1f, 100f)]
        public float SprintingVisionModifier = 0.66f;

        [Name("High Elevation Angle Range")]
        [Description(
            "The difference of angle from the bot's vision to the enemy to fully apply HighElevationVisionModifier. " +
            "The modifier is smoothed out by the angle differnce. So 1.2x at +60 degree, 1.1x at +30 degrees...and so on.")]
        [Default(60f)]
        [MinMax(1f, 90f, 1f)]
        public float HighElevationMaxAngle = 60f;

        [Name("High Elevation Vision Modifier")]
        [Description(
            "Bots will see sprinting players this much slower when the enemy's altitude is higher than the bot when the vision angle difference is equal or greater than HighElevationMaxAngle. " +
            "Higher is slower speed, so 1.2 would result in bots taking 20% longer to spot an enemy")]
        [Default(1.2f)]
        [MinMax(1f, 5f, 100f)]
        public float HighElevationVisionModifier = 1.2f;

        [Name("Low Elevation Angle Range")]
        [Description(
            "The difference of angle from the bot's vision to the enemy to fully apply LowElevationVisionModifier. " +
            "The modifier is smoothed out by the angle differnce. So 0.85x at -30 degree, 0.95x at -10 degrees...and so on.")]
        [Default(30f)]
        [MinMax(1f, 90f, 1f)]
        public float LowElevationMaxAngle = 30f;

        [Name("Low Elevation Vision Modifier")]
        [Description(
            "Bots will see sprinting players this much slower when the enemy's altitude is lower than the bot when the vision angle difference is equal or greater than LowElevationMaxAngle. " +
            "Higher is slower speed, so 0.85 would result in bots taking 15% shorter to spot an enemy")]
        [Default(0.85f)]
        [MinMax(0.01f, 1f, 100f)]
        public float LowElevationVisionModifier = 0.85f;

        [Name("Nighttime Vision Modifier")]
        [Description(
            "By how much to lower visible distance at nighttime. " +
            "at the default value of 0.2, bots will see 0.2 times as far, or 20% of " +
            "their base vision distance at night-time.")]
        [Default(0.3f)]
        [MinMax(0.01f, 1f, 100f)]
        [Advanced]
        public float NightTimeVisionModifier = 0.3f;

        [Name("Snow Nighttime Vision Modifier")]
        [Description(
            "By how much to lower visible distance at nighttime in the snow. " +
            "at the default value of 0.2, bots will see 0.2 times as far, or 20% of " +
            "their base vision distance at night-time.")]
        [Default(0.40f)]
        [MinMax(0.01f, 1f, 100f)]
        [Advanced]
        public float NightTimeVisionModifierSnow = 0.40f;

        [Name("Dawn Start Hour")]
        [Default(6f)]
        [MinMax(5f, 8f, 1f)]
        [Advanced]
        public float HourDawnStart = 6f;

        [Name("Dawn End Hour")]
        [Default(8f)]
        [MinMax(6f, 9f, 1f)]
        [Advanced]
        public float HourDawnEnd = 8f;

        [Name("Dusk Start Hour")]
        [Default(20f)]
        [MinMax(19f, 22f, 1f)]
        [Advanced]
        public float HourDuskStart = 20f;

        [Name("Dusk End Hour")]
        [Default(22f)]
        [MinMax(20f, 23f, 1f)]
        [Advanced]
        public float HourDuskEnd = 22f;
    }
}