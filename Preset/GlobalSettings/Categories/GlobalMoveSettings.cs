using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class GlobalMoveSettings
    {
        [Advanced]
        [Default(true)]
        public bool EditSprintSpeed = true;

        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        [Default(0.05f)]
        public float BotSprintNotMovingThreshold = 0.1f;

        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        [Default(0.25f)]
        public float BotSprintTryVaultTime = 0.1f;

        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        [Default(0.66f)]
        public float BotSprintTryJumpTime = 0.45f;

        [Advanced]
        [MinMax(0.01f, 3f, 100f)]
        [Default(1f)]
        public float BotSprintRecalcTime = 1f;

        [Advanced]
        [MinMax(0.01f, 1.5f, 1000f)]
        [Default(0.925f)]
        public float BotSprintFirstTurnDotThreshold = 0.925f;

        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        [Default(0.1f)]
        public float BotSprintBaseReachDist = 0.15f;

        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        [Default(0.5f)]
        public float BotSprintBufferDist = 0.5f;

        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        [Default(0.25f)]
        public float BotSprintMinDist = 0.25f;

        [Advanced]
        [MinMax(1f, 90f, 1f)]
        [Default(30f)]
        public float BotSprintNextCornerAngleMax = 30f;

        [Advanced]
        [MinMax(1f, 90f, 1f)]
        [Default(25f)]
        public float BotSprintCurrentCornerAngleMax = 25f;

        [Advanced]
        [MinMax(1f, 500f, 1f)]
        [Default(250f)]
        public float BotSprintTurnSpeed = 250f;

        [Advanced]
        [MinMax(1f, 500f, 1f)]
        [Default(400f)]
        public float BotSprintFirstTurnSpeed = 400f;
    }
}