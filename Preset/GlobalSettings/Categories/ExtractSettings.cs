using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class ExtractSettings
    {
        [JsonIgnore]
        [Hidden]
        public static readonly ExtractSettings Defaults = new ExtractSettings();

        [Default(true)]
        public bool EnableExtractsGlobal = true;
    }
}