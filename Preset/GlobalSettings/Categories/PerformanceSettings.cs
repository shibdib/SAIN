using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class PerformanceSettings : SAINSettingsBase<PerformanceSettings>, ISAINSettings
    {
        [Name("Performance Mode")]
        [Description("Limits the cover finder to maximize performance. Reduces frequency on some raycasts. " +
            "If your PC is CPU limited, this might let you regain some frames lost while using SAIN. Can cause bots to take too long to find cover to go to.")]
        public bool PerformanceMode = false;

        [JsonIgnore]
        [Hidden]
        [Advanced]
        [MinMax(1f, 20f, 1f)]
        public float MinJobSize = 2;

        [JsonIgnore]
        [Advanced]
        [MinMax(0.01f, 0.1f, 1000f)]
        [Hidden]
        public float SpherecastRadius = 0.025f;

        [Advanced]
        [MinMax(2f, 20f, 1f)]
        public float MaxBotsToCheckVisionPerFrame = 5;
    }
}