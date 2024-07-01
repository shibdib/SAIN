using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyVision : EnemyBase, ISAINEnemyClass
    {
        public event Action<Enemy, bool> OnVisionChange;
        public event Action<Enemy> OnFirstSeen;

        public SAINEnemyVision(Enemy enemy) : base(enemy)
        {
            _gainSight = new GainSightClass(enemy);
            _visionDistance = new EnemyVisionDistanceClass(enemy);
            EnemyVisionChecker = new EnemyVisionChecker(enemy);
        }

        public void Init()
        {
            Enemy.EnemyKnownChecker.OnEnemyKnownChanged += OnEnemyKnownChanged;
        }

        public void OnEnemyKnownChanged(Enemy enemy, bool known)
        {
            if (known)
            {
                return;
            }
            UpdateVisibleState(true);
            UpdateCanShootState(true);
        }

        public void Dispose()
        {
            Enemy.EnemyKnownChecker.OnEnemyKnownChanged -= OnEnemyKnownChanged;
        }

        public void Update()
        {
            getAngles();
            UpdateVisibleState(false);
            UpdateCanShootState(false);
        }

        public float EnemyVelocity => EnemyTransform.PlayerVelocity;

        public bool FirstContactOccured { get; private set; }

        public bool ShallReportRepeatContact { get; set; }

        public bool ShallReportLostVisual { get; set; }

        private const float _repeatContactMinSeenTime = 12f;

        private const float _lostContactMinSeenTime = 12f;

        private bool isEnemyInVisibleSector()
        {
            return AngleToEnemy <= MaxVisionAngle;
        }

        private float getAngleToEnemy(bool setYto0)
        {
            Vector3 direction = EnemyPosition - Enemy.Bot.Position;
            Vector3 lookDir = Bot.LookDirection;
            if (setYto0)
            {
                direction.y = 0;
                lookDir.y = 0;
            }
            return Vector3.Angle(direction, lookDir);
        }

        public float MaxVisionAngle { get; private set; }
        public float AngleToEnemy { get; private set; }
        public float AngleToEnemyHorizontal { get; private set; }

        private void getAngles()
        {
            if (_calcAngleTime < Time.time)
            {
                MaxVisionAngle = Enemy.Bot.Info.FileSettings.Core.VisibleAngle / 2f;
                _calcAngleTime = Time.time + _calcAngleFreq;
                AngleToEnemy = getAngleToEnemy(false);
                AngleToEnemyHorizontal = getAngleToEnemy(true);
            }
        }

        private float _calcAngleTime;
        private float _calcAngleFreq = 1f / ANGLE_CALC_PERSECOND;
        private const float ANGLE_CALC_PERSECOND = 30f;

        public void UpdateVisibleState(bool forceOff)
        {
            bool wasVisible = IsVisible;
            bool lineOfSight = InLineOfSight;

            if (forceOff)
            {
                IsVisible = false;
            }
            else
            {
                IsVisible =
                    EnemyInfo.IsVisible &&
                    isEnemyInVisibleSector();
            }

            if (IsVisible)
            {
                if (!wasVisible)
                {
                    VisibleStartTime = Time.time;
                    if (Seen && TimeSinceSeen >= _repeatContactMinSeenTime)
                    {
                        ShallReportRepeatContact = true;
                    }
                }
                if (!Seen)
                {
                    FirstContactOccured = true;
                    TimeFirstSeen = Time.time;
                    Seen = true;
                    OnFirstSeen?.Invoke(Enemy);
                }

                TimeLastSeen = Time.time;
                Enemy.UpdateCurrentEnemyPos(EnemyTransform.Position);
            }

            if (!IsVisible)
            {
                if (wasVisible)
                {
                    Enemy.UpdateLastSeenPosition(EnemyTransform.Position);
                }
                if (Seen && 
                    TimeSinceSeen > _lostContactMinSeenTime && 
                    _nextReportLostVisualTime < Time.time)
                {
                    _nextReportLostVisualTime = Time.time + 20f;
                    ShallReportLostVisual = true;
                }
                VisibleStartTime = -1f;
            }

            if (IsVisible != wasVisible)
            {
                OnVisionChange?.Invoke(Enemy, IsVisible);
                LastChangeVisionTime = Time.time;
            }
        }

        private float _nextReportLostVisualTime;

        public void UpdateCanShootState(bool forceOff)
        {
            if (forceOff)
            {
                CanShoot = false;
                return;
            }
            CanShoot = EnemyInfo?.CanShoot == true;
        }

        public bool InLineOfSight => EnemyVisionChecker.LineOfSight;
        public bool IsVisible { get; private set; }
        public bool CanShoot { get; private set; }
        public Vector3? LastSeenPosition { get; set; }
        public float VisibleStartTime { get; private set; }
        public float TimeSinceSeen => Seen ? Time.time - TimeLastSeen : -1f;
        public bool Seen { get; private set; }
        public float TimeFirstSeen { get; private set; }
        public float TimeLastSeen { get; private set; }
        public float LastChangeVisionTime { get; private set; }
        public float LastGainSightResult { get; set; }
        public float GainSightCoef => _gainSight.Value;
        public float VisionDistance => _visionDistance.Value;

        private readonly GainSightClass _gainSight;
        private readonly EnemyVisionDistanceClass _visionDistance;
        public EnemyVisionChecker EnemyVisionChecker { get; private set; }
    }
}