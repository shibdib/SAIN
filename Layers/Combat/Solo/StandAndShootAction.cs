﻿using EFT;
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
            Bot.Steering.SteerByPriority();
            if (!shallMoveShoot)
            {
                Bot.Mover.Pose.SetPoseToCover();
            }
            Shoot.Update();
        }

        bool shallMoveShoot = false;

        public override void Start()
        {
            shallMoveShoot = moveShoot();
            if (!shallMoveShoot)
            {
                Bot.Mover.StopMove();
                BotOwner.Mover.SprintPause(0.5f);
                shallResume = Bot.Decision.CurrentSoloDecision == SoloDecision.ShootDistantEnemy;
            }

            Bot.Mover.Lean.HoldLean(0.75f);
        }

        private bool moveShoot()
        {
            if (Bot.Enemy != null &&
                Bot.Enemy.RealDistance < 50)
            {
                float angle = UnityEngine.Random.Range(70, 110);
                if (EFTMath.RandomBool())
                {
                    angle *= -1;
                }

                Vector3 directionToEnemy = Bot.Enemy.EnemyDirection.normalized;
                Vector3 rotated = Vector.Rotate(directionToEnemy, 0, angle, 0);
                rotated.y = 0;
                rotated *= 6f;
                if (NavMesh.SamplePosition(Bot.Position + rotated, out var hit, 5f, -1) &&
                    NavMesh.SamplePosition(Bot.Position, out var hit2, 0.5f, -1))
                {
                    Vector3 movePos = hit.position;
                    if (NavMesh.Raycast(hit2.position, hit.position, out var rayHit, -1))
                    {
                        movePos = rayHit.position;
                    }
                    return Bot.Mover.GoToPoint(movePos, out _, -1, false, false);
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