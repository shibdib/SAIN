using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class EnemyPlace
    {
        public EnemyPlace(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                TimePositionUpdated = Time.time;
                _position = value;
            }
        }

        private Vector3 _position;

        public float TimePositionUpdated;
        public float TimeSincePositionUpdated => Time.time - TimePositionUpdated;

        public bool HasArrived
        {
            get
            {
                return _hasArrived;
            }
            set
            {
                if (value)
                {
                    TimeArrived = Time.time;
                    HasSeen = true;
                }
                _hasArrived = value;
            }
        }

        private bool _hasArrived;

        public float TimeArrived;

        public bool HasSeen
        {
            get
            {
                return _hasSeen;
            }
            set
            {
                if (value)
                {
                    TimeSeen = Time.time;
                }
                _hasSeen = value;
            }
        }

        private bool _hasSeen;

        public float TimeSeen;

        public bool ClearLineOfSight(Vector3 origin, LayerMask mask)
        {
            if (_nextCheckSightTime < Time.time)
            {
                _nextCheckSightTime = Time.time + 1f;
                Vector3 pos = Position + Vector3.up;
                Vector3 direction = pos - origin;
                _inSight = !Physics.Raycast(pos, direction, direction.magnitude, mask);
                if (_inSight)
                {
                    HasSeen = true;
                }
            }
            return _inSight;
        }

        private bool _inSight;
        private float _nextCheckSightTime;
    }
}