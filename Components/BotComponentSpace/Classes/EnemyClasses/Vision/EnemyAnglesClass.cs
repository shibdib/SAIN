using SAIN.Helpers;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyAnglesClass : EnemyBase, ISAINEnemyClass
    {
        private const float CALC_ANGLE_FREQ = 1f / 15f;
        private const float CALC_ANGLE_FREQ_AI = 1f / 4f;
        private const float CALC_ANGLE_FREQ_KNOWN = 1f / 30f;
        private const float CALC_ANGLE_FREQ_KNOWN_AI = 1f / 15f;
        private const float CALC_ANGLE_CURRENT_COEF = 0.5f;

        public bool CanBeSeen { get; private set; }
        public float MaxVisionAngle { get; private set; }
        public float AngleToEnemy { get; private set; }
        public float AngleToEnemyHorizontal { get; private set; }
        public float AngleToEnemyHorizontalSigned { get; private set; }
        public float AngleToEnemyVertical { get; private set; }
        public float AngleToEnemyVerticalSigned { get; private set; }

        public EnemyAnglesClass(Enemy enemy) : base(enemy) { }

        public void Init() { }

        public void Update() 
        {
            calcAngles();
        }

        public void Dispose() { }

        public void OnEnemyKnownChanged(bool known, Enemy enemy) { }

        private void calcAngles()
        {
            if (_calcAngleTime < Time.time)
            {
                float delay;
                if (Enemy.IsAI)
                    delay = Enemy.EnemyKnown ? CALC_ANGLE_FREQ_KNOWN_AI : CALC_ANGLE_FREQ_AI;
                else
                    delay = Enemy.EnemyKnown ? CALC_ANGLE_FREQ_KNOWN : CALC_ANGLE_FREQ;

                if (Enemy.IsCurrentEnemy)
                    delay *= CALC_ANGLE_CURRENT_COEF;

                _calcAngleTime = Time.time + delay;

                MaxVisionAngle = Enemy.Bot.Info.FileSettings.Core.VisibleAngle / 2f;

                Vector3 lookDir = Bot.LookDirection;
                Vector3 enemyDirNormal = Enemy.EnemyDirectionNormal;

                AngleToEnemy = Vector3.Angle(enemyDirNormal, lookDir);
                CanBeSeen = AngleToEnemy <= MaxVisionAngle;

                float verticalSigned = calcVerticalAngle(enemyDirNormal, lookDir);
                AngleToEnemyVerticalSigned = verticalSigned;
                AngleToEnemyVertical = Mathf.Abs(verticalSigned);

                float horizSigned = calcHorizontalAngle(enemyDirNormal, lookDir);
                AngleToEnemyHorizontalSigned = horizSigned;
                AngleToEnemyHorizontal = Mathf.Abs(horizSigned);
            }
        }

        private float calcVerticalAngle(Vector3 enemyDirNormal, Vector3 lookDirection)
        {
            Vector3 enemyElevDir = new Vector3(lookDirection.x, enemyDirNormal.y, lookDirection.z);
            float signedAngle = Vector3.SignedAngle(lookDirection, enemyElevDir, Vector3.right);

            if (!EnemyPlayer.IsAI)
                Logger.LogDebug($"elevAngle {signedAngle} Y-Diff {(enemyElevDir.y - lookDirection.y).Round100()}");

            return signedAngle;
        }

        private float calcHorizontalAngle(Vector3 enemyDirNormal, Vector3 lookDirection)
        {
            enemyDirNormal.y = 0;
            lookDirection.y = 0;
            float signedAngle = Vector3.SignedAngle(lookDirection, enemyDirNormal, Vector3.up);

            if (!EnemyPlayer.IsAI)
                Logger.LogDebug($"horizAngle {signedAngle}");

            return signedAngle;
        }

        private float _calcAngleTime;
    }
}