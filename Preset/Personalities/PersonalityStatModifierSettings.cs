using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.Personalities
{
    public class PersonalityStatModifierSettings : SAINSettingsBase<PersonalityStatModifierSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

    }
}