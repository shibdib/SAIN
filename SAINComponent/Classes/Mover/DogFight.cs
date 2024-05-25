using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;
using SAIN.SAINComponent.Classes.Enemy;

namespace SAIN.SAINComponent.Classes.Mover
{
    public enum EDogFightStatus
    {
        None = 0,
        BackingUp = 1,
        MovingToEnemy = 2,
        Shooting = 3,
    }

    public class DogFight : SAINBase, ISAINClass
    {
        public EDogFightStatus Status { get; private set; }
        public DogFight(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public void ResetDogFightStatus()
        {
            if (Status != EDogFightStatus.None)
                Status = EDogFightStatus.None;
        }

        public void DogFightMove(bool aggressive)
        {
            if (stopMoveToShoot())
            {
                Status = EDogFightStatus.Shooting;
                SAINBot.Mover.StopMove(0f);
                float timeAdd = 0.33f * UnityEngine.Random.Range(0.5f, 1.33f);
                SAINBot.Mover.Lean.HoldLean(timeAdd);
                _updateDogFightTimer = Time.time + timeAdd;
                return;
            }

            if (_updateDogFightTimer < Time.time)
            {
                if (backUpFromEnemy())
                {
                    Status = EDogFightStatus.BackingUp;
                    float baseTime = SAINBot.Enemy?.IsVisible == true ? 0.5f : 0.75f;
                    _updateDogFightTimer = Time.time + baseTime * UnityEngine.Random.Range(0.66f, 1.33f);
                    return;
                }

                if (!aggressive)
                {
                    _updateDogFightTimer = Time.time + 0.5f;
                    return;
                }

                if (moveToEnemy())
                {
                    Status = EDogFightStatus.MovingToEnemy;
                    float timeAdd = Mathf.Clamp(0.1f * UnityEngine.Random.Range(0.5f, 1.25f), 0.05f, 0.66f);
                    _updateDogFightTimer = Time.time + timeAdd;
                    return;
                }
                _updateDogFightTimer = Time.time + 0.2f;
            }
        }

        private bool stopMoveToShoot()
        {
            SAINEnemy enemy = SAINBot.Enemy;
            return Status == EDogFightStatus.MovingToEnemy && enemy?.IsVisible == true && enemy.CanShoot;
        }

        private bool moveToEnemy()
        {
            Vector3? target = findMoveToEnemyTarget();
            return target != null &&
                SAINBot.Mover.GoToPoint(target.Value, out _, -1, false, false);
        }

        private bool backUpFromEnemy()
        {
            return 
                findStrafePoint(out Vector3 backupPoint) &&
                SAINBot.Mover.GoToPoint(backupPoint, out _, -1, false, false);
        }

        private float _updateDogFightTimer;

        private Vector3? findBackupTarget()
        {
            SAINEnemy enemy = SAINBot.Enemy;
            if (enemy != null && 
                enemy.Seen && 
                enemy.TimeSinceSeen < _enemyTimeSinceSeenThreshold)
            {
                return SAINBot.Enemy.EnemyPosition;
            }
            return null;
        }

        private Vector3? findMoveToEnemyTarget()
        {
            SAINEnemy enemy = SAINBot.Enemy;
            if (enemy != null &&
                enemy.Seen &&
                enemy.TimeSinceSeen >= _enemyTimeSinceSeenThreshold &&
                enemy.PathToEnemy.status != NavMeshPathStatus.PathInvalid)
            {
                return SAINBot.Enemy.LastKnownPosition;
            }
            return null;
        }

        private float _enemyTimeSinceSeenThreshold = 1f;

        private bool findStrafePoint(out Vector3 trgPos)
        {
            Vector3? target = findBackupTarget();
            if (target == null)
            {
                trgPos = Vector3.zero;
                return false;
            }
            Vector3 direction = target.Value - SAINBot.Position;

            Vector3 a = -Vector.NormalizeFastSelf(direction);
            trgPos = Vector3.zero;
            float num = 0f;
            Vector3 random = Random.onUnitSphere * 1.25f;
            random.y = 0f;
            if (NavMesh.SamplePosition(SAINBot.Position + a * 2f / 2f + random, out NavMeshHit navMeshHit, 1f, -1))
            {
                trgPos = navMeshHit.position;
                Vector3 a2 = trgPos - SAINBot.Position;
                float magnitude = a2.magnitude;
                if (magnitude != 0f)
                {
                    Vector3 a3 = a2 / magnitude;
                    num = magnitude;
                    if (NavMesh.SamplePosition(SAINBot.Position + a3 * 2f, out navMeshHit, 1f, -1))
                    {
                        trgPos = navMeshHit.position;
                        num = (trgPos - SAINBot.Position).magnitude;
                    }
                }
            }
            if (num != 0f && num > BotOwner.Settings.FileSettings.Move.REACH_DIST)
            {
                dogFightPath.ClearCorners();
                if (NavMesh.CalculatePath(SAINBot.Position, trgPos, -1, dogFightPath) && dogFightPath.status == NavMeshPathStatus.PathComplete)
                {
                    trgPos = dogFightPath.corners[dogFightPath.corners.Length - 1];
                    return CheckLength(dogFightPath, num);
                }
            }
            return false;
        }

        private bool CheckLength(NavMeshPath path, float straighDist)
        {
            return path.CalculatePathLength() < straighDist * 1.5f;
        }

        private readonly NavMeshPath dogFightPath = new NavMeshPath();
    }
}
