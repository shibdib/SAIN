using Newtonsoft.Json;
using SAIN.SAINComponent.Classes.Info;

namespace SAIN.Preset.Personalities
{
    public class PersonalitySettingsClass
    {
        [JsonConstructor]
        public PersonalitySettingsClass()
        { }

        public PersonalitySettingsClass(EPersonality personality, string name, string description)
        {
            SAINPersonality = personality;
            Name = name;
            Description = description;
        }

        public EPersonality SAINPersonality;
        public string Name;
        public string Description;
        public bool CanBePersonality(SAINBotInfoClass infoClass)
        {
            return Assignment.CanBePersonality(infoClass);
        }

        public PersonalityAssignmentSettings Assignment = new PersonalityAssignmentSettings();
        public PersonalityBehaviorSettings Behavior = new PersonalityBehaviorSettings();
        public PersonalityStatModifierSettings StatModifiers = new PersonalityStatModifierSettings();
    }
}