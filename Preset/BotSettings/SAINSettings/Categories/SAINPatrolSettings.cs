using EFT;
using Newtonsoft.Json;
using SAIN.Preset.GlobalSettings;
using System.Reflection;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINPatrolSettings : SAINSettingsBase<SAINPatrolSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

    }
}
