using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class VisionSpeedSettings : SAINSettingsBase<VisionSpeedSettings>, ISAINSettings
    {
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

        public ElevationVisionSettings Elevation = new ElevationVisionSettings();
    }
}