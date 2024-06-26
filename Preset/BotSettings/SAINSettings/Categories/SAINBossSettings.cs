using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;
using System.ComponentModel;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINBossSettings : SAINSettingsBase<SAINBossSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        [Hidden]
        public bool SET_CHEAT_VISIBLE_WHEN_ADD_TO_ENEMY = false;
    }
}