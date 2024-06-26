using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class PerformanceSettings
    {
        [JsonIgnore]
        [Hidden]
        public static readonly PerformanceSettings Defaults = new PerformanceSettings();

        [Name("Performance Mode")]
        [Description("Limits the cover finder to maximize performance. If your PC is CPU limited, this might let you regain some frames lost while using SAIN. Can cause bots to take too long to find cover to go to.")]
        [Default(false)]
        public bool PerformanceMode = false;

        [JsonIgnore]
        [Advanced]
        [MinMax(1f, 20f, 1f)]
        public float MinJobSize = 2;

        [Advanced]
        [MinMax(0.01f, 0.1f, 1000f)]
        public float SpherecastRadius = 0.025f;

        [JsonIgnore]
        [Advanced]
        [MinMax(1f, 20f, 1f)]
        public float MaxBotsToCheckVisionPerFrame = 5;

        //[Advanced]
        //[MinMax(1f, 20f, 1f)]
        //public float MaxHumansToCheckVisionPerFrame = 1f;

        //[Advanced]
        //[MinMax(1f, 50f, 1f)]
        //public float MaxEnemiesToCheckPerBot = 10f;
    }
}