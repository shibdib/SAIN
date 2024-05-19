using EFT;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class EnemyPlace
    {
        public EnemyPlace(Vector3 position, float expireTime, bool isDanger, IPlayer player)
        {
            Position = position;
            ExpireTime = expireTime;
            IsDanger = isDanger;
            Player = player;
        }

        public readonly IPlayer Player;
        public readonly bool IsDanger;

        public bool ShallClear
        {
            get
            {
                if (TimeSincePositionUpdated > ExpireTime)
                {
                    return true;
                }
                if (HasArrivedPersonal && HasSeenPersonal)
                {
                    return true;
                }
                if (HasArrivedSquad && HasSquadSeen)
                {
                    return true;
                }
                return Player?.HealthController?.IsAlive == true;
            }
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

        public bool HasArrivedPersonal
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
                    HasSeenPersonal = true;
                }
                _hasArrived = value;
            }
        }

        public readonly float ExpireTime;

        private bool _hasArrived;

        public float TimeArrived;

        public bool HasArrivedSquad
        {
            get
            {
                return _hasArrivedSquad;
            }
            set
            {
                if (value)
                {
                    TimeSquadArrived = Time.time;
                }
                _hasArrivedSquad = value;
            }
        }

        private bool _hasArrivedSquad;

        public float TimeSquadArrived;

        public bool HasSeenPersonal
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

        public bool HasSquadSeen
        {
            get
            {
                return _hasSquadSeen;
            }
            set
            {
                if (value)
                {
                    TimeSquadSeen = Time.time;
                }
                _hasSquadSeen = value;
            }
        }

        private bool _hasSquadSeen;

        public float TimeSquadSeen;

        public bool PersonalClearLineOfSight(Vector3 origin, LayerMask mask)
        {
            if (_nextCheckSightTime < Time.time)
            {
                _nextCheckSightTime = Time.time + 0.5f;
                Vector3 pos = Position + Vector3.up;
                Vector3 direction = pos - origin;
                _inSightNow = !Physics.Raycast(pos, direction, direction.magnitude, mask);
                if (!HasSeenPersonal && _inSightNow)
                {
                    HasSeenPersonal = true;
                }
            }
            return _inSightNow;
        }

        public bool SquadClearLineOfSight(Vector3 origin, LayerMask mask)
        {
            if (_nextCheckSquadSightTime < Time.time)
            {
                _nextCheckSquadSightTime = Time.time + 0.5f;
                Vector3 pos = Position + Vector3.up;
                Vector3 direction = pos - origin;
                _inSightSquadNow = !Physics.Raycast(pos, direction, direction.magnitude, mask);
                if (!HasSquadSeen && _inSightSquadNow)
                {
                    HasSquadSeen = true;
                }
            }
            return _inSightSquadNow;
        }

        private bool _inSightNow;
        private bool _inSightSquadNow;
        private float _nextCheckSightTime;
        private float _nextCheckSquadSightTime;
    }
}