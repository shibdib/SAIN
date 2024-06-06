using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class TimedVector3
    {
        public TimedVector3(Vector3 point, float timestamp)
        {
            Point = point;
            Timestamp = timestamp;
        }

        public Vector3 Point { get; set; }
        public float Timestamp { get; set; }
    }
}