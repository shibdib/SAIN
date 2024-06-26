using SAIN.Editor;
using SAIN.Preset;
using System;
using System.Collections.Generic;
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
                if (def.BaseSAINDifficulty != SAINDifficulty.none)
                {
                    // This initializes the default preset, and sets default values to match what they are set to.
                    SAINDifficultyClass.GetDefaultPreset(def.BaseSAINDifficulty);
                }
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
}