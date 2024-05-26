using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using SAIN.Components;
using UnityEngine;
using SAIN.Helpers;

namespace SAIN.Layers.Combat.Solo
{
    internal class RushEnemyAction : SAINAction
    {
        public RushEnemyAction(BotOwner bot) : base(bot, nameof(RushEnemyAction))
        {
        }

        private float TryJumpTimer;

        public override void Update()
        {
            SAINBot.Mover.SetTargetPose(1f);
            SAINBot.Mover.SetTargetMoveSpeed(1f);
            Shoot.Update();

            if (SAINBot.Enemy == null)
            {
                SAINBot.Steering.SteerByPriority(true);
                return;
            }

            if (SAINBot.Enemy.InLineOfSight)
            {
                if (_shallTryJump)
                {
                    if (_shallBunnyHop)
                    {
                        SAINBot.Mover.TryJump();
                    }
                    else if (TryJumpTimer < Time.time)
                    {
                        TryJumpTimer = Time.time + 3f;
                        if (!_shallBunnyHop
                            && EFTMath.RandomBool(SAINBot.Info.PersonalitySettings.Rush.BunnyHopChance))
                        {
                            _shallBunnyHop = true;
                        }
                        SAINBot.Mover.TryJump();
                    }
                }

                SAINBot.Mover.Sprint(false);
                SAINBot.Mover.SprintController.CancelRun();
                SAINBot.Mover.DogFight.DogFightMove(true);

                if (SAINBot.Enemy.IsVisible && SAINBot.Enemy.CanShoot)
                {
                    SAINBot.Steering.SteerByPriority();
                }
                else
                {
                    SAINBot.Steering.LookToEnemy(SAINBot.Enemy);
                }
                return;
            }

            Vector3[] EnemyPath = SAINBot.Enemy.PathToEnemy.corners;
            Vector3 EnemyPos = SAINBot.Enemy.EnemyPosition;
            if (NewDestTimer < Time.time)
            {
                Vector3 Destination = EnemyPos;
                if (SAINBot.Enemy.Path.PathDistance > 5f
                    && SAINBot.Mover.SprintController.RunToPoint(Destination))
                {
                    NewDestTimer = Time.time + 2f;
                }
                else if (SAINBot.Mover.GoToPoint(Destination, out _))
                {
                    NewDestTimer = Time.time + 1f;
                }
                else
                {
                    NewDestTimer = Time.time + 0.25f;
                }
            }

            if (_shallTryJump && TryJumpTimer < Time.time)
            {
                //&& Bot.Enemy.Path.PathDistance > 3f
                var corner = SAINBot.Enemy?.LastCornerToEnemy;
                if (corner != null)
                {
                    float distance = (corner.Value - BotOwner.Position).magnitude;
                    if (distance < 0.75f)
                    {
                        TryJumpTimer = Time.time + 3f;
                        SAINBot.Mover.TryJump();
                    }
                }
            }
        }

        private bool _shallBunnyHop = false;
        private float NewDestTimer = 0f;
        private Vector3? PushDestination;

        public override void Start()
        {
            _shallTryJump = SAINBot.Info.PersonalitySettings.Rush.CanJumpCorners
                //&& Bot.Decision.CurrentSquadDecision != SquadDecision.PushSuppressedEnemy
                && EFTMath.RandomBool(SAINBot.Info.PersonalitySettings.Rush.JumpCornerChance);

            _shallBunnyHop = false;
        }

        private bool _shallTryJump = false;

        public override void Stop()
        {
            SAINBot.Mover.DogFight.ResetDogFightStatus();
            SAINBot.Mover.SprintController.CancelRun();
        }
    }
}