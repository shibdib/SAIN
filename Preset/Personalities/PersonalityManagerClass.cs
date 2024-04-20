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
            foreach (var item in EnumValues.Personalities)
            {
                if (Preset.Import(out PersonalitySettingsClass personality, item.ToString(), nameof(Personalities)))
                {
                    Personalities.Add(item, personality);
                }
            }

            InitDefaults();

            bool hadToFix = false;
            foreach (var item in Personalities)
            {
                if (item.Value.Variables.AllowedTypes.Count == 0)
                {
                    hadToFix = true;
                    if (item.Key == IPersonality.Chad || item.Key == IPersonality.GigaChad)
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

        public void ExportPersonalities()
        {
            foreach (var pers in Personalities)
            {
                if (pers.Value != null && Preset?.Export(pers.Value, pers.Key.ToString(), nameof(Personalities)) == true)
                {
                    continue;
                }
                else if (pers.Value == null)
                {
                    Logger.LogError("Personality Settings Are Null");
                }
                else if (Preset == null)
                {
                    Logger.LogError("Preset Is Null");
                }
                else
                {
                    Logger.LogError($"Failed to Export {pers.Key}");
                }
            }
        }

        public void ResetToDefaults()
        {
            Personalities.Clear();
            InitDefaults();
        }

        private void InitDefaults()
        {
            var pers = IPersonality.GigaChad;
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
                        CanRespondToVoice = true,
                        TauntFrequency = 8,
                        TauntMaxDistance = 50f,
                        ConstantTaunt = true,

                        HoldGroundBaseTime = 1.25f,
                        HoldGroundMaxRandom = 1.5f,
                        HoldGroundMinRandom = 0.65f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 8f,
                        SprintWhileSearch = true,
                        FrequentSprintWhileSearch = true,

                        CanJumpCorners = true,
                        CanRushEnemyReloadHeal = true,
                        CanFakeDeathRare = true,

                        AggressionMultiplier = 2f,
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
                Preset.Export(settings, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Chad;
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
                        CanRespondToVoice = true,
                        TauntFrequency = 15,
                        TauntMaxDistance = 30f,
                        FrequentTaunt = false,

                        HoldGroundBaseTime = 1f,
                        HoldGroundMaxRandom = 1.5f,
                        HoldGroundMinRandom = 0.65f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 30f,
                        SprintWhileSearch = true,

                        CanJumpCorners = false,
                        CanRushEnemyReloadHeal = true,
                        AggressionMultiplier = 1.5f,

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

                AddPMCTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                Preset.Export(settings, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Rat;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "Scum of Tarkov. Rarely Seeks out enemies, and will hide and ambush.";
                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 50,
                        RandomlyAssignedChance = 25,
                        HoldGroundBaseTime = 0.75f,
                        WillSearchForEnemy = false,
                        WillSearchFromAudio = false,
                        SearchBaseTime = 180f,
                        PowerLevelMax = 50f,
                        AggressionMultiplier = 0.65f,

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

                AddAllBotTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                Preset.Export(settings, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Timmy;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A New Player, terrified of everything.";

                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        RandomlyAssignedChance = 25,
                        PowerLevelMax = 40f,
                        MaxLevel = 10,
                        HoldGroundBaseTime = 0.5f,
                        WillSearchForEnemy = true,
                        WillSearchFromAudio = false,
                        SearchBaseTime = 120f,
                        AggressionMultiplier = 0.75f,
                        ShiftCoverTimeMultiplier = 0.66f,
                        CanBegForLife = true,

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

                AddPMCTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                Preset.Export(settings, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Coward;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A player who is more passive and afraid than usual.";
                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        RandomlyAssignedChance = 25,
                        HoldGroundBaseTime = 0.5f,
                        WillSearchForEnemy = false,
                        WillSearchFromAudio = false,
                        SearchBaseTime = 90f,
                        AggressionMultiplier = 0.45f,
                        CanShiftCoverPosition = false,
                        CanBegForLife = true,

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
                AddAllBotTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                Preset.Export(settings, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Normal;
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
                        CanRespondToVoice = true,

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
                Preset.Export(settings, pers.ToString(), nameof(Personalities));
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

        public Dictionary<IPersonality, PersonalitySettingsClass> Personalities = new Dictionary<IPersonality, PersonalitySettingsClass>();
    }
}