using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class DebugSettings
    {
        [Name("Global Debug Mode")]
        [Default(false)]
        public bool GlobalDebugMode;

        [Name("Test Bot Sprint Pathfinder")]
        [Default(false)]
        public bool ForceBotsToRunAround;

        [Name("Debug External")]
        [Default(false)]
        public bool DebugExternal;

        [Name("Draw Debug Gizmos")]
        [Default(false)]
        public bool DrawDebugGizmos;

        [Name("Draw Transform Gizmos")]
        [Default(false)]
        public bool DrawTransformGizmos;

        [Name("Draw Debug Labels")]
        [Default(false)]
        public bool DrawDebugLabels;

        [Name("Log Recoil Calculations")]
        [Default(false)]
        public bool DebugRecoilCalculations = false;

        [Name("Draw Recoil Gizmos")]
        [Default(false)]
        public bool DebugDrawRecoilGizmos = false;

        [Name("Log Aim Calculations")]
        [Default(false)]
        public bool DebugAimCalculations = false;

        [Name("Draw Aim Gizmos")]
        [Default(false)]
        public bool DebugDrawAimGizmos = false;

        [Name("Draw Blind Corner Raycasts")]
        [Default(false)]
        public bool DebugDrawBlindCorner = false;

        [Name("Draw Debug Suppression Points")]
        [Default(false)]
        [Hidden]
        public bool DebugDrawProjectionPoints = false;

        [Name("Draw Search Peek Start and End Gizmos")]
        [Default(false)]
        public bool DebugSearchGizmos = false;

        [Name("Log Hearing Calc Results")]
        [Default(false)]
        public bool DebugHearing = false;

        [Default(false)]
        [Hidden]
        [JsonIgnore]
        public bool DebugMovementPlan = false;

        [Name("Draw Debug Path Safety Tester")]
        [Default(false)]
        [Hidden]
        [JsonIgnore]
        public bool DebugDrawSafePaths = false;

        [Name("Path Safety Tester")]
        [Default(false)]
        [Hidden]
        [JsonIgnore]
        public bool DebugEnablePathTester = false;

        [Name("Collect and Export Bot Layer and Brain Info")]
        [Default(false)]
        [Hidden]
        [JsonIgnore]
        public bool CollectBotLayerBrainInfo = false;
    }
}