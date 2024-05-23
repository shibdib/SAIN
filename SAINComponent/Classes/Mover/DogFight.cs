using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class DogFight : SAINBase, ISAINClass
    {
        public DogFight(Bot sain) : base(sain)
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

        public void DogFightMove()
        {
            if (_updateDogFightTimer < Time.time)
            {
                Vector3 targetPos = Vector3.zero;
                if (Bot.Enemy != null)
                {
                    BackUp(out targetPos);
                }
                else if (Bot.CurrentTargetPosition != null)
                {
                    BackUpNoEnemy(out targetPos);
                }

                if (targetPos != Vector3.zero 
                    && Bot.Mover.GoToPoint(targetPos, out _ , -1, false, false))
                {
                    _updateDogFightTimer = Time.time + 0.66f;
                }
            }
        }

        private float _updateDogFightTimer;

        private bool BackUp(out Vector3 trgPos)
        {
            Vector3 a = -Vector.NormalizeFastSelf(Bot.Enemy.EnemyDirection);
            trgPos = Vector3.zero;
            float num = 0f;
            Vector3 random = Random.onUnitSphere * 1f;
            random.y = 0f;
            if (NavMesh.SamplePosition(BotOwner.Position + a * 2f / 2f + random, out NavMeshHit navMeshHit, 1f, -1))
            {
                trgPos = navMeshHit.position;
                Vector3 a2 = trgPos - BotOwner.Position;
                float magnitude = a2.magnitude;
                if (magnitude != 0f)
                {
                    Vector3 a3 = a2 / magnitude;
                    num = magnitude;
                    if (NavMesh.SamplePosition(BotOwner.Position + a3 * 2f, out navMeshHit, 1f, -1))
                    {
                        trgPos = navMeshHit.position;
                        num = (trgPos - BotOwner.Position).magnitude;
                    }
                }
            }
            if (num != 0f && num > BotOwner.Settings.FileSettings.Move.REACH_DIST)
            {
                navMeshPath_0.ClearCorners();
                if (NavMesh.CalculatePath(BotOwner.Position, trgPos, -1, navMeshPath_0) && navMeshPath_0.status == NavMeshPathStatus.PathComplete)
                {
                    trgPos = navMeshPath_0.corners[navMeshPath_0.corners.Length - 1];
                    return CheckLength(navMeshPath_0, num);
                }
            }
            return false;
        }

        private bool BackUpNoEnemy(out Vector3 trgPos)
        {
            if (Bot.CurrentTargetPosition == null)
            {
                trgPos = Vector3.zero;
                return false;
            }
            Vector3 direction = Bot.CurrentTargetPosition.Value - Bot.Position;
            Vector3 a = -Vector.NormalizeFastSelf(direction);
            trgPos = Vector3.zero;
            float num = 0f;
            Vector3 random = Random.onUnitSphere * 1f;
            random.y = 0f;
            if (NavMesh.SamplePosition(BotOwner.Position + a * 2f / 2f + random, out NavMeshHit navMeshHit, 1f, -1))
            {
                trgPos = navMeshHit.position;
                Vector3 a2 = trgPos - BotOwner.Position;
                float magnitude = a2.magnitude;
                if (magnitude != 0f)
                {
                    Vector3 a3 = a2 / magnitude;
                    num = magnitude;
                    if (NavMesh.SamplePosition(BotOwner.Position + a3 * 2f, out navMeshHit, 1f, -1))
                    {
                        trgPos = navMeshHit.position;
                        num = (trgPos - BotOwner.Position).magnitude;
                    }
                }
            }
            if (num != 0f && num > BotOwner.Settings.FileSettings.Move.REACH_DIST)
            {
                navMeshPath_0.ClearCorners();
                if (NavMesh.CalculatePath(BotOwner.Position, trgPos, -1, navMeshPath_0) && navMeshPath_0.status == NavMeshPathStatus.PathComplete)
                {
                    trgPos = navMeshPath_0.corners[navMeshPath_0.corners.Length - 1];
                    return CheckLength(navMeshPath_0, num);
                }
            }
            return false;
        }

        private bool CheckLength(NavMeshPath path, float straighDist)
        {
            return path.CalculatePathLength() < straighDist * 1.2f;
        }

        private readonly NavMeshPath navMeshPath_0 = new NavMeshPath();
    }
}
