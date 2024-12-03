using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class GlobalMoveSettings : SAINSettingsBase<GlobalMoveSettings>, ISAINSettings
    {
        [Advanced]
        [MinMax(150f, 500f, 1f)]
        public float AimTurnSpeed = 300f;

        [Category("Sprinting")]
        [Advanced]
        public bool EditSprintSpeed = false;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintNotMovingThreshold = 0.5f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintTryVaultTime = 0.25f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintTryJumpTime = 0.66f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(0.01f, 3f, 100f)]
        public float BotSprintRecalcTime = 1.5f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(0.01f, 1.5f, 1000f)]
        public float BotSprintFirstTurnDotThreshold = 0.925f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        public float BotSprintCornerReachDist = 0.15f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(0.1f, 1.5f, 100f)]
        public float BotSprintFinalDestReachDist = 0.25f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(0.01f, 2f, 100f)]
        public float BotSprintDistanceToStopSprintDestination = 1.2f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintMinDist = 0.5f;

        //[Advanced]
        //[MinMax(1f, 90f, 1f)]
        //public float BotSprintNextCornerAngleMax = 30f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(1f, 90f, 1f)]
        public float BotSprintCurrentCornerAngleMax = 25f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(1f, 500f, 1f)]
        public float BotSprintTurnSpeedWhileSprint = 300f;

        [Category("Sprinting")]
        [Advanced]
        [MinMax(1f, 500f, 1f)]
        public float BotSprintTurningSpeed = 400f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}