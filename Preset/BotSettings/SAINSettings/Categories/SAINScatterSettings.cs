using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINScatterSettings : SAINSettingsBase<SAINScatterSettings>, ISAINSettings
    {
        [Name("EFT Scatter Multiplier")]
        [Description("Higher = more scattering. Modifies EFT's default scatter feature. 1.5 = 1.5x more scatter")]
        [MinMax(0.1f, 10f, 100f)]
        public float ScatterMultiplier = 1f;

        [Name("Arm Injury Scatter Multiplier")]
        [Description("Increase scatter when a bots arms are injured.")]
        [MinMax(1f, 5f, 100f)]
        [Advanced]
        public float HandDamageScatteringMinMax = 1.5f;

        [Name("Arm Injury Aim Speed Multiplier")]
        [Description("Increase scatter when a bots arms are injured.")]
        [MinMax(1f, 5f, 100f)]
        [Advanced]
        public float HandDamageAccuracySpeed = 1.5f;

        [JsonIgnore]
        [Hidden]
        public float DIST_NOT_TO_SHOOT = 0f;

        [JsonIgnore]
        [Hidden]
        public float FromShot = 0.002f;
    }
}