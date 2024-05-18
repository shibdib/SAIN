using EFT.UI;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static Mono.Security.X509.X520;
using static SAIN.Editor.SAINLayout;

namespace SAIN.Editor.GUISections
{
    public static class PresetSelection
    {
        private static readonly List<SAINPresetDefinition> defaultPresets = SAINDifficultyClass.DefaultPresetDefinitions.Values.ToList();

        public static bool Menu()
        {
            const float LabelHeight = 55f;

            SAINPresetDefinition selectedPreset = SAINPlugin.LoadedPreset.Info;

            GUIContent content = new GUIContent(
                        $"Selected Preset Version: {selectedPreset.SAINVersion} " +
                        $"but current SAIN Preset Version is: {AssemblyInfoClass.SAINPresetVersion}, default bot config values may be set incorrectly due to updates to SAIN.");

            Rect rect = GUILayoutUtility.GetRect(content, GetStyle(Style.alert), Height(LabelHeight + 5));
            if (selectedPreset.SAINVersion != AssemblyInfoClass.SAINPresetVersion)
            {
                GUI.Box(rect, content, GetStyle(Style.alert));
                //Box(content, GetStyle(Style.alert), Height(LabelHeight + 5));
            }
            else
            {
                GUI.Box(rect, new GUIContent(""), GetStyle(Style.blankbox));
            }

            BeginHorizontal();

            const float optionHeight = 25f;
            const float presetWidth = 500;

            BeginVertical();
            Box("Presets", "Select an Installed preset for SAIN Settings", Height(LabelHeight), Width(150));
            if (Button("Refresh", "Refresh installed Presets", EUISoundType.ButtonClick, Height(LabelHeight), Width(150)))
            {
                PresetHandler.LoadCustomPresetOptions();
            }
            OpenNewPresetMenu = Toggle(OpenNewPresetMenu, new GUIContent("Create New Preset"), EUISoundType.ButtonClick, Height(LabelHeight), Width(150));
            EndVertical();

            BeginVertical();
            Label("Default Presets", Width(presetWidth));

            for (int i = 0; i < defaultPresets.Count; i++)
            {
                var preset = defaultPresets[i];
                if (SAINDifficultyClass.DefaultPresetDefinitions.TryGetKey(preset, out var sainDifficulty))
                {
                    bool selected = SAINPlugin.EditorDefaults.SelectedDefaultPreset == sainDifficulty;

                    if (Toggle(
                        selected,
                        $"{preset.Name}",
                        preset.Description,
                        EUISoundType.MenuCheckBox,
                        Height(optionHeight), Width(presetWidth)
                        ))
                    {
                        if (!selected)
                        {
                            SAINPlugin.EditorDefaults.SelectedDefaultPreset = sainDifficulty;
                            selectedPreset = preset;
                        }
                    }
                }
            }
            EndVertical();

            BeginVertical();
            Label("Custom Presets", Width(presetWidth));
            for (int i = 0; i < PresetHandler.CustomPresetOptions.Count; i++)
            {
                var preset = PresetHandler.CustomPresetOptions[i];
                if (preset.IsCustom == true)
                {
                    bool selected = SAINPlugin.EditorDefaults.SelectedDefaultPreset == SAINDifficulty.none 
                        && selectedPreset.Name == preset.Name;

                    if (Toggle(
                        selected,
                        $"{preset.Name}",
                        preset.Description,
                        EUISoundType.MenuCheckBox,
                        Height(optionHeight), Width(presetWidth)
                        ))
                    {
                        if (!selected)
                        {
                            selectedPreset = preset;
                        }
                    }
                }
            }
            EndVertical();

            if (OpenNewPresetMenu)
            {
                BeginVertical();

                BeginHorizontal();
                Space(25);
                SAINPresetDefinition info = SAINPlugin.LoadedPreset.Info;
                if (info.CanEditName && Button("Save Info", "Update the selected presets name, description, and creator.", EFT.UI.EUISoundType.InsuranceInsured, Height(30f)))
                {
                    string oldName = info.Name;
                    var newInfo = info.Clone();

                    newInfo.Name = NewName;
                    newInfo.Description = NewDescription;
                    newInfo.Creator = NewCreator;

                    DeletePreset(info);

                    PresetHandler.SavePresetDefinition(newInfo);
                    PresetHandler.InitPresetFromDefinition(newInfo, true);
                    PresetHandler.LoadCustomPresetOptions();
                }
                if (Button("Save A New Preset", EFT.UI.EUISoundType.InsuranceInsured, Height(30f)))
                {
                    SAINPresetDefinition newPreset = SAINPlugin.LoadedPreset.Info.Clone();

                    newPreset.Name = NewName;
                    newPreset.Description = NewDescription;
                    newPreset.Creator = NewCreator;
                    newPreset.SAINVersion = AssemblyInfoClass.SAINPresetVersion;
                    newPreset.DateCreated = DateTime.Today.ToString();

                    PresetHandler.SavePresetDefinition(newPreset);
                    PresetHandler.InitPresetFromDefinition(newPreset, true);
                }
                Space(25);
                EndHorizontal();

                Space(3);

                NewName = LabeledTextField(NewName, "Name");
                NewDescription = LabeledTextField(NewDescription, "Description");
                NewCreator = LabeledTextField(NewCreator, "Creator");

                EndVertical();
            }

            FlexibleSpace();
            EndHorizontal();

            if (selectedPreset.Name != SAINPlugin.LoadedPreset.Info.Name)
            {
                PresetHandler.InitPresetFromDefinition(selectedPreset);
                return true;
            }
            return false;
        }

        private static void DeletePreset(SAINPresetDefinition preset)
        {
            if (JsonUtility.GetFoldersPath(out string foldersPath, "Presets", preset.Name))
            {
                string filePath = Path.Combine(foldersPath, "Info");
                filePath += ".json";

                Logger.NotifyDebug($"Trying to delete: {foldersPath}");
                if (Directory.Exists(foldersPath))
                {
                    Directory.Delete(foldersPath, true);
                    Logger.NotifyDebug($"Deleted: {foldersPath}");
                }
            }
            PresetHandler.CustomPresetOptions.Remove(preset);
        }

        private static string LabeledTextField(string value, string label)
        {
            BeginHorizontal();
            Box(label, Width(125f), Height(30));
            value = TextField(value, null, Width(350f), Height(30));
            EndHorizontal();

            return Regex.Replace(value, @"[^\w \-]", "");
        }

        private static bool OpenNewPresetMenu;

        private static string NewName = "Enter Name Here";
        private static string NewDescription = "Enter Description Here";
        private static string NewCreator = "Your Name Here";
    }
}
