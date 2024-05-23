using EFT;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static SAIN.Helpers.EnumValues;

namespace SAIN.Preset.Personalities
{
    public class PersonalityManagerClass : BasePreset
    {
        public PersonalityManagerClass(SAINPresetClass preset) : base(preset)
        {
            ImportPersonalities();
        }

        public bool VerificationPassed = true;

        private void ImportPersonalities()
        {
            import();
            PersonalityDefaultsClass.InitDefaults(Personalities);
            checkFix();
        }

        private void import()
        {
            if (Preset.Info.IsCustom == true)
            {
                foreach (var item in EnumValues.Personalities)
                {
                    if (SAINPresetClass.Import(out PersonalitySettingsClass personality, Preset.Info.Name, item.ToString(), nameof(Personalities)))
                    {
                        Personalities.Add(item, personality);
                    }
                }
            }
        }

        private void checkFix()
        {
            if (Preset.Info.IsCustom == true)
            {
                bool hadToFix = false;
                foreach (var item in Personalities)
                {
                    if (item.Value.Variables.AllowedTypes.Count == 0)
                    {
                        hadToFix = true;
                        break;
                    }
                }
                if (hadToFix)
                {
                    string message = "The Preset you are using is out of date, and required manual fixing. Its recommended you create a new one.";
                    Logger.LogAndNotifyError(message);
                    Personalities.Clear();
                    PersonalityDefaultsClass.InitDefaults(Personalities);
                }
            }
        }

        public void ResetAllToDefaults()
        {
            Personalities.Remove(EPersonality.Wreckless);
            Personalities.Remove(EPersonality.SnappingTurtle);
            Personalities.Remove(EPersonality.GigaChad);
            Personalities.Remove(EPersonality.Chad);
            Personalities.Remove(EPersonality.Rat);
            Personalities.Remove(EPersonality.Coward);
            Personalities.Remove(EPersonality.Timmy);
            Personalities.Remove(EPersonality.Normal);
            PersonalityDefaultsClass.InitDefaults(Personalities);
        }

        public void ResetToDefault(EPersonality personality)
        {
            Personalities.Remove(personality);
            PersonalityDefaultsClass.InitDefaults(Personalities);
        }

        public PersonalityDictionary Personalities = new PersonalityDictionary();

        public class PersonalityDictionary : Dictionary<EPersonality, PersonalitySettingsClass>{ }

        private static class PersonalityDefaultsClass
        {

            public static void InitDefaults(PersonalityDictionary Personalities)
            {
                initGigaChad(Personalities);
                initWreckless(Personalities);
                initSnappingTurtle(Personalities);
                initChad(Personalities);
                initRat(Personalities);
                initTimmy(Personalities);
                initCoward(Personalities);
                initNormal(Personalities);
            }

            private static void initGigaChad(PersonalityDictionary Personalities)
            {
                EPersonality GigaChad = EPersonality.GigaChad;
                if (!Personalities.ContainsKey(GigaChad))
                {
                    string name = GigaChad.ToString();
                    string description = "A true alpha threat. Hyper Aggressive and typically wearing high tier equipment.";
                    var settings = new PersonalitySettingsClass(GigaChad, name, description)
                    {
                        Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 60,
                        RandomlyAssignedChance = 3,
                        PowerLevelMin = 250f,

                        CanTaunt = true,
                        CanRespondToEnemyVoice = true,
                        TauntFrequency = 6,
                        TauntMaxDistance = 75f,
                        ConstantTaunt = true,
                        FrequentTaunt = true,

                        HoldGroundBaseTime = 1.25f,
                        HoldGroundMaxRandom = 1.5f,
                        HoldGroundMinRandom = 0.65f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 6f,
                        SprintWhileSearch = true,
                        FrequentSprintWhileSearch = true,
                        SearchWaitMultiplier = 3f,

                        CanJumpCorners = true,
                        JumpCornerChance = 40f,
                        CanBunnyHop = true,
                        BunnyHopChance = 5f,

                        CanRushEnemyReloadHeal = true,
                        CanFakeDeathRare = true,

                        AggressionMultiplier = 1f,
                        ShiftCoverTimeMultiplier = 0.5f,

                        SearchHasEnemySpeed = 1f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 1f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                    };

                    AddPMCTypes(settings.Variables.AllowedTypes);
                    Personalities.Add(GigaChad, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, GigaChad.ToString(), nameof(Personalities));
                    }
                }
            }
            private static void initWreckless(PersonalityDictionary Personalities)
            {
                EPersonality Wreckless = EPersonality.Wreckless;
                if (!Personalities.ContainsKey(Wreckless))
                {
                    string name = Wreckless.ToString();
                    string description = "Rush B Cyka Blyat. Who care if I die? Gotta get the clip";
                    var settings = new PersonalitySettingsClass(Wreckless, name, description)
                    {
                        Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 3,
                        RandomlyAssignedChance = 1,
                        PowerLevelMin = 150f,

                        CanTaunt = true,
                        CanRespondToEnemyVoice = true,
                        TauntFrequency = 4,
                        TauntMaxDistance = 90f,
                        ConstantTaunt = true,

                        HoldGroundBaseTime = 2.5f,
                        HoldGroundMaxRandom = 1.5f,
                        HoldGroundMinRandom = 0.65f,
                        SearchWaitMultiplier = 4f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 0.1f,
                        SprintWhileSearch = true,
                        FrequentSprintWhileSearch = true,

                        CanJumpCorners = true,
                        JumpCornerChance = 75f,
                        CanBunnyHop = true,
                        BunnyHopChance = 25f,
                        CanRushEnemyReloadHeal = true,
                        CanFakeDeathRare = true,

                        AggressionMultiplier = 1f,
                        ShiftCoverTimeMultiplier = 0.5f,

                        SearchHasEnemySpeed = 1f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 1f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                    };

                    AddAllBotTypes(settings.Variables.AllowedTypes);
                    Personalities.Add(Wreckless, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Wreckless.ToString(), nameof(Personalities));
                    }
                }
            }
            private static void initSnappingTurtle(PersonalityDictionary Personalities)
            {
                EPersonality SnappingTurtle = EPersonality.SnappingTurtle;
                if (!Personalities.ContainsKey(SnappingTurtle))
                {
                    string name = SnappingTurtle.ToString();
                    string description = "A player who finds the balance between rat and chad, yin and yang. Will rat you out but can spring out at any moment.";
                    var settings = new PersonalitySettingsClass(SnappingTurtle, name, description)
                    {
                        Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 30,
                        RandomlyAssignedChance = 1,
                        PowerLevelMin = 350f,

                        CanTaunt = true,
                        CanRespondToEnemyVoice = false,
                        TauntFrequency = 15,
                        TauntMaxDistance = 50f,
                        ConstantTaunt = false,
                        FrequentTaunt = false,

                        HoldGroundBaseTime = 1.5f,
                        HoldGroundMaxRandom = 1.2f,
                        HoldGroundMinRandom = 0.8f,
                        SearchWaitMultiplier = 0.8f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 90f,
                        SprintWhileSearch = false,
                        FrequentSprintWhileSearch = false,

                        CanJumpCorners = true,
                        CanRushEnemyReloadHeal = true,
                        CanFakeDeathRare = true,

                        AggressionMultiplier = 1f,
                        ShiftCoverTimeMultiplier = 0.5f,

                        SearchHasEnemySpeed = 0.7f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0.8f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                    };

                    AddPMCTypes(settings.Variables.AllowedTypes);
                    Personalities.Add(SnappingTurtle, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, SnappingTurtle.ToString(), nameof(Personalities));
                    }
                }
            }
            private static void initChad(PersonalityDictionary Personalities)
            {
                EPersonality Chad = EPersonality.Chad;
                if (!Personalities.ContainsKey(Chad))
                {
                    string name = Chad.ToString();
                    string description = "An aggressive player. Typically wearing high tier equipment, and is more aggressive than usual.";
                    var settings = new PersonalitySettingsClass(Chad, name, description)
                    {
                        Variables =
                    {
                        Enabled = true,

                        RandomChanceIfMeetRequirements = 60,
                        RandomlyAssignedChance = 5,
                        PowerLevelMin = 200f,

                        CanTaunt = true,
                        CanRespondToEnemyVoice = true,
                        TauntFrequency = 10,
                        TauntMaxDistance = 60f,
                        FrequentTaunt = true,

                        HoldGroundBaseTime = 1f,
                        HoldGroundMaxRandom = 1.5f,
                        HoldGroundMinRandom = 0.65f,
                        SearchWaitMultiplier = 1.5f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 16f,
                        SprintWhileSearch = true,

                        CanJumpCorners = true,
                        JumpCornerChance = 25f,
                        CanRushEnemyReloadHeal = true,
                        AggressionMultiplier = 1f,

                        SearchHasEnemySpeed = 1f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0.7f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 0.7f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                    };

                    AddAllBotTypes(settings.Variables.AllowedTypes);
                    Personalities.Add(Chad, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Chad.ToString(), nameof(Personalities));
                    }
                }
            }
            private static void initRat(PersonalityDictionary Personalities)
            {
                EPersonality Rat = EPersonality.Rat;
                if (!Personalities.ContainsKey(Rat))
                {
                    string name = Rat.ToString();
                    string description = "Scum of Tarkov. Rarely Seeks out enemies, and will hide and ambush.";
                    var settings = new PersonalitySettingsClass(Rat, name, description)
                    {
                        Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 25,
                        RandomlyAssignedChance = 15,
                        HoldGroundBaseTime = 0.75f,
                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 240f,
                        SearchWaitMultiplier = 0.8f,
                        PowerLevelMax = 150f,
                        AggressionMultiplier = 1f,
                        CanRespondToEnemyVoice = false,
                        WillChaseDistantGunshots = false,

                        Sneaky = true,
                        SneakyPose = 0f,
                        SneakySpeed = 0f,

                        SearchHasEnemySpeed = 0f,
                        SearchHasEnemyPose = 0f,
                        SearchNoEnemySpeed = 0f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 0.5f,
                        MoveToCoverHasEnemyPose = 0.5f,
                        MoveToCoverNoEnemySpeed = 0.3f,
                        MoveToCoverNoEnemyPose = 0.7f,

                        CanShiftCoverPosition = false
                    }
                    };

                    var allowedTypes = settings.Variables.AllowedTypes;
                    AddAllBotTypes(allowedTypes);

                    allowedTypes.Remove("Raider");
                    allowedTypes.Remove("Rogue");
                    allowedTypes.Remove("Bloodhound");

                    Personalities.Add(Rat, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Rat.ToString(), nameof(Personalities));
                    }
                }
            }
            private static void initTimmy(PersonalityDictionary Personalities)
            {
                EPersonality Timmy = EPersonality.Timmy;
                if (!Personalities.ContainsKey(Timmy))
                {
                    string name = Timmy.ToString();
                    string description = "A New Player, terrified of everything.";

                    var settings = new PersonalitySettingsClass(Timmy, name, description)
                    {
                        Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 50f,
                        RandomlyAssignedChance = 5,
                        PowerLevelMax = 150f,
                        MaxLevel = 10,
                        HoldGroundBaseTime = 0.5f,
                        WillSearchForEnemy = true,
                        WillSearchFromAudio = false,
                        SearchBaseTime = 90f,
                        SearchWaitMultiplier = 0.75f,
                        AggressionMultiplier = 1f,
                        ShiftCoverTimeMultiplier = 0.66f,
                        CanBegForLife = true,
                        CanRespondToEnemyVoice = false,
                        WillChaseDistantGunshots = false,

                        SearchHasEnemySpeed = 0f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                    };

                    var allowedTypes = settings.Variables.AllowedTypes;
                    AddAllBotTypes(allowedTypes);

                    allowedTypes.Remove("Raider");
                    allowedTypes.Remove("Rogue");
                    allowedTypes.Remove("Bloodhound");

                    Personalities.Add(Timmy, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Timmy.ToString(), nameof(Personalities));
                    }
                }
            }
            private static void initCoward(PersonalityDictionary Personalities)
            {
                EPersonality Coward = EPersonality.Coward;
                if (!Personalities.ContainsKey(Coward))
                {
                    string name = Coward.ToString();
                    string description = "A player who is more passive and afraid than usual.";
                    var settings = new PersonalitySettingsClass(Coward, name, description)
                    {
                        Variables =
                    {
                        Enabled = true,
                        RandomlyAssignedChance = 5,
                        RandomChanceIfMeetRequirements = 20f,
                        PowerLevelMax = 200f,
                        HoldGroundBaseTime = 0.5f,
                        WillSearchForEnemy = false,
                        WillSearchFromAudio = false,
                        SearchBaseTime = 110f,
                        SearchWaitMultiplier = 0.4f,
                        AggressionMultiplier = 1f,
                        CanShiftCoverPosition = false,
                        CanBegForLife = true,
                        CanRespondToEnemyVoice = false,
                        WillChaseDistantGunshots = false,

                        SearchHasEnemySpeed = 0f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                    };

                    var allowedTypes = settings.Variables.AllowedTypes;
                    AddAllBotTypes(allowedTypes);

                    allowedTypes.Remove("Raider");
                    allowedTypes.Remove("Rogue");
                    allowedTypes.Remove("Bloodhound");

                    Personalities.Add(Coward, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Coward.ToString(), nameof(Personalities));
                    }
                }
            }
            private static void initNormal(PersonalityDictionary Personalities)
            {
                EPersonality Normal = EPersonality.Normal;
                if (!Personalities.ContainsKey(Normal))
                {
                    string name = Normal.ToString();
                    string description = "An Average Tarkov Enjoyer";
                    var settings = new PersonalitySettingsClass(Normal, name, description)
                    {
                        Variables =
                    {
                        Enabled = true,
                        HoldGroundBaseTime = 0.65f,
                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 60f,
                        CanRespondToEnemyVoice = true,
                        WillChaseDistantGunshots = false,

                        SearchHasEnemySpeed = 0.6f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0.33f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                    };

                    AddAllBotTypes(settings.Variables.AllowedTypes);
                    Personalities.Add(Normal, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Normal.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void AddPMCTypes(List<string> allowedTypes)
            {
                allowedTypes.Add(BotTypeDefinitions.BotTypes[WildSpawn.Usec].Name);
                allowedTypes.Add(BotTypeDefinitions.BotTypes[WildSpawn.Bear].Name);
            }

            private static void AddAllBotTypes(List<string> allowedTypes)
            {
                allowedTypes.Clear();
                allowedTypes.AddRange(BotTypeDefinitions.BotTypesNames);
            }

            private static SAINPresetClass Preset => SAINPlugin.LoadedPreset;

        }
    }
}