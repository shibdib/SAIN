using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class ExtractSettings : SAINSettingsBase<ExtractSettings>, ISAINSettings
    {
        public bool EnableExtractsGlobal = true;
    }
}