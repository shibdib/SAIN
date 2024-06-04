using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Preset;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SAIN.Helpers.JsonUtility;

namespace SAIN.Plugin
{
    public enum SAINDifficulty
    {
        none,
        easy,
        lesshard,
        hard,
        harderpmcs,
        veryhard ,
        deathwish,
        custom,
    }

    internal class PresetHandler
    {
        public const string DefaultPreset = "3. Default";
        public const string DefaultPresetDescription = "Bots are difficult but fair, the way SAIN was meant to played.";

        private const string Settings = "ConfigSettings";

        public static Action OnPresetUpdated;
        public static Action OnEditorSettingsChanged;

        public static readonly List<SAINPresetDefinition> CustomPresetOptions = new List<SAINPresetDefinition>();

        public static SAINPresetClass LoadedPreset;

        public static PresetEditorDefaults EditorDefaults;

        public static void LoadCustomPresetOptions()
        {
            Load.LoadCustommPresetOptions(CustomPresetOptions);
        }

        public static void Init()
        {
            ImportEditorDefaults();
            LoadCustomPresetOptions();
            SAINPresetDefinition presetDefinition = null;
            if (!EditorDefaults.SelectedCustomPreset.IsNullOrEmpty())
            {
                CheckIfPresetLoaded(EditorDefaults.SelectedCustomPreset, out presetDefinition);
            }
            InitPresetFromDefinition(presetDefinition);
        }

        public static bool LoadPresetDefinition(string presetKey, out SAINPresetDefinition definition)
        {
            for (int i = 0; i < CustomPresetOptions.Count; i++)
            {
                var preset = CustomPresetOptions[i];
                if (preset.IsCustom == true && preset.Name == presetKey)
                {
                    definition = preset;
                    return true;
                }
            }
            if (Load.LoadObject(out definition, "Info", PresetsFolder, presetKey))
            {
                if (definition.IsCustom == true)
                {
                    CustomPresetOptions.Add(definition);
                    return true;
                }
            }
            return false;
        }

        public static void SavePresetDefinition(SAINPresetDefinition definition)
        {
            if (definition.IsCustom == false)
            {
                return;
            }
            string baseName = definition.Name;
            for (int i = 0; i < 100; i++)
            {
                if (DoesFileExist("Info", PresetsFolder, definition.Name))
                {
                    definition.Name = baseName + $" Copy({i})";
                    continue;
                }
                break;
            }
            CustomPresetOptions.Add(definition);
            SaveObjectToJson(definition, "Info", PresetsFolder, definition.Name);
        }

        public static void InitPresetFromDefinition(SAINPresetDefinition def, bool isCopy = false)
        {
            if (def == null || def.IsCustom == false)
            {
                LoadedPreset = SAINDifficultyClass.GetDefaultPreset(EditorDefaults.SelectedDefaultPreset);
                if (LoadedPreset == null)
                {
                    LoadedPreset = SAINDifficultyClass.GetDefaultPreset(SAINDifficulty.hard);
                }
                UpdateExistingBots();
                ExportEditorDefaults();
                return;
            }

            try
            {
                LoadedPreset = new SAINPresetClass(def, isCopy);
            }
            catch (Exception ex)
            {
                Sounds.PlaySound(EFT.UI.EUISoundType.ErrorMessage);
                Logger.LogError(ex);

                LoadedPreset = SAINDifficultyClass.GetDefaultPreset(EditorDefaults.SelectedDefaultPreset);
                if (LoadedPreset == null)
                {
                    LoadedPreset = SAINDifficultyClass.GetDefaultPreset(SAINDifficulty.hard);
                }
            }
            UpdateExistingBots();
            ExportEditorDefaults();
        }

        public static void ExportEditorDefaults()
        {
            if (EditorDefaults.SelectedDefaultPreset == SAINDifficulty.none && LoadedPreset.Info.IsCustom)
            {
                EditorDefaults.SelectedCustomPreset = LoadedPreset.Info.Name;
            }
            else
            {
                EditorDefaults.SelectedCustomPreset = string.Empty;
            }
            SaveObjectToJson(EditorDefaults, Settings, PresetsFolder);
        }

        public static void ImportEditorDefaults()
        {
            if (Load.LoadObject(out PresetEditorDefaults editorDefaults, Settings, PresetsFolder))
            {
                EditorDefaults = editorDefaults;
            }
            else
            {
                EditorDefaults = new PresetEditorDefaults(DefaultPreset);
            }
        }

        public static void UpdateExistingBots()
        {
            OnPresetUpdated?.Invoke();
        }

        private static bool CheckIfPresetLoaded(string presetName, out SAINPresetDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(presetName))
            {
                return false;
            }
            for (int i = 0; i < CustomPresetOptions.Count; i++)
            {
                var presetDef = CustomPresetOptions[i];
                if (presetDef.Name.Contains(presetName) || presetDef.Name == presetName)
                {
                    definition = presetDef;
                    return true;
                }
            }
            return false;
        }
    }

    internal static class SAINDifficultyClass
    {
        const string PresetNameEasy = "Baby Bots";
        const string PresetNameNormal = "Less Difficult";
        const string PresetNameHard = "Default";
        const string PresetNameHarderPMCs = "Default with Harder PMCs";
        const string DefaultPresetDescription = "Bots are difficult but fair, the way SAIN was meant to played.";
        const string PresetNameVeryHard = "I Like Pain";
        const string PresetNameImpossible = "Death Wish";

        public static readonly Dictionary<SAINDifficulty, SAINPresetDefinition> DefaultPresetDefinitions = new Dictionary<SAINDifficulty, SAINPresetDefinition>();

        static SAINDifficultyClass()
        {
            DefaultPresetDefinitions.Add(
                SAINDifficulty.easy,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameEasy,
                    "Bots react slowly and are incredibly inaccurate."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.lesshard,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameNormal,
                    "Bots react more slowly, and are less accurate than usual."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.hard,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameHard,
                    DefaultPresetDescription));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.harderpmcs, 
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameHarderPMCs, 
                    "Default Settings, but PMCs are harder than normal."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.veryhard,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameVeryHard,
                    "Bots react faster, are more accurate, and can see further."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.deathwish,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameImpossible,
                    "Prepare To Die. Bots have almost no scatter, get less recoil from their weapon while shooting, are more accurate, and react deadly fast."));
        }

        public static SAINPresetClass GetDefaultPreset(SAINDifficulty difficulty)
        {
            switch (difficulty)
            {
                case SAINDifficulty.easy:
                    return SAINDifficultyClass.CreateEasyPreset();

                case SAINDifficulty.lesshard:
                    return SAINDifficultyClass.CreateNormalPreset();

                case SAINDifficulty.hard:
                    return SAINDifficultyClass.CreateHardPreset();

                case SAINDifficulty.harderpmcs:
                    return SAINDifficultyClass.CreateHarderPMCsPreset();

                case SAINDifficulty.veryhard:
                    return SAINDifficultyClass.CreateVeryHardPreset();

                case SAINDifficulty.deathwish:
                    return SAINDifficultyClass.CreateImpossiblePreset();

                default:
                    return null;
            }
        }

        private static SAINPresetClass CreateEasyPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.easy);

            var global = preset.GlobalSettings;
            global.Shoot.GlobalRecoilMultiplier = 2.0f;
            global.Shoot.GlobalScatterMultiplier = 1.5f;
            global.Aiming.AccuracySpreadMultiGlobal = 2f;
            global.Aiming.FasterCQBReactionsGlobal = false;
            global.General.GlobalDifficultyModifier = 0.65f;
            global.Look.GlobalVisionDistanceMultiplier = 0.66f;
            global.Look.GlobalVisionSpeedModifier = 1.75f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 0.5f, 0.01f, 1f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 120f;
                    setting.Value.Shoot.FireratMulti *= 0.6f;
                }
            }
            return preset;
        }

        private static SAINPresetClass CreateNormalPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.lesshard);

            var global = preset.GlobalSettings;
            global.Shoot.GlobalRecoilMultiplier = 1.6f;
            global.Shoot.GlobalScatterMultiplier = 1.2f;
            global.Aiming.AccuracySpreadMultiGlobal = 1.5f;
            global.Aiming.FasterCQBReactionsGlobal = false;
            global.General.GlobalDifficultyModifier = 0.75f;
            global.Look.GlobalVisionDistanceMultiplier = 0.85f;
            global.Look.GlobalVisionSpeedModifier = 1.25f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 0.85f, 0.01f, 1f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 150f;
                    setting.Value.Shoot.FireratMulti *= 0.8f;
                }
            }
            return preset;
        }

        private static SAINPresetClass CreateHardPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.hard);
            return preset;
        }

        private static SAINPresetClass CreateHarderPMCsPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.harderpmcs);
            ApplyHarderPMCs(preset);
            return preset;
        }

        private static void ApplyHarderPMCs(SAINPresetClass preset)
        {
            var botSettings = preset.BotSettings;
            foreach (var botsetting in botSettings.SAINSettings)
            {
                if (botsetting.Key == EnumValues.WildSpawn.Usec || botsetting.Key == EnumValues.WildSpawn.Bear)
                {
                    var pmcSettings = botsetting.Value.Settings;

                    // Set for all difficulties
                    foreach (var diff in pmcSettings.Values)
                    {
                        diff.Aiming.BASE_HIT_AFFECTION_MIN_ANG = 1f;
                        diff.Aiming.BASE_HIT_AFFECTION_MAX_ANG = 3f;
                        diff.Core.ScatteringPerMeter = 0.035f;
                        diff.Core.ScatteringClosePerMeter = 0.125f;
                        diff.Core.GainSightCoef -= 0.005f;
                        diff.Mind.WeaponProficiency = 0.7f;
                        diff.Scattering.ScatterMultiplier = 0.85f;
                    }

                    var easy = pmcSettings[BotDifficulty.easy];
                    easy.Aiming.FasterCQBReactionsDistance = 20f;
                    easy.Aiming.FasterCQBReactionsMinimum = 0.3f;
                    easy.Aiming.AccuracySpreadMulti = 0.9f;
                    easy.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.35f;
                    easy.Aiming.MAX_AIM_TIME = 1.5f;
                    easy.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.65f;
                    easy.Core.VisibleDistance = 200f;

                    var normal = pmcSettings[BotDifficulty.normal];
                    normal.Aiming.FasterCQBReactionsDistance = 35f;
                    normal.Aiming.FasterCQBReactionsMinimum = 0.25f;
                    normal.Aiming.AccuracySpreadMulti = 0.85f;
                    normal.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.4f;
                    normal.Aiming.MAX_AIM_TIME = 1.35f;
                    normal.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.5f;
                    normal.Core.VisibleDistance = 225f;

                    var hard = pmcSettings[BotDifficulty.hard];
                    hard.Aiming.FasterCQBReactionsDistance = 50f;
                    hard.Aiming.FasterCQBReactionsMinimum = 0.2f;
                    hard.Aiming.AccuracySpreadMulti = 0.8f;
                    hard.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.2f;
                    hard.Aiming.MAX_AIM_TIME = 1.15f;
                    hard.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.35f;
                    hard.Core.VisibleDistance = 250f;

                    var impossible = pmcSettings[BotDifficulty.impossible];
                    impossible.Aiming.FasterCQBReactionsDistance = 60f;
                    impossible.Aiming.FasterCQBReactionsMinimum = 0.15f;
                    impossible.Aiming.AccuracySpreadMulti = 0.75f;
                    impossible.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.15f;
                    impossible.Aiming.MAX_AIM_TIME = 1.0f;
                    impossible.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.25f;
                    impossible.Core.VisibleDistance = 275f;
                }
            }
        }

        private static SAINPresetClass CreateVeryHardPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.veryhard);

            var global = preset.GlobalSettings;
            global.Shoot.GlobalRecoilMultiplier = 0.66f;
            global.Shoot.GlobalScatterMultiplier = 0.85f;
            global.Aiming.AccuracySpreadMultiGlobal = 0.8f;
            global.Aiming.HeadShotProtection = false;
            global.General.GlobalDifficultyModifier = 1.35f;
            global.Look.GlobalVisionDistanceMultiplier = 1.33f;
            global.Look.GlobalVisionSpeedModifier = 0.8f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 1.33f, 0.01f, 1f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 170f;
                    setting.Value.Shoot.FireratMulti *= 1.2f;
                    //setting.Value.Core.VisibleDistance *= 1.25f;
                }
            }
            return preset;
        }

        private static SAINPresetClass CreateImpossiblePreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.deathwish);

            var global = preset.GlobalSettings;
            global.Shoot.GlobalRecoilMultiplier = 0.25f;
            global.Shoot.GlobalScatterMultiplier = 0.01f;
            global.Aiming.AccuracySpreadMultiGlobal = 0.33f;
            global.General.GlobalDifficultyModifier = 2f;
            global.Look.GlobalVisionDistanceMultiplier = 2f;
            global.Look.GlobalVisionSpeedModifier = 0.65f;
            global.Aiming.HeadShotProtection = false;
            global.Look.NotLookingToggle = false;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Sqrt(bot.Value.DifficultyModifier).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 180f;
                    setting.Value.Shoot.FireratMulti *= 2f;
                    //setting.Value.Core.VisibleDistance *= 1.25f;
                }
            }
            return preset;
        }
    }
}