using SAIN.Attributes;
using SAIN.Plugin;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Editor
{
    public class PresetEditorDefaults : SAINSettingsBase<PresetEditorDefaults>, ISAINSettings
    {
        public PresetEditorDefaults()
        {
            DefaultPreset = PresetHandler.DefaultPreset;
        }

        public PresetEditorDefaults(string selectedPreset)
        {
            SelectedCustomPreset = selectedPreset;
            DefaultPreset = PresetHandler.DefaultPreset;
        }

        [Hidden]
        public SAINDifficulty SelectedDefaultPreset = SAINDifficulty.none;

        [Hidden]
        public string SelectedCustomPreset;

        [Hidden]
        public string DefaultPreset;

        [Name("Show Advanced Bot Configs")]
        public bool AdvancedBotConfigs = false;

        [Name("GUI Size Scaling")]
        [MinMax(1f, 2f, 100f)]
        public float ConfigScaling = 1f;
    }
}