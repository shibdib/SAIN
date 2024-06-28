using Newtonsoft.Json;

namespace SAIN.Preset.Personalities
{
    public class PersonalitySettingsClass : SettingsGroupBase<PersonalitySettingsClass>
    {
        [JsonConstructor]
        public PersonalitySettingsClass()
        {
        }

        public PersonalitySettingsClass(EPersonality personality)
        {
            Name = personality.ToString();
            Description = PersonalityDescriptionsClass.PersonalityDescriptions[personality];
        }

        public string Name;
        public string Description;
        public PersonalityAssignmentSettings Assignment = new PersonalityAssignmentSettings();
        public PersonalityBehaviorSettings Behavior = new PersonalityBehaviorSettings();
        public PersonalityStatModifierSettings StatModifiers = new PersonalityStatModifierSettings();

        public override void Init()
        {
            InitList();
            CreateDefaults();
            Behavior.Init();
            Update();
        }

        public override void InitList()
        {
            SettingsList.Clear();
            SettingsList.Add(Assignment);
            SettingsList.Add(StatModifiers);
        }
    }
}