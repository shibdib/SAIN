using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;

namespace SAIN.Preset.Personalities
{
    public interface ISettingsGroup
    {
        void Init();
        List<ISAINSettings> SettingsList { get; }
        void InitList();
        void CreateDefaults();
        void UpdateDefaults(ISettingsGroup replacementValues = null);
    }
}