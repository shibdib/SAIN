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
            SAIN.Mover.SetTargetPose(1f);
            SAIN.Mover.SetTargetMoveSpeed(1f);
            Shoot.Update();

            if (SAIN.Enemy == null)
            {
                SAIN.Steering.SteerByPriority(true);
                return;
            }

            if (SAIN.Enemy.InLineOfSight)
            {
                if (SAIN.Info.PersonalitySettings.CanJumpCorners)
                {
                    if (_shallBunnyHop)
                    {
                        SAIN.Mover.TryJump();
                    }
                    else if (TryJumpTimer < Time.time)
                    {
                        TryJumpTimer = Time.time + 5f;
                        if (EFTMath.RandomBool(SAIN.Info.PersonalitySettings.JumpCornerChance))
                        {
                            if (!_shallBunnyHop && EFTMath.RandomBool(5))
                            {
                                _shallBunnyHop = true;
                            }
                            SAIN.Mover.TryJump();
                        }
                    }
                }

                SAIN.Mover.Sprint(false);
                SAIN.Mover.SprintController.Stop();

                SAIN.Mover.DogFight.DogFightMove();
                if (SAIN.Enemy.IsVisible && SAIN.Enemy.CanShoot)
                {
                    SAIN.Steering.SteerByPriority();
                }
                else
                {
                    SAIN.Steering.LookToEnemy(SAIN.Enemy);
                }
                return;
            }

            Vector3[] EnemyPath = SAIN.Enemy.PathToEnemy.corners;
            Vector3 EnemyPos = SAIN.Enemy.EnemyPosition;
            if (NewDestTimer < Time.time)
            {
                Vector3 Destination = EnemyPos;
                if (SAIN.Enemy.Path.PathDistance > 5f 
                    && SAIN.Mover.SprintController.RunToPoint(Destination))
                {
                    NewDestTimer = Time.time + 2f;
                }
                else if (SAIN.Mover.GoToPoint(Destination, out _))
                {
                    NewDestTimer = Time.time + 1f;
                }
                else
                {
                    NewDestTimer = Time.time + 0.25f;
                }
            }

            if (_shallTryJump && TryJumpTimer < Time.time && SAIN.Enemy.Path.PathDistance > 5f)
            {
                var corner = SAIN.Enemy?.LastCornerToEnemy;
                if (corner != null)
                {
                    float distance = (corner.Value - BotOwner.Position).magnitude;
                    if (distance < 0.5f)
                    {
                        TryJumpTimer = Time.time + 3f;
                        if (EFTMath.RandomBool(SAIN.Info.PersonalitySettings.JumpCornerChance))
                        {
                            SAIN.Mover.TryJump();
                        }
                    }
                }
            }

        }

        private bool _shallBunnyHop = false;
        private float NewDestTimer = 0f;
        private Vector3? PushDestination;

        public override void Start()
        {
            _shallTryJump = SAIN.Info.PersonalitySettings.CanJumpCorners 
                && SAIN.Decision.CurrentSquadDecision != SquadDecision.PushSuppressedEnemy
                && EFTMath.RandomBool(SAIN.Info.PersonalitySettings.JumpCornerChance);

            _shallBunnyHop = false;
        }

        bool _shallTryJump = false;

        public override void Stop()
        {
            SAIN.Mover.SprintController.Stop();
        }
    }
}