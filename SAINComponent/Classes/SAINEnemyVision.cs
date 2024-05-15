using EFT;
using SAIN.SAINComponent.SubComponents;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyVision : EnemyBase
    {
        public SAINEnemyVision(SAINEnemy enemy) : base(enemy)
        {
            SightChecker = SAIN.SightChecker;
        }

        public void Update(bool isCurrentEnemy)
        {
            if (Enemy == null || BotOwner == null || BotOwner.Settings?.Current == null || EnemyPlayer == null)
            {
                return;
            }

            bool visible = false;
            bool canshoot = false;

            var enemyInfo = EnemyInfo;
            if (enemyInfo?.IsVisible == true && InLineOfSight)
            {
                visible = true;
            }
            if (enemyInfo?.CanShoot == true)
            {
                canshoot = true;
            }

            UpdateVisible(visible);
            UpdateCanShoot(canshoot);
        }

        public bool FirstContactOccured { get; private set; }
        public bool ShallReportRepeatContact { get; set; }
        public bool ShallReportLostVisual { get; set; }

        public bool CheckLineOfSight(bool useVisibleDistance, bool simpleCheck)
        {
            if (Enemy == null || BotOwner == null || BotOwner?.Settings?.Current == null || EnemyPlayer == null || SightChecker == null)
            {
                return false;
            }

            bool performanceMode = SAINPlugin.LoadedPreset.GlobalSettings.General.PerformanceMode;
            bool currentEnemy = SAIN.Enemy == Enemy;

            if (_checkLosTime + LOSCheckFreq > Time.time)
            {
                return InLineOfSight;
            }
            _checkLosTime = Time.time;

            float maxDist = float.MaxValue;
            if (useVisibleDistance)
            {
                maxDist = BotOwner.Settings.Current.CurrentVisibleDistance;
            }

            InLineOfSight = false;
            if (Enemy.RealDistance <= maxDist)
            {
                if (simpleCheck)
                {
                    InLineOfSight = SightChecker.SimpleSightCheck(Enemy.EnemyChestPosition, BotOwner.LookSensor._headPoint);
                }
                else
                {
                    InLineOfSight = SightChecker.CheckLineOfSight(EnemyPlayer);
                }
            }
            return InLineOfSight;
        }

        private float LOSCheckFreq
        {
            get
            {
                bool performanceMode = SAINPlugin.LoadedPreset.GlobalSettings.General.PerformanceMode;
                bool currentEnemy = SAIN.Enemy == Enemy;
                bool isAI = Enemy.IsAI;
                float timeAdd;

                // Is the person a human and my current enemy?
                if (!isAI
                    && currentEnemy)
                {
                    timeAdd = 0.05f;
                }
                // Is the person a human but not my current enemy?
                else if (!isAI)
                {
                    timeAdd = 0.15f;
                }
                // Is the person a bot and my current enemy?
                else if (currentEnemy)
                {
                    timeAdd = 0.1f;
                }
                // the person is a bot and not my current active enemy
                else
                {
                    timeAdd = 1f;
                }

                if (SAINPlugin.LoadedPreset.GlobalSettings.General.PerformanceMode)
                {
                    timeAdd *= 2f;
                }
                return timeAdd;
            }
        }

        private SightCheckerComponent SightChecker;

        private const float _repeatContactMinSeenTime = 12f;
        private const float _lostContactMinSeenTime = 12f;

        public void UpdateVisible(bool visible)
        {
            bool wasVisible = IsVisible;
            IsVisible = visible;

            if (IsVisible)
            {
                if (!wasVisible)
                {
                    VisibleStartTime = Time.time;
                }
                if (!wasVisible 
                    && TimeSinceSeen >= _repeatContactMinSeenTime)
                {
                    ShallReportRepeatContact = true;
                }
                if (!Seen)
                {
                    FirstContactOccured = true;
                    TimeFirstSeen = Time.time;
                    Seen = true;
                }
                TimeLastSeen = Time.time;
                LastSeenPosition = EnemyPerson.Position;
                Enemy.UpdateKnownPosition(EnemyPerson.Position, false, true);
            }

            if (!IsVisible)
            {
                if (Seen 
                    && TimeSinceSeen > _lostContactMinSeenTime 
                    && _nextReportLostVisualTime < Time.time)
                {
                    _nextReportLostVisualTime = Time.time + 20f;
                    ShallReportLostVisual = true;
                }
                VisibleStartTime = -1f;
            }

            if (IsVisible != wasVisible)
            {
                LastChangeVisionTime = Time.time;
            }
        }

        private float _nextReportLostVisualTime;

        private void CheckForAimingDelay()
        {

        }

        public void UpdateCanShoot(bool value)
        {
            CanShoot = value;
        }

        public bool InLineOfSight { get; private set; }
        public bool IsVisible { get; private set; }
        public bool CanShoot { get; private set; }
        public Vector3? LastSeenPosition { get; set; }
        public float VisibleStartTime { get; private set; }
        public float TimeSinceSeen => Seen ? Time.time - TimeLastSeen : -1f;
        public bool Seen { get; private set; }
        public float TimeFirstSeen { get; private set; }
        public float TimeLastSeen { get; private set; }
        public float LastChangeVisionTime { get; private set; }

        private float _checkLosTime;
    }
}