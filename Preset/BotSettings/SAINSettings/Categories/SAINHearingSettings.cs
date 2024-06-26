using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;
using System.ComponentModel;
using System.Reflection;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINHearingSettings : SAINSettingsBase<SAINHearingSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

    }
}