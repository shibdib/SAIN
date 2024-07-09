using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class LayerSettings : SAINSettingsBase<LayerSettings>, ISAINSettings
    {
        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [MinMax(0, 100)]
        [Hidden]
        public int SAINCombatSquadLayerPriority = 22;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [MinMax(0, 100)]
        [Hidden]
        public int SAINExtractLayerPriority = 24;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [MinMax(0, 100)]
        [Hidden]
        public int SAINCombatSoloLayerPriority = 20;
    }

}