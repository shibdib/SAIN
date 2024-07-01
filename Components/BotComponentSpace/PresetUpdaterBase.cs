using SAIN.Plugin;
using SAIN.Preset;

namespace SAIN.SAINComponent
{
    public abstract class PresetUpdaterBase
    {
        public PresetUpdaterBase(object obj)
        {
            _obj = obj;
            PresetHandler.OnPresetUpdated += checkUpdateSettings;
            UpdatePresetSettings(SAINPlugin.LoadedPreset);
        }

        private readonly object _obj;

        private void checkUpdateSettings(SAINPresetClass preset)
        {
            if (_obj == null)
            {
                PresetHandler.OnPresetUpdated -= checkUpdateSettings;
                Logger.LogDebug($"Object is null, Unsubd to Preset Updater Event");
                return;
            }
            UpdatePresetSettings(preset);
        }

        public virtual void UpdatePresetSettings(SAINPresetClass preset)
        {

        }
    }
}