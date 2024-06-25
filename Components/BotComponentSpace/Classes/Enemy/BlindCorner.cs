using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public struct BlindCorner
    {
        public BlindCorner(Vector3 groundPoint, float angle)
        {
            GroundPosition = groundPoint;
            Angle = angle;
            _nextLookPointTime = 0f;
            _blindCornerLookPoint = groundPoint;
        }

        public Vector3 GroundPosition { get; }

        public float Angle { get; }

        public Vector3 Corner(Vector3 eyePos, Vector3 botPosition)
        {
            Vector3 blindCorner = GroundPosition;
            blindCorner.y += (eyePos - botPosition).y;
            return blindCorner;
        }

        public Vector3 PointPastCorner(Vector3 eyePos, Vector3 botPosition)
        {
            if (_nextLookPointTime < Time.time)
            {
                _nextLookPointTime = Time.time + LOOK_POINT_FREQUENCY;
                Vector3 corner = Corner(eyePos, botPosition);
                _blindCornerLookPoint = BlindCornerFinder.RaycastPastCorner(corner, eyePos, 0f, 2f);
            }
            return _blindCornerLookPoint;

        }

        private Vector3 _blindCornerLookPoint;
        private float _nextLookPointTime;
        const float LOOK_POINT_FREQUENCY = 1f / 10f;
    }
}