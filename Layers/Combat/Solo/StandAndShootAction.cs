using EFT;
using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Solo
{
    public class StandAndShootAction : SAINAction
    {
        public StandAndShootAction(BotOwner bot) : base(bot, nameof(StandAndShootAction))
        {
        }

        public override void Update()
        {
            SAINBot.Steering.SteerByPriority();
            if (!shallMoveShoot)
            {
                SAINBot.Mover.Pose.SetPoseToCover();
            }
            Shoot.Update();
        }

        bool shallMoveShoot = false;

        public override void Start()
        {
            shallMoveShoot = moveShoot();
            if (!shallMoveShoot)
            {
                SAINBot.Mover.StopMove();
                BotOwner.Mover.SprintPause(0.5f);
                shallResume = SAINBot.Decision.CurrentSoloDecision == SoloDecision.ShootDistantEnemy;
            }

            SAINBot.Mover.Lean.HoldLean(0.75f);
        }

        private bool moveShoot()
        {
            if (SAINBot.Enemy != null &&
                SAINBot.Enemy.RealDistance < 50)
            {
                float angle = UnityEngine.Random.Range(70, 110);
                if (EFTMath.RandomBool())
                {
                    angle *= -1;
                }

                Vector3 directionToEnemy = SAINBot.Enemy.EnemyDirection.normalized;
                Vector3 rotated = Vector.Rotate(directionToEnemy, 0, angle, 0);
                rotated.y = 0;
                rotated *= 6f;
                if (NavMesh.SamplePosition(SAINBot.Position + rotated, out var hit, 5f, -1) &&
                    NavMesh.SamplePosition(SAINBot.Position, out var hit2, 0.5f, -1))
                {
                    Vector3 movePos = hit.position;
                    if (NavMesh.Raycast(hit2.position, hit.position, out var rayHit, -1))
                    {
                        movePos = rayHit.position;
                    }
                    return SAINBot.Mover.GoToPoint(movePos, out _, -1, false, false);
                }
            }
            return false;
        }

        bool shallResume = false;

        public override void Stop()
        {
            if (shallResume)
                BotOwner.Mover.MovementResume();
        }
    }
}