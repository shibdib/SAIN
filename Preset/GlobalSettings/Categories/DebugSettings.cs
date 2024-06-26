using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class DebugSettings : SAINSettingsBase<DebugSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        [Name("Global Debug Mode")]
        public bool GlobalDebugMode;

        [Name("Test Bot Sprint Pathfinder")]
        public bool ForceBotsToRunAround;

        [Name("Debug External")]
        public bool DebugExternal;

        [Name("Draw Debug Gizmos")]
        public bool DrawDebugGizmos;

        [Name("Draw Transform Gizmos")]
        public bool DrawTransformGizmos;

        [Name("Draw Debug Labels")]
        public bool DrawDebugLabels;

        [Name("Log Recoil Calculations")]
        public bool DebugRecoilCalculations = false;

        [Name("Draw Recoil Gizmos")]
        public bool DebugDrawRecoilGizmos = false;

        [Name("Log Aim Calculations")]
        public bool DebugAimCalculations = false;

        [Name("Draw Aim Gizmos")]
        public bool DebugDrawAimGizmos = false;

        [Name("Draw Blind Corner Raycasts")]
        public bool DebugDrawBlindCorner = false;

        [Name("Draw Debug Suppression Points")]
        [Hidden]
        public bool DebugDrawProjectionPoints = false;

        [Name("Draw Search Peek Start and End Gizmos")]
        public bool DebugSearchGizmos = false;

        [Name("Log Hearing Calc Results")]
        public bool DebugHearing = false;

        [Hidden]
        [JsonIgnore]
        public bool DebugMovementPlan = false;

        [Name("Draw Debug Path Safety Tester")]
        [Hidden]
        [JsonIgnore]
        public bool DebugDrawSafePaths = false;

        [Name("Path Safety Tester")]
        [Hidden]
        [JsonIgnore]
        public bool DebugEnablePathTester = false;

        [Name("Collect and Export Bot Layer and Brain Info")]
        [Hidden]
        [JsonIgnore]
        public bool CollectBotLayerBrainInfo = false;
    }
}