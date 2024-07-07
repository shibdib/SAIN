using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public struct EnemyCorner
    {
        public EnemyCorner(Vector3 groundPoint, float signedAngle)
        {
            GroundPosition = groundPoint;
            SignedAngleToTarget = signedAngle;
            _nextLookPointTime = 0f;
            _blindCornerLookPoint = groundPoint;
        }

        public Vector3 GroundPosition { get; }
        public float SignedAngleToTarget { get; }

        public Vector3 EyeLevelCorner(Vector3 eyePos, Vector3 botPosition)
        {
            return CornerHelpers.EyeLevelCorner(eyePos, botPosition, GroundPosition);
        }

        public Vector3 PointPastCorner(Vector3 eyePos, Vector3 botPosition)
        {
            if (_nextLookPointTime < Time.time)
            {
                _nextLookPointTime = Time.time + LOOK_POINT_FREQUENCY;
                _blindCornerLookPoint = CornerHelpers.PointPastEyeLevelCorner(eyePos, botPosition, GroundPosition);
            }
            return _blindCornerLookPoint;
        }

        private Vector3 _blindCornerLookPoint;
        private float _nextLookPointTime;
        const float LOOK_POINT_FREQUENCY = 1f / 30f;
    }
}