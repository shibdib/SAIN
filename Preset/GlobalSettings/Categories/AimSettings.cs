using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class AimSettings
    {
        [Name("Headshot Protection")]
        [Description("Force Bots to aim for center of mass.")]
        [Default(true)]
        public bool HeadShotProtection = true;

        [Name("Center Mass Point")]
        [Description("The place where bots will target if headshot protection is on. A value of 0 will be directly on your head, a value of 1 will be directly at the floor below you at your feet.")]
        [Advanced]
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
    }
}