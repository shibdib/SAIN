using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.BotSettings;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.GearStealthValues;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.Personalities;
using System;
using static SAIN.Helpers.JsonUtility;

namespace SAIN.Preset
{
    public class SAINPresetClass
    {
        public SAINPresetClass(SAINPresetDefinition preset, bool isCopy = false)
        {
            SAINPlugin.EditorDefaults.SelectedDefaultPreset = SAINDifficulty.none;
            if (isCopy && SAINPlugin.LoadedPreset != null)
            {
                SAINPresetDefinition oldDefinition = SAINPlugin.LoadedPreset.Info;
                SAINPlugin.LoadedPreset.Info = preset;
                ExportAll(SAINPlugin.LoadedPreset);
                SAINPlugin.LoadedPreset.Info = oldDefinition;
            }
            Info = preset;
            GlobalSettings = GlobalSettingsClass.ImportGlobalSettings(preset);
            BotSettings = new BotSettings.SAINBotSettingsClass(this);
            PersonalityManager = new PersonalityManagerClass(this);
            GearStealthValuesClass = new GearStealthValuesClass(preset);
        }

        public GearStealthValuesClass GearStealthValuesClass { get; private set; }

        public SAINPresetClass(SAINDifficulty sainDifficulty)
        {
            EFTCoreSettings.UpdateCoreSettings();

            SAINPlugin.EditorDefaults.SelectedCustomPreset = string.Empty;
            SAINPlugin.EditorDefaults.SelectedDefaultPreset = sainDifficulty;
            PresetHandler.ExportEditorDefaults();

            Info = SAINDifficultyClass.DefaultPresetDefinitions[sainDifficulty];
            GlobalSettings = new GlobalSettingsClass();
            BotSettings = new BotSettings.SAINBotSettingsClass(this);
            PersonalityManager = new PersonalityManagerClass(this);
            GearStealthValuesClass = new GearStealthValuesClass(Info);
        }

        public void Init()
        {
            GlobalSettings.Init();
            BotSettings.Init();
            PersonalityManager.Init();
        }

        public void UpdateDefaults(SAINPresetClass preset = null)
        {
            GlobalSettings.UpdateDefaults(preset?.GlobalSettings);
            BotSettings.UpdateDefaults(preset?.BotSettings);
            PersonalityManager.UpdateDefaults(preset?.PersonalityManager);
        }

        public static void ExportAll(SAINPresetClass preset)
        {
            ConfigEditingTracker.Clear();

            if (preset.Info.IsCustom == false)
            {
                SAINPresetDefinition newPreset = preset.Info.Clone();

                newPreset.Name += " [Modified]";
                newPreset.Creator = "user";
                newPreset.Description = "[Modified] " + newPreset.Description;
                newPreset.DateCreated = DateTime.Today.ToString();

                PresetHandler.SavePresetDefinition(newPreset);
                PresetHandler.InitPresetFromDefinition(newPreset, true);
                PresetHandler.UpdateExistingBots();
                return;
            }

            ExportDefinition(preset.Info);
            ExportGlobalSettings(preset.GlobalSettings, preset.Info.Name);
            ExportPersonalities(preset.PersonalityManager, preset.Info.Name);
            ExportBotSettings(preset.BotSettings, preset.Info.Name);
            GearStealthValuesClass.Export(preset.GearStealthValuesClass, preset.Info);
            PresetHandler.UpdateExistingBots();
        }

        private static void ExportDefinition(SAINPresetDefinition info)
        {
            if (info.IsCustom == false)
            {
                return;
            }
            try
            {
                Export(info, info.Name, "Info");
            }
            catch (Exception updateEx)
            {
                LogExportError(updateEx);
            }
        }

        private static bool ExportGlobalSettings(GlobalSettingsClass globalSettings, string presetName)
        {
            bool success = false;
            try
            {
                Export(globalSettings, presetName, "GlobalSettings");
                success = true;
            }
            catch (Exception ex)
            {
                LogExportError(ex);
            }
            return success;
        }

        private static bool ExportPersonalities(PersonalityManagerClass personClass, string presetName)
        {
            bool success = false;
            try
            {
                foreach (var pers in personClass.PersonalityDictionary)
                {
                    if (pers.Value != null && Export(pers.Value, presetName, pers.Key.ToString(), nameof(Personalities)))
                    {
                        continue;
                    }
                    else if (pers.Value == null)
                    {
                        Logger.LogError("Personality Settings Are Null");
                    }
                    else
                    {
                        Logger.LogError($"Failed to Export {pers.Key}");
                    }
                }
                success = true;
            }
            catch (Exception ex)
            {
                LogExportError(ex);
            }
            return success;
        }

        private static bool ExportBotSettings(SAINBotSettingsClass botSettings, string presetName)
        {
            bool success = false;
            try
            {
                foreach (SAINSettingsGroupClass settings in botSettings.SAINSettings.Values)
                {
                    Export(settings, presetName, settings.Name, "BotSettings");
                }
                success = true;
            }
            catch (Exception ex)
            {
                LogExportError(ex);
            }
            return success;
        }

        public static bool Export(object obj, string presetName, string fileName, string subFolder = null)
        {
            bool success = false;
            try
            {
                string[] folders = Folders(presetName, subFolder);
                SaveObjectToJson(obj, fileName, folders);
                success = true;

                string debugFolders = string.Empty;
                for (int i = 0; i < folders.Length; i++)
                {
                    debugFolders += $"/{folders[i]}";
                }
                Logger.LogDebug($"Successfully Exported [{obj.GetType().Name}] : Name: [{fileName}] To: [{debugFolders}]");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed Export of Type [{obj.GetType().Name}] Name: [{fileName}]");
                LogExportError(ex);
            }
            return success;
        }

        public static bool Import<T>(out T result, string presetName, string fileName, string subFolder = null)
        {
            string[] folders = Folders(presetName, subFolder);
            if (Load.LoadJsonFile(out string json, fileName, folders))
            {
                try
                {
                    result = Load.DeserializeObject<T>(json);

                    string debugFolders = string.Empty;
                    for (int i = 0; i < folders.Length; i++)
                    {
                        debugFolders += $"/{folders[i]}";
                    }
                    Logger.LogDebug($"Successfully Imported [{typeof(T).Name}] File Name: [{fileName}] To Path: [{debugFolders}]");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed import Item of Type {typeof(T)}");
                    LogExportError(ex);
                }
            }
            result = default;
            return false;
        }

        public static string[] Folders(string presetName, string subFolder = null)
        {
            string presets = "Presets";
            string[] result;
            if (subFolder == null)
            {
                result = new string[]
                {
                    presets,
                    presetName
                };
            }
            else
            {
                result = new string[]
                {
                    presets,
                    presetName,
                    subFolder
                };
            }
            return result;
        }

        public SAINPresetDefinition Info;
        public GlobalSettingsClass GlobalSettings;
        public BotSettings.SAINBotSettingsClass BotSettings;
        public PersonalityManagerClass PersonalityManager;

        private static void LogExportError(Exception ex)
        {
            Logger.LogError($"Export Error: {ex}");
        }
    }

    public abstract class BasePreset
    {
        public BasePreset(SAINPresetClass presetClass)
        {
            Preset = presetClass;
            Info = presetClass.Info;
        }

        public readonly SAINPresetClass Preset;
        public readonly SAINPresetDefinition Info;
    }
}