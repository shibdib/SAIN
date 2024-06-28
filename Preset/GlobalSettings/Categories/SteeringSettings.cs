using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class SteeringSettings : SAINSettingsBase<SteeringSettings>, ISAINSettings
    {
        [MinMax(30, 180f, 1f)]
        public float SteerSpeed_MaxAngle = 150f;
        [MinMax(0, 150f, 1f)]
        public float SteerSpeed_MinAngle = 1f;
        [MinMax(200, 500, 1f)]
        public float SteerSpeed_MaxSpeed = 360f;
        [MinMax(50, 300, 1f)]
        public float SteerSpeed_MinSpeed = 100f;
    }
}