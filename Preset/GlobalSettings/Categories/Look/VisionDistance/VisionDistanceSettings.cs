using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class VisionDistanceSettings : SAINSettingsBase<VisionDistanceSettings>, ISAINSettings
    {
        [Name("Global Vision Distance Multiplier")]
        [Description(
            "Multiplies whatever a bot's visible distance is set to. " +
            "Higher is further visible distance, so 1.5 would result in bots seeing 1.5 times further. " +
            "Or if their visible distance is set to 100 meters, they will see at 150 meters instead.")]
        [MinMax(0.1f, 5f, 100f)]
        public float GlobalVisionDistanceMultiplier = 1;

        [Name("Movement Vision Distance Modifier")]
        [Description(
            "Bots will see moving players this much further. " +
            "Higher is further distance, so 1.75 would result in bots seeing enemies 1.75x further at max player speed. " +
            "Scales with player velocity.")]
        [MinMax(1f, 3f, 100f)]
        public float MovementDistanceModifier = 1.5f;
    }
}