using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using SAIN.Components;
using UnityEngine;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine.AI;
using System.Collections;

namespace SAIN.Layers.Combat.Solo
{
    internal class RushEnemyAction : SAINAction
    {
        public RushEnemyAction(BotOwner bot) : base(bot, nameof(RushEnemyAction))
        {
        }

        private IEnumerator rushEnemy()
        {
            while (true)
            {
                if (Bot == null || !Bot.BotActive)
                {
                    break;
                }

                Bot.Mover.SetTargetPose(1f);
                Bot.Mover.SetTargetMoveSpeed(1f);

                Enemy enemy = Bot.Enemy;
                if (enemy == null)
                {
                    Bot.Steering.SteerByPriority(true);
                    yield return null;
                    continue;
                }

                if (enemy.InLineOfSight)
                {
                    if (_shallTryJump)
                    {
                        if (_shallBunnyHop)
                        {
                            Bot.Mover.TryJump();
                        }
                        else if (TryJumpTimer < Time.time &&
                                Bot.Player.IsSprintEnabled)
                        {
                            TryJumpTimer = Time.time + 3f;
                            if (!_shallBunnyHop
                                && EFTMath.RandomBool(Bot.Info.PersonalitySettings.Rush.BunnyHopChance))
                            {
                                _shallBunnyHop = true;
                            }
                            Bot.Mover.TryJump();
                        }
                    }

                    Shoot.Update();
                    Bot.Mover.Sprint(false);
                    Bot.Mover.SprintController.CancelRun();
                    Bot.Mover.DogFight.DogFightMove(true);

                    if (enemy.IsVisible && enemy.CanShoot)
                    {
                        Bot.Steering.SteerByPriority();
                    }
                    else
                    {
                        Bot.Steering.LookToEnemy(enemy);
                    }
                    yield return null;
                    continue;
                }

                if (_updateMoveTime < Time.time)
                {
                    updateMove(enemy, out float nextTime);
                    _updateMoveTime = Time.time + nextTime;
                }

                if (_shallTryJump && TryJumpTimer < Time.time && Bot.Player.IsSprintEnabled)
                {
                    //&& Bot.Enemy.Path.PathDistance > 3f
                    var corner = Bot.Enemy?.LastCornerToEnemy;
                    if (corner != null)
                    {
                        float distance = (corner.Value - BotOwner.Position).magnitude;
                        if (distance < 0.75f)
                        {
                            TryJumpTimer = Time.time + 3f;
                            Bot.Mover.TryJump();
                        }
                    }
                }

                yield return null;
            }
        }

        private float TryJumpTimer;

        public override void Update()
        {
        }

        private void updateMove(Enemy enemy, out float nextUpdateTime)
        {
            float pathDistance = enemy.EnemyPath.PathDistance;
            var sprintController = Bot.Mover.SprintController;
            if (pathDistance <= 1f && (sprintController.Running || BotOwner.Mover.IsMoving))
            {
                nextUpdateTime = 1f;
                return;
            }

            if (pathDistance > BotOwner.Settings.FileSettings.Move.RUN_TO_COVER_MIN && sprintController.RunToPointByWay(enemy.EnemyPath.PathToEnemy, SAINComponent.Classes.Mover.ESprintUrgency.High))
            {
                nextUpdateTime = 1f;
                return;
            }

            if (sprintController.Running)
            {
                nextUpdateTime = 1f;
                return;
            }

            if (Bot.Mover.GoToPoint(enemy.EnemyPosition, out _, -1, false, false, true))
            {
                nextUpdateTime = 1f;
                return;
            }

            nextUpdateTime = 0.25f;
        }

        private bool _shallBunnyHop = false;
        private float _updateMoveTime = 0f;

        public override void Start()
        {
            _shallTryJump = Bot.Info.PersonalitySettings.Rush.CanJumpCorners
                //&& Bot.Decision.CurrentSquadDecision != SquadDecision.PushSuppressedEnemy
                && EFTMath.RandomBool(Bot.Info.PersonalitySettings.Rush.JumpCornerChance);

            _shallBunnyHop = false;

            _coroutine = Bot.StartCoroutine(rushEnemy());
        }

        private bool _shallTryJump = false;

        public override void Stop()
        {
            Bot.StopCoroutine(_coroutine);
            _coroutine = null;

            Bot.Mover.DogFight.ResetDogFightStatus();
            Bot.Mover.SprintController.CancelRun();
        }

        private Coroutine _coroutine;
    }
}