using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Plugin;

namespace SAIN.Editor
{
    public class PresetEditorDefaults
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
        [Default(false)]
        public bool AdvancedBotConfigs;

        [Name("GUI Size Scaling")]
        [Default(1f)]
        [MinMax(1f, 2f, 100f)]
        public float ConfigScaling = 1f;
    }
}