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
                if (findStrafePoint(out targetPos) 
                    && SAINBot.Mover.GoToPoint(targetPos, out _, -1, false, false))
                {
                    _updateDogFightTimer = Time.time + 0.5f;
                }
            }
        }

        private float _updateDogFightTimer;

        private bool findStrafePoint(out Vector3 trgPos)
        {
            if (SAINBot.CurrentTargetPosition == null)
            {
                trgPos = Vector3.zero;
                return false;
            }
            Vector3 direction = SAINBot.CurrentTargetPosition.Value - SAINBot.Position;
            Vector3 a = -Vector.NormalizeFastSelf(direction);
            trgPos = Vector3.zero;
            float num = 0f;
            Vector3 random = Random.onUnitSphere * 1.25f;
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
                dogFightPath.ClearCorners();
                if (NavMesh.CalculatePath(BotOwner.Position, trgPos, -1, dogFightPath) && dogFightPath.status == NavMeshPathStatus.PathComplete)
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
