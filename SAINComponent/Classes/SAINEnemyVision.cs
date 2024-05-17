using EFT;
using SAIN.SAINComponent.SubComponents;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyVision : EnemyBase
    {
        public SAINEnemyVision(SAINEnemy enemy) : base(enemy)
        {
        }

        public void Update(bool isCurrentEnemy)
        {
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

        public void UpdateCanShoot(bool value)
        {
            CanShoot = value;
        }

        public bool InLineOfSight { get; set; }
        public bool IsVisible { get; private set; }
        public bool CanShoot { get; private set; }
        public Vector3? LastSeenPosition { get; set; }
        public float VisibleStartTime { get; private set; }
        public float TimeSinceSeen => Seen ? Time.time - TimeLastSeen : -1f;
        public bool Seen { get; private set; }
        public float TimeFirstSeen { get; private set; }
        public float TimeLastSeen { get; private set; }
        public float LastChangeVisionTime { get; private set; }
    }
}