
using Newtonsoft.Json;
using SAIN.Preset.GlobalSettings;
using System.Reflection;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINChangeSettings : SAINSettingsBase<SAINChangeSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

    }
}