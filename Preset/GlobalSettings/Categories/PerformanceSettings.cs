using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class PerformanceSettings
    {
        [Advanced]
        [MinMax(1f, 20f, 1f)]
        public float MinJobSize = 2;

        [Advanced]
        [MinMax(0.01f, 0.1f, 1000f)]
        public float SpherecastRadius = 0.025f;

        [Advanced]
        [MinMax(1f, 20f, 1f)]
        public float MaxBotsToCheckVisionPerFrame = 3;

        //[Advanced]
        //[MinMax(1f, 20f, 1f)]
        //public float MaxHumansToCheckVisionPerFrame = 1f;

        //[Advanced]
        //[MinMax(1f, 50f, 1f)]
        //public float MaxEnemiesToCheckPerBot = 10f;
    }
}