using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class SteeringSettings : SAINSettingsBase<SteeringSettings>, ISAINSettings
    {
        [Name("Turn Angle Max")]
        [Description("The maximum angle, in degrees, to scale turn speed by.")]
        [Category("Character Turning")]
        [Advanced]
        [MinMax(30, 180f, 1f)]
        public float SteerSpeed_MaxAngle = 150f;

        [Name("Turn Angle Min")]
        [Description("The minimum angle, in degrees, to scale turn speed by.")]
        [Category("Character Turning")]
        [Advanced]
        [MinMax(0, 150f, 1f)]
        public float SteerSpeed_MinAngle = 5f;

        [Name("Turn Speed Max")]
        [Description("The maximum speed, in degrees per second, a bot can turn.")]
        [Category("Character Turning")]
        [Advanced]
        [MinMax(200, 500, 1f)]
        public float SteerSpeed_MaxSpeed = 360f;

        [Name("Turn Speed Min")]
        [Description("The minimum speed, in degrees per second, a bot can turn.")]
        [Category("Character Turning")]
        [Advanced]
        [MinMax(50, 300, 1f)]
        public float SteerSpeed_MinSpeed = 125f;

        [Name("Aim Turn Speed")]
        [Description("The maximum speed, in degrees per second, a bot can turn while they are aiming.")]
        [Category("Character Turning")]
        [Advanced]
        [MinMax(150f, 500f, 1f)]
        public float AimTurnSpeed = 300f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}