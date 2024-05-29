using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings.Categories
{
    public class MindSettings
    {
        [Default(1f)]
        [MinMax(0.1f, 5f, 100f)]
        public float GlobalAggression = 1f;

        [Description("The maximum distance between the bullet, and a bot's head to be considered Suppressing fire.")]
        [Default(10f)]
        [MinMax(1f, 30f, 10f)]
        [Advanced]
        public float MaxSuppressionDistance = 10f;

        [Description("The maximum distance between the bullet, and a bot's head to be considered under active enemy fire.")]
        [Default(10f)]
        [MinMax(0.1f, 20f, 10f)]
        [Advanced]
        public float MaxUnderFireDistance = 2f;
    }
}