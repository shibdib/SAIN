namespace SAIN.Preset.Personalities
{
    public class PersonalityBehaviorSettings : SettingsGroupBase<PersonalityBehaviorSettings>
    {
        public PersonalityGeneralSettings General = new PersonalityGeneralSettings();
        public PersonalitySearchSettings Search = new PersonalitySearchSettings();
        public PersonalityRushSettings Rush = new PersonalityRushSettings();
        public PersonalityCoverSettings Cover = new PersonalityCoverSettings();
        public PersonalityTalkSettings Talk = new PersonalityTalkSettings();

        public override void InitList()
        {
            SettingsList.Clear();
            SettingsList.Add(Cover);
            SettingsList.Add(General);
            SettingsList.Add(Rush);
            SettingsList.Add(Search);
            SettingsList.Add(Talk);
        }
    }
}