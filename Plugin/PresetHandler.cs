using SAIN.Attributes;
using SAIN.Editor;
using SAIN.Preset;
using System;
using System.Collections.Generic;
using static SAIN.Helpers.JsonUtility;

namespace SAIN.Plugin
{
    internal static class ConfigEditingTracker
    {
        public static void Add(AttributesInfoClass info, object value)
        {
            if (EditedConfigValues.ContainsKey(info))
            {
                EditedConfigValues[info] = value;
                return;
            }
            EditedConfigValues.Add(info, value);
        }

        public static void Remove(AttributesInfoClass info)
        {
            EditedConfigValues.Remove(info);
        }

        public static bool WasEdited(AttributesInfoClass info)
        {
            return EditedConfigValues.ContainsKey(info);
        }

        public static void Clear()
        {
            EditedConfigValues.Clear();
        }

        public static bool UnsavedChanges => EditedConfigValues.Count > 0;

        public static readonly Dictionary<AttributesInfoClass, object> EditedConfigValues = new Dictionary<AttributesInfoClass, object>();
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
            Load.LoadCustomPresetOptions(CustomPresetOptions);
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

        private static void loadDefault()
        {
            LoadedPreset = SAINDifficultyClass.GetDefaultPreset(EditorDefaults.SelectedDefaultPreset) ?? SAINDifficultyClass.GetDefaultPreset(SAINDifficulty.hard);
            LoadedPreset.Init();
            LoadedPreset.UpdateDefaults();
        }

        public static void InitPresetFromDefinition(SAINPresetDefinition def, bool isCopy = false)
        {
            if (def == null || def.IsCustom == false)
            {
                loadDefault();
                UpdateExistingBots();
                ExportEditorDefaults();
                return;
            }

            try
            {
                var defaultPreset = SAINDifficultyClass.GetDefaultPreset(def.BaseSAINDifficulty);

                LoadedPreset = new SAINPresetClass(def, isCopy);
                LoadedPreset.Init();

                if (defaultPreset != null)
                    LoadedPreset.UpdateDefaults(defaultPreset);
            }
            catch (Exception ex)
            {
                Sounds.PlaySound(EFT.UI.EUISoundType.ErrorMessage);
                Logger.LogError(ex);
                loadDefault();
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
            LoadedPreset?.GlobalSettings.Update();
            LoadedPreset?.PersonalityManager.Update();
            LoadedPreset?.BotSettings.Update();
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