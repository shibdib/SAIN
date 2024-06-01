using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class AimSettings
    {
        [Name("Headshot Protection")]
        [Description("Force Bots to aim for center of mass.")]
        [Default(true)]
        public bool HeadShotProtection = true;

        [Name("Headshot Protection for PMCs")]
        [Description("Force PMCs to aim for center of mass.")]
        [Default(true)]
        public bool HeadShotProtectionPMC = true;

        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 1000f)]
        [Default(0.8f)]
        public float PoseLevelScatterMulti = 0.8f;

        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 1000f)]
        [Default(0.7f)]
        public float ProneScatterMulti = 0.7f;

        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 1000f)]
        [Default(0.75f)]
        public float PartVisScatterMulti = 0.75f;

        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(1f, 1.5f, 1000f)]
        [Default(1.2f)]
        public float OpticFarMulti = 1.2f;

        [Advanced]
        [MinMax(25f, 150f, 10f)]
        [Default(100f)]
        public float OpticFarDistance = 100f;

        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 1000f)]
        [Default(0.8f)]
        public float OpticCloseMulti = 0.8f;

        [Advanced]
        [MinMax(25f, 150f, 10f)]
        [Default(75f)]
        public float OpticCloseDistance = 75f;

        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 1000f)]
        [Default(0.85f)]
        public float RedDotFarMulti = 0.85f;

        [Advanced]
        [MinMax(25f, 150f, 10f)]
        [Default(100f)]
        public float RedDotFarDistance = 100f;

        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(1f, 1.5f, 1000f)]
        [Default(1.15f)]
        public float RedDotCloseMulti = 1.15f;

        [Advanced]
        [MinMax(25f, 150f, 10f)]
        [Default(75f)]
        public float RedDotCloseDistance = 65f;

        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 1000f)]
        [Default(0.75f)]
        public float IronSightFarMulti = 0.7f;

        [Advanced]
        [MinMax(25f, 200f, 10f)]
        [Default(75f)]
        public float IronSightScaleDistanceStart = 50f;

        [Advanced]
        [MinMax(25f, 200f, 10f)]
        [Default(125f)]
        public float IronSightScaleDistanceEnd = 100f;

        [Name("Center Mass Point")]
        [Description("The place where bots will target if headshot protection is on. A value of 0 will be directly on your head, a value of 1 will be directly at the floor below you at your feet.")]
        [Advanced]
        [Default(0.3125f)]
        [MinMax(0f, 1f, 1000f)]
        public float CenterMassVal = 0.3125f;

        [Name("Global Accuracy Spread Multiplier")]
        [Description("Higher = less accurate. Modifies all bots base accuracy and spread. 1.5 = 1.5x higher accuracy spread")]
        [Default(1f)]
        [MinMax(0.1f, 10f, 100f)]
        public float AccuracySpreadMultiGlobal = 1f;

        [Name("Global Faster CQB Reactions")]
        [Description("if this toggle is disabled, all bots will have Faster CQB Reactions turned OFF, so their individual settings will be ignored.")]
        [Default(true)]
        public bool FasterCQBReactionsGlobal = true;

        [Default(false)]
        public bool PMCSAimForHead = false;
    }
}