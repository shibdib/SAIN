using SAIN.Helpers;
using SAIN.Preset;
using System.Collections.Generic;
using UnityEngine;
using static HBAO_Core;
using static SAIN.Attributes.AttributesGUI;
using static SAIN.Editor.SAINLayout;

namespace SAIN.Editor.GUISections
{
    public static class BotPersonalityEditor
    {
        public static void ClearCache()
        {
            ListHelpers.ClearCache(OpenPersMenus);
        }

        public static void PersonalityMenu()
        {
            string toolTip = $"Apply Values set below to Personalities. " +
                $"Exports edited values to SAIN/Presets/{SAINPlugin.LoadedPreset.Info.Name}/Personalities folder";

            if (BuilderClass.SaveChanges(PersonalitiesWereEdited, toolTip, 35))
            {
                SAINPresetClass.ExportAll(SAINPlugin.LoadedPreset);
            }

            foreach (var personality in SAINPlugin.LoadedPreset.PersonalityManager.PersonalityDictionary.Values)
            {
                string name = personality.Name;
                if (!OpenPersMenus.ContainsKey(name))
                {
                    OpenPersMenus.Add(name, false);
                }

                BeginHorizontal(80f);
                OpenPersMenus[name] = BuilderClass.ExpandableMenu(name, OpenPersMenus[name], personality.Description);
                EndHorizontal(80f);

                if (OpenPersMenus[name])
                {
                    EditAllValuesInObj(personality.Assignment, out bool newEdit);

                    if (newEdit)
                        PersonalitiesWereEdited = true;

                    EditAllValuesInObj(personality.Behavior.General, out newEdit);

                    if (newEdit)
                        PersonalitiesWereEdited = true;

                    EditAllValuesInObj(personality.Behavior.Rush, out newEdit);

                    if (newEdit)
                        PersonalitiesWereEdited = true;

                    EditAllValuesInObj(personality.Behavior.Search, out newEdit);

                    if (newEdit)
                        PersonalitiesWereEdited = true;

                    EditAllValuesInObj(personality.Behavior.Talk, out newEdit);

                    if (newEdit)
                        PersonalitiesWereEdited = true;

                    EditAllValuesInObj(personality.StatModifiers, out newEdit);

                    if (newEdit)
                        PersonalitiesWereEdited = true;
                }
            }
        }

        public static bool PersonalitiesWereEdited;

        private static readonly Dictionary<string, bool> OpenPersMenus = new Dictionary<string, bool>();
    }
}