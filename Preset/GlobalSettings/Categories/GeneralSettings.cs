using HarmonyLib;
using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class GeneralSettings : SAINSettingsBase<GeneralSettings>, ISAINSettings
    {
        [Name("Bots Use Grenades")]
        public bool BotsUseGrenades = true;

        [Name("Bots Open Doors Fast")]
        [Description("WIP. Can cause bots to get stuck on doors sometimes.")]
        public bool NewDoorOpening = true;

        [Name("Random Speed Hacker AI")]
        [Description("Emulate the real Live-Like experience! 1% of bots will be a speed-hacker.")]
        public bool RandomSpeedHacker = false;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [MinMax(0, 100)]
        [Hidden]
        [JsonIgnore]
        public int SAINCombatSquadLayerPriority = 22;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [MinMax(0, 100)]
        [Hidden]
        [JsonIgnore]
        public int SAINExtractLayerPriority = 24;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [MinMax(0, 100)]
        [Hidden]
        [JsonIgnore]
        public int SAINCombatSoloLayerPriority = 20;

        [JsonIgnore]
        [Hidden]
        public float SprintReachDistance = 1f;

        [JsonIgnore]
        [Hidden]
        public float BaseReachDistance = 0.5f;
    }
}