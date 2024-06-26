using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.Personalities
{
    public class PersonalityGeneralSettings : SAINSettingsBase<PersonalityGeneralSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        [Name("Aggression Multiplier")]
        [Description("Linearly increases or decreases search time and hold ground time.")]
        [MinMax(0.01f, 5f, 100)]
        public float AggressionMultiplier = 1f;

        [Name("Kick Open All Possible Doors")]
        [Description("If this personality has an enemy, always kick open doors if they can.")]
        public bool KickOpenAllDoors = false;

        [Name("Hold Ground Base Time")]
        [Description("The base time, before modifiers, that a personality will stand their ground and shoot or return fire on an enemy if caught out of cover.")]
        [Advanced]
        [MinMax(0, 3f, 10)]
        public float HoldGroundBaseTime = 1f;

        [Advanced]
        [MinMax(0.1f, 2f, 10)]
        public float HoldGroundMinRandom = 0.66f;

        [Advanced]
        [MinMax(0.1f, 2f, 10)]
        public float HoldGroundMaxRandom = 1.5f;
    }
}