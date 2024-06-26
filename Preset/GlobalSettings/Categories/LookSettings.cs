using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class LookSettings : SAINSettingsBase<LookSettings>, ISAINSettings
    {
        [Name("Global Vision Distance Multiplier")]
        [Description(
            "Multiplies whatever a bot's visible distance is set to. " +
            "Higher is further visible distance, so 1.5 would result in bots seeing 1.5 times further. " +
            "Or if their visible distance is set to 100 meters, they will see at 150 meters instead.")]
        [MinMax(0.1f, 5f, 100f)]
        public float GlobalVisionDistanceMultiplier = 1;

        [Name("Global Vision Speed Multiplier")]
        [Description(
            "The Base vision speed multiplier, applies to all bots equally, affects all ranges to enemy. " +
            "Bots will see this much faster, or slower, at any range. " +
            "Higher is slower speed, so 1.5 would result in bots taking 1.5 times longer to spot an enemy")]
        [MinMax(0.01f, 5f, 100f)]
        public float GlobalVisionSpeedModifier = 1;

        [Name("Sprinting Vision Modifier")]
        [Description(
            "Bots will see sprinting players this much faster, at any range." +
            "Higher is slower speed, so 0.66 would result in bots spotting an enemy who is sprinting 0.66x faster. So if they usually would take 10 seconds to spot someone, it would instead take around 6.6 seconds.")]
        [MinMax(0.01f, 1f, 100f)]
        public float SprintingVisionModifier = 0.5f;

        [Name("Movement Vision Distance Modifier")]
        [Description(
            "Bots will see moving players this much further. " +
            "Higher is further distance, so 1.75 would result in bots seeing enemies 1.75x further at max player speed. " +
            "Scales with player velocity.")]
        [MinMax(1f, 3f, 100f)]
        public float MovementDistanceModifier = 1.5f;

        [Name("High Elevation Angle Range")]
        [Description(
            "The difference of angle from the bot's vision to the enemy to fully apply HighElevationVisionModifier. " +
            "The modifier is smoothed out by the angle differnce. So 1.2x at +60 degree, 1.1x at +30 degrees...and so on.")]
        [MinMax(1f, 90f, 1f)]
        public float HighElevationMaxAngle = 60f;

        [Name("High Elevation Vision Modifier")]
        [Description(
            "Bots will see players this much slower when the enemy's altitude is higher than the bot when the vision angle difference is equal or greater than HighElevationMaxAngle. " +
            "Higher is slower speed, so 1.2 would result in bots taking 20% longer to spot an enemy")]
        [MinMax(1f, 5f, 100f)]
        public float HighElevationVisionModifier = 1.5f;

        [Name("Low Elevation Angle Range")]
        [Description(
            "The difference of angle from the bot's vision to the enemy to fully apply LowElevationVisionModifier. " +
            "The modifier is smoothed out by the angle differnce. So 0.85x at -30 degree, 0.95x at -10 degrees...and so on.")]
        [MinMax(1f, 90f, 1f)]
        public float LowElevationMaxAngle = 30f;

        [Name("Low Elevation Vision Modifier")]
        [Description(
            "Bots will see sprinting players this much slower when the enemy's altitude is lower than the bot when the vision angle difference is equal or greater than LowElevationMaxAngle. " +
            "Higher is slower speed, so 0.85 would result in bots taking 15% shorter to spot an enemy")]
        [MinMax(0.01f, 1f, 100f)]
        public float LowElevationVisionModifier = 0.75f;

        [Name("Bot Reaction and Accuracy Changes Toggle - Experimental")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("Experimental: Bots will have slightly reduced accuracy and vision speed if you are not looking in their direction. " +
            "So if a bot notices and starts shooting you while your back is turned, they will be less accurate and notice you more slowly.")]
        public bool NotLookingToggle = true;

        [Name("Bot Reaction and Accuracy Changes Time Limit")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("The Maximum Time that a bot can be shooting at you before the reduced spread not longer has an affect. " +
            "So if a bot is shooting at you from the back for X seconds, after that time it will no longer reduce their accuracy to give you a better chance to react.")]
        [MinMax(0.5f, 20f, 100f)]
        [Advanced]
        public float NotLookingTimeLimit = 4f;

        [Name("Bot Reaction and Accuracy Changes Angle")]
        [Section("Unseen Bot")]
        [Experimental]
        [Advanced]
        [Description("The Maximum Angle for the player to be considered looking at a bot.")]
        [MinMax(5f, 45f, 1f)]
        public float NotLookingAngle = 45f;

        [Name("Bot Reaction Multiplier When Out of Sight")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("How much to multiply bot vision speed by if you aren't looking at them when they notice you. Higher = More time before reacting.")]
        [MinMax(1f, 2f, 100f)]
        [Advanced]
        public float NotLookingVisionSpeedModifier = 1.1f;

        [Name("Bot Accuracy and Spread Increase When Out of Sight")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("How much additional random Spread to add to a bot's aim if the player isn't look at them." +
            " 1 means it will randomize in a 1 meter sphere around their original aim target in addition to existing random spread." +
            " Higher = More spread and less accurate bots.")]
        [MinMax(0.1f, 1.5f, 100f)]
        [Advanced]
        public float NotLookingAccuracyAmount = 0.33f;

        [Name("Nighttime Vision Modifier")]
        [Description(
            "By how much to lower visible distance at nighttime. " +
            "at the default value of 0.2, bots will see 0.2 times as far, or 20% of " +
            "their base vision distance at night-time.")]
        [MinMax(0.01f, 1f, 100f)]
        [Advanced]
        public float NightTimeVisionModifier = 0.3f;

        [Name("Snow Nighttime Vision Modifier")]
        [Description(
            "By how much to lower visible distance at nighttime in the snow. " +
            "at the default value of 0.2, bots will see 0.2 times as far, or 20% of " +
            "their base vision distance at night-time.")]
        [MinMax(0.01f, 1f, 100f)]
        [Advanced]
        public float NightTimeVisionModifierSnow = 0.40f;

        [Name("Dawn Start Hour")]
        [MinMax(5f, 8f, 1f)]
        [Advanced]
        public float HourDawnStart = 6f;

        [Name("Dawn End Hour")]
        [MinMax(6f, 9f, 1f)]
        [Advanced]
        public float HourDawnEnd = 8f;

        [Name("Dusk Start Hour")]
        [MinMax(19f, 22f, 1f)]
        [Advanced]
        public float HourDuskStart = 20f;

        [Name("Dusk End Hour")]
        [MinMax(20f, 23f, 1f)]
        [Advanced]
        public float HourDuskEnd = 22f;
    }
}