using Newtonsoft.Json;
using RootMotion.FinalIK;
using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.Info;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements.Experimental;

namespace SAIN.Preset.Personalities
{
    public class PersonalitySettingsClass : SettingsGroupBase<PersonalitySettingsClass>
    {
        [JsonConstructor]
        public PersonalitySettingsClass()
        { }

        public PersonalitySettingsClass(EPersonality personality, string description)
        {
            Name = personality.ToString();
            Description = description;
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
        }

        public override void InitList()
        {
            SettingsList.Clear();
            SettingsList.Add(Assignment);
            SettingsList.Add(StatModifiers);
        }
    }
}