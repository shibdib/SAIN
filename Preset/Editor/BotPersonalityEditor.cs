using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using System.Collections.Generic;
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

            if (BuilderClass.SaveChanges(ConfigEditingTracker.GetUnsavedValuesString(), 35))
            {
                SAINPresetClass.ExportAll(SAINPlugin.LoadedPreset);
            }

            var personalities = SAINPresetClass.Instance.PersonalityManager.PersonalityDictionary;
            if (_options.Count == 0)
            {
                _options.AddRange(personalities.Keys);
            }

            _selected = BuilderClass.SelectionGrid(_selected, 35f, 4, _options);
            if (_selected == EPersonality.None)
            {
                return;
            }

            if (personalities.TryGetValue(_selected, out var settings))
            {
                EditAllValuesInObj(settings, out bool newEdit, null, null, 1);
            }
            return;

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
                    EditAllValuesInObj(personality, out bool newEdit,null ,null , 1);
                }
            }
        }

        private static EPersonality _selected = EPersonality.None;
        public static bool PersonalitiesWereEdited => ConfigEditingTracker.UnsavedChanges;

        private static List<EPersonality> _options = new List<EPersonality>();

        private static readonly Dictionary<string, bool> OpenPersMenus = new Dictionary<string, bool>();
    }
}