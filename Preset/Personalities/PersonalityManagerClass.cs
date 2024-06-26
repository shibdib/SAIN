using EFT;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.GlobalSettings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static EFT.SpeedTree.TreeWind;
using static SAIN.Helpers.EnumValues;

namespace SAIN.Preset.Personalities
{
    public class PersonalityManagerClass : BasePreset
    {
        public PersonalityManagerClass(SAINPresetClass preset) : base(preset)
        {
            ImportPersonalities();
        }

        public void Init()
        {
            foreach (var settings in PersonalityDictionary.Values)
            {
                settings.Init();
            }
        }

        public void UpdateDefaults(PersonalityManagerClass replacementClass = null)
        {
            foreach (var settings in PersonalityDictionary)
            {
                var replacementSettings = replacementClass?.PersonalityDictionary[settings.Key];
                settings.Value.UpdateDefaults(replacementSettings);
            }
        }

        public bool VerificationPassed = true;

        private void ImportPersonalities()
        {
            import();
            PersonalityDefaultsClass.InitDefaults(PersonalityDictionary, Preset);
        }

        private void import()
        {
            if (Preset.Info.IsCustom == true)
            {
                foreach (var item in EnumValues.Personalities)
                {
                    if (SAINPresetClass.Import(out PersonalitySettingsClass personality, Preset.Info.Name, item.ToString(), nameof(Personalities)))
                    {
                        PersonalityDictionary.Add(item, personality);
                    }
                    else
                    {
                        //Logger.LogWarning($"Could not import {item.ToString()} for {Preset.Info.Name}");
                    }
                }
            }
        }

        public void ResetAllToDefaults()
        {
            PersonalityDictionary.Remove(EPersonality.Wreckless);
            PersonalityDictionary.Remove(EPersonality.SnappingTurtle);
            PersonalityDictionary.Remove(EPersonality.GigaChad);
            PersonalityDictionary.Remove(EPersonality.Chad);
            PersonalityDictionary.Remove(EPersonality.Rat);
            PersonalityDictionary.Remove(EPersonality.Coward);
            PersonalityDictionary.Remove(EPersonality.Timmy);
            PersonalityDictionary.Remove(EPersonality.Normal);
            PersonalityDefaultsClass.InitDefaults(PersonalityDictionary, Preset);
        }

        public void ResetToDefault(EPersonality personality)
        {
            PersonalityDictionary.Remove(personality);
            PersonalityDefaultsClass.InitDefaults(PersonalityDictionary, Preset);
        }

        public PersonalityDictionary PersonalityDictionary = new PersonalityDictionary();

        private static class PersonalityDefaultsClass
        {
            public static void InitDefaults(PersonalityDictionary Personalities, SAINPresetClass preset)
            {
                initWreckless(Personalities, preset);
                initSnappingTurtle(Personalities, preset);
                initGigaChad(Personalities, preset);
                initChad(Personalities, preset);
                initRat(Personalities, preset);
                initTimmy(Personalities, preset);
                initCoward(Personalities, preset);
                initNormal(Personalities, preset);
            }

            private static void initGigaChad(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality GigaChad = EPersonality.GigaChad;
                if (!Personalities.ContainsKey(GigaChad))
                {
                    var settings = new PersonalitySettingsClass
                    {
                        Name = GigaChad.ToString(),
                        Description = "A true alpha threat. Hyper Aggressive and typically wearing high tier equipment."
                    };

                    var assignment = settings.Assignment;
                    assignment.Enabled = true;
                    assignment.RandomlyAssignedChance = 3;
                    assignment.CanBeRandomlyAssigned = true;
                    assignment.MaxChanceIfMeetRequirements = 80;
                    assignment.MinLevel = 0;
                    assignment.MaxLevel = 100;
                    assignment.PowerLevelMin = 250;
                    assignment.PowerLevelMax = 1000;
                    assignment.PowerLevelScaleStart = 250;
                    assignment.PowerLevelScaleEnd = 500;

                    var behavior = settings.Behavior;

                    behavior.General.KickOpenAllDoors = true;
                    behavior.General.AggressionMultiplier = 1;
                    behavior.General.HoldGroundBaseTime = 1.25f;
                    behavior.General.HoldGroundMaxRandom = 1.5f;
                    behavior.General.HoldGroundMinRandom = 0.65f;

                    behavior.Cover.CanShiftCoverPosition = true;
                    behavior.Cover.ShiftCoverTimeMultiplier = 0.5f;
                    behavior.Cover.MoveToCoverHasEnemySpeed = 1f;
                    behavior.Cover.MoveToCoverHasEnemyPose = 1f;
                    behavior.Cover.MoveToCoverNoEnemySpeed = 1f;
                    behavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    behavior.Talk.CanTaunt = true;
                    behavior.Talk.CanRespondToEnemyVoice = true;
                    behavior.Talk.TauntFrequency = 8;
                    behavior.Talk.TauntChance = 45;
                    behavior.Talk.TauntMaxDistance = 65f;
                    behavior.Talk.ConstantTaunt = true;
                    behavior.Talk.FrequentTaunt = true;
                    behavior.Talk.CanFakeDeathRare = true;
                    behavior.Talk.FakeDeathChance = 3;

                    behavior.Search.WillSearchForEnemy = true;
                    behavior.Search.WillSearchFromAudio = true;
                    behavior.Search.WillChaseDistantGunshots = true;
                    behavior.Search.SearchBaseTime = 6;
                    behavior.Search.SprintWhileSearchChance = 60;
                    behavior.Search.SearchHasEnemySpeed = 1f;
                    behavior.Search.SearchHasEnemyPose = 1f;
                    behavior.Search.SearchNoEnemySpeed = 1f;
                    behavior.Search.SearchNoEnemyPose = 1f;
                    behavior.Search.SearchWaitMultiplier = 3f;

                    behavior.Rush.CanRushEnemyReloadHeal = true;
                    behavior.Rush.CanJumpCorners = true;
                    behavior.Rush.JumpCornerChance = 40f;
                    behavior.Rush.CanBunnyHop = true;
                    behavior.Rush.BunnyHopChance = 5;

                    AddPMCTypes(settings.Assignment.AllowedTypes);
                    Personalities.Add(GigaChad, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, GigaChad.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void initWreckless(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality Wreckless = EPersonality.Wreckless;
                if (!Personalities.ContainsKey(Wreckless))
                {
                    var settings = new PersonalitySettingsClass
                    {
                        Name = Wreckless.ToString(),
                        Description = "This personality tends to sprint at their enemies, and will very frequently scream at everyone - Usually both at the same time. More Aggressive than Gigachads."
                    };

                    var assignment = settings.Assignment;
                    assignment.Enabled = true;
                    assignment.RandomlyAssignedChance = 1;
                    assignment.CanBeRandomlyAssigned = true;
                    assignment.MaxChanceIfMeetRequirements = 5;
                    assignment.MinLevel = 0;
                    assignment.MaxLevel = 100;
                    assignment.PowerLevelMin = 250;
                    assignment.PowerLevelMax = 1000;
                    assignment.PowerLevelScaleStart = 250;
                    assignment.PowerLevelScaleEnd = 500;

                    var behavior = settings.Behavior;

                    behavior.General.KickOpenAllDoors = true;
                    behavior.General.AggressionMultiplier = 1;
                    behavior.General.HoldGroundBaseTime = 2f;
                    behavior.General.HoldGroundMaxRandom = 2.5f;
                    behavior.General.HoldGroundMinRandom = 0.75f;

                    behavior.Cover.CanShiftCoverPosition = true;
                    behavior.Cover.ShiftCoverTimeMultiplier = 0.5f;
                    behavior.Cover.MoveToCoverHasEnemySpeed = 1f;
                    behavior.Cover.MoveToCoverHasEnemyPose = 1f;
                    behavior.Cover.MoveToCoverNoEnemySpeed = 1f;
                    behavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    behavior.Talk.CanTaunt = true;
                    behavior.Talk.CanRespondToEnemyVoice = true;
                    behavior.Talk.TauntFrequency = 4;
                    behavior.Talk.TauntChance = 33;
                    behavior.Talk.TauntMaxDistance = 75f;
                    behavior.Talk.ConstantTaunt = true;
                    behavior.Talk.FrequentTaunt = true;
                    behavior.Talk.CanFakeDeathRare = true;
                    behavior.Talk.FakeDeathChance = 6;

                    behavior.Search.WillSearchForEnemy = true;
                    behavior.Search.WillSearchFromAudio = true;
                    behavior.Search.WillChaseDistantGunshots = true;
                    behavior.Search.SearchBaseTime = 0.1f;
                    behavior.Search.SprintWhileSearchChance = 85;
                    behavior.Search.SearchHasEnemySpeed = 1f;
                    behavior.Search.SearchHasEnemyPose = 1f;
                    behavior.Search.SearchNoEnemySpeed = 1f;
                    behavior.Search.SearchNoEnemyPose = 1f;
                    behavior.Search.SearchWaitMultiplier = 1f;

                    behavior.Rush.CanRushEnemyReloadHeal = true;
                    behavior.Rush.CanJumpCorners = true;
                    behavior.Rush.JumpCornerChance = 60f;
                    behavior.Rush.CanBunnyHop = true;
                    behavior.Rush.BunnyHopChance = 10;

                    AddAllBotTypes(settings.Assignment.AllowedTypes);
                    Personalities.Add(Wreckless, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Wreckless.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void initSnappingTurtle(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality SnappingTurtle = EPersonality.SnappingTurtle;
                if (!Personalities.ContainsKey(SnappingTurtle))
                {
                    var settings = new PersonalitySettingsClass
                    {
                        Name = SnappingTurtle.ToString(),
                        Description = "A player who finds the balance between rat and chad, yin and yang. Will rat you out but can spring out at any moment."
                    };

                    var turtleAssignment = settings.Assignment;
                    turtleAssignment.Enabled = true;
                    turtleAssignment.RandomlyAssignedChance = 1;
                    turtleAssignment.CanBeRandomlyAssigned = true;
                    turtleAssignment.MaxChanceIfMeetRequirements = 30;
                    turtleAssignment.MinLevel = 15;
                    turtleAssignment.MaxLevel = 100;
                    turtleAssignment.PowerLevelMin = 150;
                    turtleAssignment.PowerLevelMax = 1000;
                    turtleAssignment.PowerLevelScaleStart = 150;
                    turtleAssignment.PowerLevelScaleEnd = 500;

                    var turtleBehavior = settings.Behavior;

                    turtleBehavior.General.AggressionMultiplier = 1;
                    turtleBehavior.General.HoldGroundBaseTime = 1.5f;
                    turtleBehavior.General.HoldGroundMaxRandom = 1.2f;
                    turtleBehavior.General.HoldGroundMinRandom = 0.8f;

                    turtleBehavior.Cover.CanShiftCoverPosition = true;
                    turtleBehavior.Cover.ShiftCoverTimeMultiplier = 2f;
                    turtleBehavior.Cover.MoveToCoverHasEnemySpeed = 1f;
                    turtleBehavior.Cover.MoveToCoverHasEnemyPose = 1f;
                    turtleBehavior.Cover.MoveToCoverNoEnemySpeed = 1f;
                    turtleBehavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    turtleBehavior.Talk.CanTaunt = true;
                    turtleBehavior.Talk.CanRespondToEnemyVoice = false;
                    turtleBehavior.Talk.TauntFrequency = 15;
                    turtleBehavior.Talk.TauntMaxDistance = 70f;
                    turtleBehavior.Talk.ConstantTaunt = false;
                    turtleBehavior.Talk.FrequentTaunt = false;
                    turtleBehavior.Talk.CanFakeDeathRare = true;
                    turtleBehavior.Talk.FakeDeathChance = 10;

                    turtleBehavior.Search.WillSearchForEnemy = true;
                    turtleBehavior.Search.WillSearchFromAudio = true;
                    turtleBehavior.Search.WillChaseDistantGunshots = false;
                    turtleBehavior.Search.SearchBaseTime = 90f;
                    turtleBehavior.Search.SprintWhileSearchChance = 0f;
                    turtleBehavior.Search.Sneaky = true;
                    turtleBehavior.Search.SneakyPose = 0f;
                    turtleBehavior.Search.SneakySpeed = 0f;
                    turtleBehavior.Search.SearchHasEnemySpeed = 1f;
                    turtleBehavior.Search.SearchHasEnemyPose = 1f;
                    turtleBehavior.Search.SearchNoEnemySpeed = 1f;
                    turtleBehavior.Search.SearchNoEnemyPose = 1f;
                    turtleBehavior.Search.SearchWaitMultiplier = 3f;

                    turtleBehavior.Rush.CanRushEnemyReloadHeal = true;
                    turtleBehavior.Rush.CanJumpCorners = true;
                    turtleBehavior.Rush.JumpCornerChance = 100f;
                    turtleBehavior.Rush.CanBunnyHop = true;
                    turtleBehavior.Rush.BunnyHopChance = 20;

                    AddPMCTypes(settings.Assignment.AllowedTypes);
                    Personalities.Add(SnappingTurtle, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, SnappingTurtle.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void initChad(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality Chad = EPersonality.Chad;
                if (!Personalities.ContainsKey(Chad))
                {
                    var settings = new PersonalitySettingsClass
                    {
                        Name = Chad.ToString(),
                        Description = "An aggressive player. Typically wearing high tier equipment, and is more aggressive than usual.",
                    };

                    var assignment = settings.Assignment;
                    assignment.Enabled = true;
                    assignment.RandomlyAssignedChance = 8;
                    assignment.CanBeRandomlyAssigned = true;
                    assignment.MaxChanceIfMeetRequirements = 80;
                    assignment.MinLevel = 0;
                    assignment.MaxLevel = 100;
                    assignment.PowerLevelMin = 100;
                    assignment.PowerLevelMax = 1000;
                    assignment.PowerLevelScaleStart = 100;
                    assignment.PowerLevelScaleEnd = 400;

                    var behavior = settings.Behavior;

                    behavior.General.AggressionMultiplier = 1;
                    behavior.General.HoldGroundBaseTime = 1.5f;
                    behavior.General.HoldGroundMaxRandom = 1.5f;
                    behavior.General.HoldGroundMinRandom = 0.75f;

                    behavior.Cover.CanShiftCoverPosition = true;
                    behavior.Cover.ShiftCoverTimeMultiplier = 1f;
                    behavior.Cover.MoveToCoverHasEnemySpeed = 1f;
                    behavior.Cover.MoveToCoverHasEnemyPose = 1f;
                    behavior.Cover.MoveToCoverNoEnemySpeed = 1f;
                    behavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    behavior.Talk.CanTaunt = true;
                    behavior.Talk.CanRespondToEnemyVoice = false;
                    behavior.Talk.TauntFrequency = 20;
                    behavior.Talk.TauntChance = 60;
                    behavior.Talk.TauntMaxDistance = 50f;
                    behavior.Talk.FrequentTaunt = true;
                    behavior.Talk.ConstantTaunt = false;
                    behavior.Talk.CanFakeDeathRare = false;
                    behavior.Talk.FakeDeathChance = 0;

                    behavior.Search.WillSearchForEnemy = true;
                    behavior.Search.WillSearchFromAudio = true;
                    behavior.Search.WillChaseDistantGunshots = true;
                    behavior.Search.SearchBaseTime = 16f;
                    behavior.Search.SprintWhileSearchChance = 30f;
                    behavior.Search.Sneaky = false;
                    behavior.Search.SneakyPose = 0f;
                    behavior.Search.SneakySpeed = 0f;
                    behavior.Search.SearchHasEnemySpeed = 1f;
                    behavior.Search.SearchHasEnemyPose = 1f;
                    behavior.Search.SearchNoEnemySpeed = 1f;
                    behavior.Search.SearchNoEnemyPose = 1f;
                    behavior.Search.SearchWaitMultiplier = 1f;

                    behavior.Rush.CanRushEnemyReloadHeal = true;
                    behavior.Rush.CanJumpCorners = false;
                    behavior.Rush.JumpCornerChance = 0f;
                    behavior.Rush.CanBunnyHop = false;
                    behavior.Rush.BunnyHopChance = 0f;

                    AddAllBotTypes(settings.Assignment.AllowedTypes);
                    Personalities.Add(Chad, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Chad.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void initRat(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality Rat = EPersonality.Rat;
                if (!Personalities.ContainsKey(Rat))
                {
                    var settings = new PersonalitySettingsClass
                    {
                        Name = Rat.ToString(),
                        Description = "Scum of Tarkov. Rarely Seeks out enemies, and when they do - they will crab walk all the way there",
                    };

                    var assignment = settings.Assignment;
                    assignment.Enabled = true;
                    assignment.RandomlyAssignedChance = 10;
                    assignment.CanBeRandomlyAssigned = true;
                    assignment.MaxChanceIfMeetRequirements = 60;
                    assignment.MinLevel = 0;
                    assignment.MaxLevel = 100;
                    assignment.PowerLevelMin = 0;
                    assignment.PowerLevelMax = 200;
                    assignment.PowerLevelScaleStart = 0;
                    assignment.PowerLevelScaleEnd = 200;
                    assignment.InverseScale = true;

                    var behavior = settings.Behavior;

                    behavior.General.AggressionMultiplier = 1;
                    behavior.General.HoldGroundBaseTime = 1f;
                    behavior.General.HoldGroundMaxRandom = 1.5f;
                    behavior.General.HoldGroundMinRandom = 0.75f;

                    behavior.Cover.CanShiftCoverPosition = false;
                    behavior.Cover.ShiftCoverTimeMultiplier = 1f;
                    behavior.Cover.MoveToCoverHasEnemySpeed = 0.5f;
                    behavior.Cover.MoveToCoverHasEnemyPose = 0.5f;
                    behavior.Cover.MoveToCoverNoEnemySpeed = 0.5f;
                    behavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    behavior.Talk.CanTaunt = false;
                    behavior.Talk.CanRespondToEnemyVoice = false;
                    behavior.Talk.TauntFrequency = 10;
                    behavior.Talk.TauntChance = 0;
                    behavior.Talk.TauntMaxDistance = 70f;
                    behavior.Talk.FrequentTaunt = false;
                    behavior.Talk.ConstantTaunt = false;
                    behavior.Talk.CanFakeDeathRare = false;
                    behavior.Talk.FakeDeathChance = 0;

                    behavior.Search.WillSearchForEnemy = true;
                    behavior.Search.WillSearchFromAudio = true;
                    behavior.Search.WillChaseDistantGunshots = false;
                    behavior.Search.SearchBaseTime = 240f;
                    behavior.Search.SprintWhileSearchChance = 0f;
                    behavior.Search.Sneaky = true;
                    behavior.Search.SneakyPose = 0f;
                    behavior.Search.SneakySpeed = 0f;
                    behavior.Search.SearchHasEnemySpeed = 0f;
                    behavior.Search.SearchHasEnemyPose = 0f;
                    behavior.Search.SearchNoEnemySpeed = 0f;
                    behavior.Search.SearchNoEnemyPose = 1f;
                    behavior.Search.SearchWaitMultiplier = 1f;

                    behavior.Rush.CanRushEnemyReloadHeal = false;
                    behavior.Rush.CanJumpCorners = false;
                    behavior.Rush.JumpCornerChance = 0f;
                    behavior.Rush.CanBunnyHop = false;
                    behavior.Rush.BunnyHopChance = 0f;

                    var allowedTypes = settings.Assignment.AllowedTypes;
                    AddAllBotTypes(allowedTypes);

                    allowedTypes.Remove(WildSpawnType.arenaFighter);
                    allowedTypes.Remove(WildSpawnType.exUsec);
                    allowedTypes.Remove(WildSpawnType.pmcBot);

                    Personalities.Add(Rat, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Rat.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void initTimmy(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality Timmy = EPersonality.Timmy;
                if (!Personalities.ContainsKey(Timmy))
                {
                    var settings = new PersonalitySettingsClass
                    {
                        Name = Timmy.ToString(),
                        Description = "A New Player, terrified of everything."
                    };

                    var assignment = settings.Assignment;
                    assignment.Enabled = true;
                    assignment.RandomlyAssignedChance = 0;
                    assignment.CanBeRandomlyAssigned = false;
                    assignment.MaxChanceIfMeetRequirements = 60f;
                    assignment.MinLevel = 0;
                    assignment.MaxLevel = 15;
                    assignment.PowerLevelMin = 0;
                    assignment.PowerLevelMax = 150f;
                    assignment.PowerLevelScaleStart = 0;
                    assignment.PowerLevelScaleEnd = 150;
                    assignment.InverseScale = true;

                    var behavior = settings.Behavior;

                    behavior.General.AggressionMultiplier = 1;
                    behavior.General.HoldGroundBaseTime = 0.5f;
                    behavior.General.HoldGroundMaxRandom = 1.5f;
                    behavior.General.HoldGroundMinRandom = 0.75f;

                    behavior.Cover.CanShiftCoverPosition = false;
                    behavior.Cover.ShiftCoverTimeMultiplier = 0.5f;
                    behavior.Cover.MoveToCoverHasEnemySpeed = 0.5f;
                    behavior.Cover.MoveToCoverHasEnemyPose = 0.5f;
                    behavior.Cover.MoveToCoverNoEnemySpeed = 0.5f;
                    behavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    behavior.Talk.CanTaunt = false;
                    behavior.Talk.CanRespondToEnemyVoice = false;
                    behavior.Talk.TauntFrequency = 10;
                    behavior.Talk.TauntMaxDistance = 70f;
                    behavior.Talk.FrequentTaunt = false;
                    behavior.Talk.ConstantTaunt = false;
                    behavior.Talk.CanFakeDeathRare = false;
                    behavior.Talk.FakeDeathChance = 0;
                    behavior.Talk.CanBegForLife = true;

                    behavior.Search.WillSearchForEnemy = true;
                    behavior.Search.WillSearchFromAudio = false;
                    behavior.Search.WillChaseDistantGunshots = false;
                    behavior.Search.SearchBaseTime = 90f;
                    behavior.Search.SprintWhileSearchChance = 0f;
                    behavior.Search.Sneaky = false;
                    behavior.Search.SneakyPose = 0f;
                    behavior.Search.SneakySpeed = 0f;
                    behavior.Search.SearchHasEnemySpeed = 0f;
                    behavior.Search.SearchHasEnemyPose = 1f;
                    behavior.Search.SearchNoEnemySpeed = 0f;
                    behavior.Search.SearchNoEnemyPose = 1f;
                    behavior.Search.SearchWaitMultiplier = 0.5f;

                    behavior.Rush.CanRushEnemyReloadHeal = false;
                    behavior.Rush.CanJumpCorners = false;
                    behavior.Rush.JumpCornerChance = 0f;
                    behavior.Rush.CanBunnyHop = false;
                    behavior.Rush.BunnyHopChance = 0f;

                    var allowedTypes = settings.Assignment.AllowedTypes;
                    AddAllBotTypes(allowedTypes);

                    allowedTypes.Remove(WildSpawnType.arenaFighter);
                    allowedTypes.Remove(WildSpawnType.exUsec);
                    allowedTypes.Remove(WildSpawnType.pmcBot);

                    Personalities.Add(Timmy, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Timmy.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void initCoward(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality Coward = EPersonality.Coward;
                if (!Personalities.ContainsKey(Coward))
                {
                    var settings = new PersonalitySettingsClass
                    {
                        Name = Coward.ToString(),
                        Description = "A player who is more passive and afraid than usual. Will never seek out enemies and will hide in a closet until the scary thing goes away."
                    };

                    var assignment = settings.Assignment;
                    assignment.Enabled = true;
                    assignment.RandomlyAssignedChance = 5;
                    assignment.CanBeRandomlyAssigned = true;
                    assignment.MaxChanceIfMeetRequirements = 30f;
                    assignment.MinLevel = 0;
                    assignment.MaxLevel = 100;
                    assignment.PowerLevelMin = 0;
                    assignment.PowerLevelMax = 250f;
                    assignment.PowerLevelScaleStart = 0;
                    assignment.PowerLevelScaleEnd = 250f;
                    assignment.InverseScale = true;

                    var behavior = settings.Behavior;

                    behavior.General.AggressionMultiplier = 1;
                    behavior.General.HoldGroundBaseTime = 0.25f;
                    behavior.General.HoldGroundMaxRandom = 1.5f;
                    behavior.General.HoldGroundMinRandom = 0.75f;

                    behavior.Cover.CanShiftCoverPosition = false;
                    behavior.Cover.MoveToCoverHasEnemySpeed = 0.5f;
                    behavior.Cover.MoveToCoverHasEnemyPose = 0.5f;
                    behavior.Cover.MoveToCoverNoEnemySpeed = 0.5f;
                    behavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    behavior.Talk.CanBegForLife = true;

                    behavior.Search.WillSearchForEnemy = false;
                    behavior.Search.WillSearchFromAudio = false;
                    behavior.Search.WillChaseDistantGunshots = false;

                    var allowedTypes = settings.Assignment.AllowedTypes;
                    AddAllBotTypes(allowedTypes);

                    allowedTypes.Remove(WildSpawnType.arenaFighter);
                    allowedTypes.Remove(WildSpawnType.exUsec);
                    allowedTypes.Remove(WildSpawnType.pmcBot);

                    Personalities.Add(Coward, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Coward.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void initNormal(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality Normal = EPersonality.Normal;
                if (!Personalities.ContainsKey(Normal))
                {
                    var settings = new PersonalitySettingsClass
                    {
                        Name = Normal.ToString(),
                        Description = "An Average Tarkov Enjoyer"
                    };

                    var assignment = settings.Assignment;

                    assignment.Enabled = true;
                    assignment.RandomlyAssignedChance = 0;
                    assignment.CanBeRandomlyAssigned = false;
                    assignment.MaxChanceIfMeetRequirements = 50f;
                    assignment.MinLevel = 0;
                    assignment.MaxLevel = 100;
                    assignment.PowerLevelMin = 0;
                    assignment.PowerLevelMax = 1000f;
                    assignment.PowerLevelScaleStart = 0;
                    assignment.PowerLevelScaleEnd = 1000f;
                    assignment.InverseScale = true;

                    var behavior = settings.Behavior;

                    behavior.General.AggressionMultiplier = 1;
                    behavior.General.HoldGroundBaseTime = 1f;
                    behavior.General.HoldGroundMaxRandom = 1.5f;
                    behavior.General.HoldGroundMinRandom = 0.5f;

                    behavior.Cover.CanShiftCoverPosition = true;
                    behavior.Cover.ShiftCoverTimeMultiplier = 1f;
                    behavior.Cover.MoveToCoverHasEnemySpeed = 0.75f;
                    behavior.Cover.MoveToCoverHasEnemyPose = 1f;
                    behavior.Cover.MoveToCoverNoEnemySpeed = 0.75f;
                    behavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    behavior.Talk.CanRespondToEnemyVoice = true;
                    behavior.Talk.TauntFrequency = 10;
                    behavior.Talk.TauntMaxDistance = 50f;

                    behavior.Search.WillSearchForEnemy = true;
                    behavior.Search.WillSearchFromAudio = true;
                    behavior.Search.WillChaseDistantGunshots = false;
                    behavior.Search.SearchBaseTime = 60f;
                    behavior.Search.SprintWhileSearchChance = 10f;
                    behavior.Search.SearchHasEnemySpeed = 1f;
                    behavior.Search.SearchHasEnemyPose = 1f;
                    behavior.Search.SearchNoEnemySpeed = 1f;
                    behavior.Search.SearchNoEnemyPose = 1f;
                    behavior.Search.SearchWaitMultiplier = 1f;

                    var allowedTypes = settings.Assignment.AllowedTypes;
                    AddAllBotTypes(allowedTypes);

                    Personalities.Add(Normal, settings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(settings, Preset.Info.Name, Normal.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void AddPMCTypes(List<WildSpawnType> allowedTypes)
            {
                allowedTypes.Add(WildSpawn.Usec);
                allowedTypes.Add(WildSpawn.Bear);
            }

            private static void AddAllBotTypes(List<WildSpawnType> allowedTypes)
            {
                allowedTypes.Clear();
                foreach (var botType in BotTypeDefinitions.BotTypes)
                {
                    allowedTypes.Add(botType.Key);
                }
            }
        }
    }

    public class PersonalityDictionary : Dictionary<EPersonality, PersonalitySettingsClass>
    { }
}