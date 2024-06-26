using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class PersonalitySettings : SAINSettingsBase<PersonalitySettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        [Name("Force Single Personality For All Bots")]
        [Description("All Spawned SAIN bots will be assigned the selected Personality, if any are set to true, no matter what.")]
        [DefaultDictionary(nameof(ForcePersonalityDefaults))]
        public Dictionary<EPersonality, bool> ForcePersonality = new Dictionary<EPersonality, bool>()
        {
            { EPersonality.Wreckless, false},
            { EPersonality.GigaChad, false },
            { EPersonality.Chad, false },
            { EPersonality.SnappingTurtle, false},
            { EPersonality.Rat, false },
            { EPersonality.Coward, false },
            { EPersonality.Timmy, false},
            { EPersonality.Normal, false},
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<EPersonality, bool> ForcePersonalityDefaults = new Dictionary<EPersonality, bool>()
        {
            { EPersonality.Wreckless, false},
            { EPersonality.GigaChad, false },
            { EPersonality.Chad, false },
            { EPersonality.SnappingTurtle, false},
            { EPersonality.Rat, false },
            { EPersonality.Coward, false },
            { EPersonality.Timmy, false},
            { EPersonality.Normal, false},
        };

        public bool CheckForForceAllPers(out EPersonality personality)
        {
            foreach (var item in ForcePersonality)
            {
                if (item.Value == true)
                {
                    personality = item.Key;
                    return true;
                }
            }
            personality = EPersonality.Normal;
            return false;
        }
    }
}