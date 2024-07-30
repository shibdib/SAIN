using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class SteeringSettings : SAINSettingsBase<SteeringSettings>, ISAINSettings
    {
        [Advanced]
        [MinMax(30, 180f, 1f)]
        public float SteerSpeed_MaxAngle = 150f;

        [Advanced]
        [MinMax(0, 150f, 1f)]
        public float SteerSpeed_MinAngle = 5f;

        [Advanced]
        [MinMax(200, 500, 1f)]
        public float SteerSpeed_MaxSpeed = 360f;

        [Advanced]
        [MinMax(50, 300, 1f)]
        public float SteerSpeed_MinSpeed = 125f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}