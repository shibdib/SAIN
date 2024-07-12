using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyPositionTracker : EnemyBase, IBotEnemyClass
    {
        private const float CHECK_MOVE_DIR_FREQ = 1f;
        public Vector3? EnemyWalkDirection { get; private set; }
        public Vector3? EnemySprintDirection { get; private set; }

        public EnemyPositionTracker(Enemy enemy) : base(enemy)
        {
        }

        public void Init()
        {
            Enemy.Events.OnPositionUpdated += positionUpdated;
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            Enemy.Events.OnPositionUpdated -= positionUpdated;
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
            if (known)
            {
                return;
            }
            EnemyWalkDirection = null;
        }

        private void positionUpdated(Enemy enemy, EnemyPlace place)
        {

        }
    }
}