using EFT;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Plugin;
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
                    if (SAINPresetClass.Import(out PersonalitySettingsClass personality, Preset.Info.Name, item.ToString(), nameof(PersonalityDictionary)))
                    {
                        PersonalityDictionary.Add(item, personality);
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
                    string description = "A true alpha threat. Hyper Aggressive and typically wearing high tier equipment.";
                    var GigaChadSettings = new PersonalitySettingsClass(GigaChad, GigaChad.ToString(), description);

                    var gigaChadAssignment = GigaChadSettings.Assignment;
                    gigaChadAssignment.Enabled = true;
                    gigaChadAssignment.RandomlyAssignedChance = 3;
                    gigaChadAssignment.CanBeRandomlyAssigned = true;
                    gigaChadAssignment.MaxChanceIfMeetRequirements = 80;
                    gigaChadAssignment.MinLevel = 0;
                    gigaChadAssignment.MaxLevel = 100;
                    gigaChadAssignment.PowerLevelMin = 250;
                    gigaChadAssignment.PowerLevelMax = 1000;
                    gigaChadAssignment.PowerLevelScaleStart = 250;
                    gigaChadAssignment.PowerLevelScaleEnd = 500;

                    var gigaChadBehavior = GigaChadSettings.Behavior;

                    gigaChadBehavior.General.AggressionMultiplier = 1;
                    gigaChadBehavior.General.HoldGroundBaseTime = 1.25f;
                    gigaChadBehavior.General.HoldGroundMaxRandom = 1.5f;
                    gigaChadBehavior.General.HoldGroundMinRandom = 0.65f;

                    gigaChadBehavior.Cover.CanShiftCoverPosition = true;
                    gigaChadBehavior.Cover.ShiftCoverTimeMultiplier = 0.5f;
                    gigaChadBehavior.Cover.MoveToCoverHasEnemySpeed = 1f;
                    gigaChadBehavior.Cover.MoveToCoverHasEnemyPose = 1f;
                    gigaChadBehavior.Cover.MoveToCoverNoEnemySpeed = 1f;
                    gigaChadBehavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    gigaChadBehavior.Talk.CanTaunt = true;
                    gigaChadBehavior.Talk.CanRespondToEnemyVoice = true;
                    gigaChadBehavior.Talk.TauntFrequency = 6;
                    gigaChadBehavior.Talk.TauntMaxDistance = 75f;
                    gigaChadBehavior.Talk.ConstantTaunt = true;
                    gigaChadBehavior.Talk.FrequentTaunt = true;
                    gigaChadBehavior.Talk.CanFakeDeathRare = true;
                    gigaChadBehavior.Talk.FakeDeathChance = 3;

                    gigaChadBehavior.Search.WillSearchForEnemy = true;
                    gigaChadBehavior.Search.WillSearchFromAudio = true;
                    gigaChadBehavior.Search.WillChaseDistantGunshots = true;
                    gigaChadBehavior.Search.SearchBaseTime = 6;
                    gigaChadBehavior.Search.SprintWhileSearchChance = 60;
                    gigaChadBehavior.Search.SearchHasEnemySpeed = 1f;
                    gigaChadBehavior.Search.SearchHasEnemyPose = 1f;
                    gigaChadBehavior.Search.SearchNoEnemySpeed = 1f;
                    gigaChadBehavior.Search.SearchNoEnemyPose = 1f;
                    gigaChadBehavior.Search.SearchWaitMultiplier = 3f;

                    gigaChadBehavior.Rush.CanRushEnemyReloadHeal = true;
                    gigaChadBehavior.Rush.CanJumpCorners = true;
                    gigaChadBehavior.Rush.JumpCornerChance = 40f;
                    gigaChadBehavior.Rush.CanBunnyHop = true;
                    gigaChadBehavior.Rush.BunnyHopChance = 5;

                    AddPMCTypes(GigaChadSettings.Assignment.AllowedTypes);
                    Personalities.Add(GigaChad, GigaChadSettings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(GigaChadSettings, Preset.Info.Name, GigaChad.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void initWreckless(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality Wreckless = EPersonality.Wreckless;
                if (!Personalities.ContainsKey(Wreckless))
                {
                    string description = "Rush B Cyka Blyat. Who care if I die? Gotta get the clip";

                    var wrecklessSettings = new PersonalitySettingsClass(Wreckless, Wreckless.ToString(), description);

                    var wrecklessAssignment = wrecklessSettings.Assignment;
                    wrecklessAssignment.Enabled = true;
                    wrecklessAssignment.RandomlyAssignedChance = 1;
                    wrecklessAssignment.CanBeRandomlyAssigned = true;
                    wrecklessAssignment.MaxChanceIfMeetRequirements = 5;
                    wrecklessAssignment.MinLevel = 0;
                    wrecklessAssignment.MaxLevel = 100;
                    wrecklessAssignment.PowerLevelMin = 250;
                    wrecklessAssignment.PowerLevelMax = 1000;
                    wrecklessAssignment.PowerLevelScaleStart = 250;
                    wrecklessAssignment.PowerLevelScaleEnd = 500;

                    var wrecklessBehavior = wrecklessSettings.Behavior;

                    wrecklessBehavior.General.AggressionMultiplier = 1;
                    wrecklessBehavior.General.HoldGroundBaseTime = 2f;
                    wrecklessBehavior.General.HoldGroundMaxRandom = 2.5f;
                    wrecklessBehavior.General.HoldGroundMinRandom = 0.75f;

                    wrecklessBehavior.Cover.CanShiftCoverPosition = true;
                    wrecklessBehavior.Cover.ShiftCoverTimeMultiplier = 0.5f;
                    wrecklessBehavior.Cover.MoveToCoverHasEnemySpeed = 1f;
                    wrecklessBehavior.Cover.MoveToCoverHasEnemyPose = 1f;
                    wrecklessBehavior.Cover.MoveToCoverNoEnemySpeed = 1f;
                    wrecklessBehavior.Cover.MoveToCoverNoEnemyPose = 1f;

                    wrecklessBehavior.Talk.CanTaunt = true;
                    wrecklessBehavior.Talk.CanRespondToEnemyVoice = true;
                    wrecklessBehavior.Talk.TauntFrequency = 1;
                    wrecklessBehavior.Talk.TauntMaxDistance = 100f;
                    wrecklessBehavior.Talk.ConstantTaunt = true;
                    wrecklessBehavior.Talk.FrequentTaunt = true;
                    wrecklessBehavior.Talk.CanFakeDeathRare = true;
                    wrecklessBehavior.Talk.FakeDeathChance = 6;

                    wrecklessBehavior.Search.WillSearchForEnemy = true;
                    wrecklessBehavior.Search.WillSearchFromAudio = true;
                    wrecklessBehavior.Search.WillChaseDistantGunshots = true;
                    wrecklessBehavior.Search.SearchBaseTime = 0.1f;
                    wrecklessBehavior.Search.SprintWhileSearchChance = 85;
                    wrecklessBehavior.Search.SearchHasEnemySpeed = 1f;
                    wrecklessBehavior.Search.SearchHasEnemyPose = 1f;
                    wrecklessBehavior.Search.SearchNoEnemySpeed = 1f;
                    wrecklessBehavior.Search.SearchNoEnemyPose = 1f;
                    wrecklessBehavior.Search.SearchWaitMultiplier = 1f;

                    wrecklessBehavior.Rush.CanRushEnemyReloadHeal = true;
                    wrecklessBehavior.Rush.CanJumpCorners = true;
                    wrecklessBehavior.Rush.JumpCornerChance = 60f;
                    wrecklessBehavior.Rush.CanBunnyHop = true;
                    wrecklessBehavior.Rush.BunnyHopChance = 10;

                    AddAllBotTypes(wrecklessSettings.Assignment.AllowedTypes);
                    Personalities.Add(Wreckless, wrecklessSettings);
                    if (Preset.Info.IsCustom == true)
                    {
                        SAINPresetClass.Export(wrecklessSettings, Preset.Info.Name, Wreckless.ToString(), nameof(Personalities));
                    }
                }
            }

            private static void initSnappingTurtle(PersonalityDictionary Personalities, SAINPresetClass Preset)
            {
                EPersonality SnappingTurtle = EPersonality.SnappingTurtle;
                if (!Personalities.ContainsKey(SnappingTurtle))
                {
                    string description = "A player who finds the balance between rat and chad, yin and yang. Will rat you out but can spring out at any moment.";
                    var settings = new PersonalitySettingsClass(SnappingTurtle, SnappingTurtle.ToString(), description);

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
                    string description = "An aggressive player. Typically wearing high tier equipment, and is more aggressive than usual.";
                    var settings = new PersonalitySettingsClass(Chad, Chad.ToString(), description);
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
                    behavior.Talk.TauntFrequency = 10;
                    behavior.Talk.TauntMaxDistance = 70f;
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
                    string description = "Scum of Tarkov. Rarely Seeks out enemies, and when they do - they will crab walk all the way there";
                    var settings = new PersonalitySettingsClass(Rat, Rat.ToString(), description);
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
                    string description = "A New Player, terrified of everything.";
                    var settings = new PersonalitySettingsClass(Timmy, Timmy.ToString(), description);
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
                    string name = Coward.ToString();
                    string description = "A player who is more passive and afraid than usual. Will never seek out enemies and will hide in a closet until the scary thing goes away.";

                    var settings = new PersonalitySettingsClass(Coward, Coward.ToString(), description);
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
                    string description = "An Average Tarkov Enjoyer";
                    var settings = new PersonalitySettingsClass(Normal, Normal.ToString(), description);
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