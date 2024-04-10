using EFT.UI;
using SAIN.Plugin;
using SAIN.Preset;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using static Mono.Security.X509.X520;
using static SAIN.Editor.SAINLayout;

namespace SAIN.Editor.GUISections
{
    public static class PresetSelection
    {
        public static bool Menu()
        {
            const float LabelHeight = 55f;

            SAINPresetDefinition selectedPreset = SAINPlugin.LoadedPreset.Info;

            GUIContent content = new GUIContent(
                        $"Selected Preset Version: {selectedPreset.SAINVersion} " +
                        $"but current SAIN Preset Version is: {AssemblyInfoClass.SAINPresetVersion}, you may experience issues.");

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
                PresetHandler.LoadPresetOptions();
            }
            OpenNewPresetMenu = Toggle(OpenNewPresetMenu, new GUIContent("Create New Preset"), EUISoundType.ButtonClick, Height(LabelHeight), Width(150));
            EndVertical();

            BeginVertical();
            Label("Default Presets", Width(presetWidth));
            for (int i = 0; i < PresetHandler.PresetOptions.Count; i++)
            {
                var preset = PresetHandler.PresetOptions[i];
                if (preset.IsCustom == false)
                {
                    bool selected = selectedPreset.Name == preset.Name;

                    if (Toggle(
                        selected,
                        $"{preset.Name} for SAIN {preset.SAINVersion}",
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

            BeginVertical();
            Label("Custom Presets", Width(presetWidth));
            for (int i = 0; i < PresetHandler.PresetOptions.Count; i++)
            {
                var preset = PresetHandler.PresetOptions[i];
                if (preset.IsCustom == true)
                {
                    bool selected = selectedPreset.Name == preset.Name;

                    if (Toggle(
                        selected,
                        $"{preset.Name} for SAIN {preset.SAINVersion}",
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
                if (Button("Save New Preset", EFT.UI.EUISoundType.InsuranceInsured, Height(30f)))
                {
                    var definition = new SAINPresetDefinition()
                    {
                        Name = NewName,
                        Description = NewDescription,
                        Creator = NewCreator,
                        SAINVersion = AssemblyInfoClass.SAINPresetVersion,
                        DateCreated = DateTime.Today.ToString()
                    };
                    PresetHandler.PresetOptions.Add(definition);
                    PresetHandler.SavePresetDefinition(definition);
                    PresetHandler.InitPresetFromDefinition(definition);

                    OpenNewPresetMenu = !OpenNewPresetMenu;
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
