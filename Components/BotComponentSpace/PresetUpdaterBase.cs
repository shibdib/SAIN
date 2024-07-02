using SAIN.Plugin;
using SAIN.Preset;

namespace SAIN.SAINComponent
{
    public abstract class PresetUpdaterBase
    {
        protected void Subscribe()
        {
            PresetHandler.OnPresetUpdated += UpdatePresetSettings;
        }

        protected void UnSubscribe()
        {
            PresetHandler.OnPresetUpdated -= UpdatePresetSettings;
        }

        protected virtual void UpdatePresetSettings(SAINPresetClass preset) { }
    }
}