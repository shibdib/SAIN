using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class LightNVGSettings : SAINSettingsBase<LightNVGSettings>, ISAINSettings
    {
        [Advanced]
        [MinMax(0.01f, 0.99f, 100f)]
        public float LightOnRatio = 0.6f;

        [Advanced]
        [MinMax(0.01f, 0.99f, 100f)]
        public float LightOffRatio = 0.8f;

        [Advanced]
        [MinMax(0.01f, 0.99f, 100f)]
        public float NightVisionOnRatio = 0.6f;

        [Advanced]
        [MinMax(0.01f, 0.99f, 100f)]
        public float NightVisionOffRatio = 0.8f;
    }
}