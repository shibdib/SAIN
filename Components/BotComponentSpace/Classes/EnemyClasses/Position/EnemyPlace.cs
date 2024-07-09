using EFT;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyPlace
    {
        public event Action<EnemyPlace> OnPositionUpdated;
        public BotComponent Owner { get; }
        public string OwnerID { get; }
        public Enemy Enemy { get; }
        public bool IsDanger { get; }
        public bool EnemyIsAI { get; }

        public bool ShallClear
        {
            get
            {
                var person = Enemy?.EnemyPerson;
                if (person == null)
                {
                    return true;
                }
                var activeClass = person.ActiveClass;
                if (!activeClass.Active || !activeClass.IsAlive)
                {
                    return true;
                }
                if (playerLeftArea)
                {
                    return true;
                }
                return false;
            }
        }

        private bool playerLeftArea
        {
            get
            {
                if (_nextCheckLeaveTime < Time.time)
                {
                    _nextCheckLeaveTime = Time.time + ENEMY_DIST_TO_PLACE_CHECK_FREQ;
                    // If the person this place was created for is AI and left the area, just forget it and move on.
                    float dist = DistanceToEnemyRealPosition;
                    if (EnemyIsAI)
                    {
                        return dist > ENEMY_DIST_TO_PLACE_FOR_LEAVE_AI;
                    }
                    return dist > ENEMY_DIST_TO_PLACE_FOR_LEAVE;
                }
                return false;
            }
        }

        private const float ENEMY_DIST_TO_PLACE_CHECK_FREQ = 10;
        private const float ENEMY_DIST_TO_PLACE_FOR_LEAVE = 150;
        private const float ENEMY_DIST_TO_PLACE_FOR_LEAVE_AI = 100f;
        private const float ENEMY_DIST_UPDATE_FREQ = 0.25f;

        public EnemyPlace(BotComponent owner, Vector3 position, bool isDanger, Enemy enemy)
        {
            Owner = owner;
            OwnerID = owner.ProfileId;
            Enemy = enemy;
            EnemyIsAI = enemy.IsAI;

            _position = position;
            _nextCheckDistTime = 0f;
            _timeLastUpdated = Time.time;
            IsDanger = isDanger;
        }

        public void Update()
        {
            checkUpdateDistance();
        }

        public Vector3 GroundedPosition(float range = 2f)
        {
            Vector3 pos = _position;
            if (Physics.Raycast(pos, Vector3.down, out var hit, range, LayerMaskClass.HighPolyWithTerrainMask))
            {
                return hit.point;
            }
            return pos + (Vector3.down * range);
        }


        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                checkNewValue(value, _position);
                _position = value;
                _timeLastUpdated = Time.time;
                OnPositionUpdated?.Invoke(this);
            }
        }

        private void checkNewValue(Vector3 value, Vector3 oldValue)
        {
            if ((value - oldValue).sqrMagnitude > ENEMY_DIST_RECHECK_MIN_SQRMAG)
                updateDistancesNow(value);
        }

        private const float ENEMY_DIST_RECHECK_MIN_SQRMAG = 0.25f;

        public float TimeSincePositionUpdated => Time.time - _timeLastUpdated;
        public float DistanceToBot { get; private set; }
        public float DistanceToEnemyRealPosition { get; private set; }

        private void checkUpdateDistance()
        {
            if (_nextCheckDistTime <= Time.time)
            {
                updateDistancesNow(_position);
            }
        }

        private void updateDistancesNow(Vector3 position)
        {
            _nextCheckDistTime = Time.time + ENEMY_DIST_UPDATE_FREQ;
            DistanceToBot = (position - Owner.Position).magnitude;
            DistanceToEnemyRealPosition = (position - Enemy.EnemyTransform.Position).magnitude;
        }

        public float Distance(Vector3 point)
        {
            return (_position - point).magnitude;
        }

        public float DistanceSqr(Vector3 toPoint)
        {
            return (_position - toPoint).sqrMagnitude;
        }

        public bool HasArrivedPersonal
        {
            get
            {
                return _hasArrivedPers;
            }
            set
            {
                if (value)
                {
                    _timeArrivedPers = Time.time;
                    HasSeenPersonal = true;
                }
                _hasArrivedPers = value;
            }
        }

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
                    _timeArrivedSquad = Time.time;
                }
                _hasArrivedSquad = value;
            }
        }

        public bool HasSeenPersonal
        {
            get
            {
                return _hasSeenPers;
            }
            set
            {
                if (value)
                {
                    _timeSeenPers = Time.time;
                }
                _hasSeenPers = value;
            }
        }

        public bool HasSeenSquad
        {
            get
            {
                return _hasSquadSeen;
            }
            set
            {
                if (value)
                {
                    _timeSquadSeen = Time.time;
                }
                _hasSquadSeen = value;
            }
        }

        private float _nextCheckDistTime;
        private Vector3 _position;
        private float _nextCheckLeaveTime;
        public float _timeLastUpdated;
        private bool _hasArrivedPers;
        public float _timeArrivedPers;
        private bool _hasArrivedSquad;
        public float _timeArrivedSquad;
        private bool _hasSeenPers;
        public float _timeSeenPers;
        private bool _hasSquadSeen;
        public float _timeSquadSeen;

        public bool PersonalClearLineOfSight(Vector3 origin, LayerMask mask)
        {
            if (_nextCheckSightTime < Time.time)
            {
                _nextCheckSightTime = Time.time + 0.33f;
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
                if (!HasSeenSquad && _inSightSquadNow)
                {
                    HasSeenSquad = true;
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