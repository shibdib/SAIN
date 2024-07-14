using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.Personalities
{
    public class PersonalityStatModifierSettings : SAINSettingsBase<PersonalityStatModifierSettings>, ISAINSettings
    {
        [Description("Lower is less scatter, and more accurate bots")]
        [MinMax(0.01f, 3f, 100f)]
        public float ScatterMultiplier = 1f;
        
        [Description("Lower is faster vision speed. Multiplies the time before an enemy is targetted. so if it usually took 5 seconds to spot an enemy, with a setting here of 0.1, it would take 0.3 seconds.")]
        [MinMax(0.01f, 3f, 100f)]
        public float VisionSpeedMultiplier = 1f;
    }
}