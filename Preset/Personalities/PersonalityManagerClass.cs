using EFT;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;
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

            InitDefaults();

            if (Preset.Info.IsCustom == true)
            {
                bool hadToFix = false;
                foreach (var item in Personalities)
                {
                    if (item.Value.Variables.AllowedTypes.Count == 0)
                    {
                        hadToFix = true;
                        if (item.Key == EPersonality.Chad || item.Key == EPersonality.GigaChad)
                        {
                            AddPMCTypes(item.Value.Variables.AllowedTypes);
                        }
                        else
                        {
                            AddAllBotTypes(item.Value.Variables.AllowedTypes);
                        }
                    }
                }
                if (hadToFix)
                {
                    string message = "The Preset you are using is out of date, and required manual fixing. Its recommended you create a new one.";
                    NotificationManagerClass.DisplayMessageNotification(message);
                    Logger.LogWarning(message);
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
            InitDefaults();
        }

        public void ResetToDefault(EPersonality personality)
        {
            Personalities.Remove(personality);
            InitDefaults();
        }

        private void InitDefaults()
        {
            var pers = EPersonality.GigaChad;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A true alpha threat. Hyper Aggressive and typically wearing high tier equipment.";
                var settings = new PersonalitySettingsClass(pers, name, description)
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
                Personalities.Add(pers, settings);
                if (Preset.Info.IsCustom == true)
                {
                    SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
                }
            }

            pers = EPersonality.Wreckless;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "Rush B Cyka Blyat. Who care if I die? Gotta get the clip";
                var settings = new PersonalitySettingsClass(pers, name, description)
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
                Personalities.Add(pers, settings);
                if (Preset.Info.IsCustom == true)
                {
                    SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
                }
            }

            pers = EPersonality.SnappingTurtle;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A player who finds the balance between rat and chad, yin and yang. Will rat you out but can spring out at any moment.";
                var settings = new PersonalitySettingsClass(pers, name, description)
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
                Personalities.Add(pers, settings);
                if (Preset.Info.IsCustom == true)
                {
                    SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
                }
            }

            pers = EPersonality.Chad;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "An aggressive player. Typically wearing high tier equipment, and is more aggressive than usual.";
                var settings = new PersonalitySettingsClass(pers, name, description)
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
                Personalities.Add(pers, settings);
                if (Preset.Info.IsCustom == true)
                {
                    SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
                }
            }

            pers = EPersonality.Rat;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "Scum of Tarkov. Rarely Seeks out enemies, and will hide and ambush.";
                var settings = new PersonalitySettingsClass(pers, name, description)
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

                Personalities.Add(pers, settings);
                if (Preset.Info.IsCustom == true)
                {
                    SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
                }
            }

            pers = EPersonality.Timmy;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A New Player, terrified of everything.";

                var settings = new PersonalitySettingsClass(pers, name, description)
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

                Personalities.Add(pers, settings);
                if (Preset.Info.IsCustom == true)
                {
                    SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
                }
            }

            pers = EPersonality.Coward;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A player who is more passive and afraid than usual.";
                var settings = new PersonalitySettingsClass(pers, name, description)
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

                Personalities.Add(pers, settings);
                if (Preset.Info.IsCustom == true)
                {
                    SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
                }
            }

            pers = EPersonality.Normal;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "An Average Tarkov Enjoyer";
                var settings = new PersonalitySettingsClass(pers, name, description)
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
                Personalities.Add(pers, settings);
                if (Preset.Info.IsCustom == true)
                {
                    SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
                }
            }
        }

        private static void AddAllBotTypes(List<string> allowedTypes)
        {
            allowedTypes.Clear();
            allowedTypes.AddRange(BotTypeDefinitions.BotTypesNames);
        }

        private static void AddPMCTypes(List<string> allowedTypes)
        {
            allowedTypes.Add(BotTypeDefinitions.BotTypes[WildSpawn.Usec].Name);
            allowedTypes.Add(BotTypeDefinitions.BotTypes[WildSpawn.Bear].Name);
        }

        public Dictionary<EPersonality, PersonalitySettingsClass> Personalities = new Dictionary<EPersonality, PersonalitySettingsClass>();
    }
}