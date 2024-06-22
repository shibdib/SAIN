using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo
{
    internal class MoveToEngageAction : SAINAction
    {
        public MoveToEngageAction(BotOwner bot) : base(bot, nameof(MoveToEngageAction))
        {
        }

        private float RecalcPathTimer;

        public override void Update()
        {
            Enemy enemy = Bot.Enemy;
            if (enemy == null)
            {
                Bot.Steering.SteerByPriority();
                return;
            }

            Bot.Mover.SetTargetPose(1f);
            Bot.Mover.SetTargetMoveSpeed(1f);

            if (CheckShoot(enemy))
            {
                Bot.Steering.SteerByPriority();
                Shoot.Update();
                return;
            }

            //if (Bot.Decision.SelfActionDecisions.LowOnAmmo(0.66f))
            //{
            //    Bot.SelfActions.TryReload();
            //}

            Vector3? LastSeenPosition = enemy.LastSeenPosition;
            Vector3 movePos;
            if (LastSeenPosition != null)
            {
                movePos = LastSeenPosition.Value;
            }
            else if (enemy.TimeSinceSeen < 5f)
            {
                movePos = enemy.EnemyPosition;
            }
            else
            {
                Bot.Steering.SteerByPriority();
                Shoot.Update();
                return;
            }
            var cover = Bot.Cover.FindPointInDirection(movePos - Bot.Position, 0.5f, 3f);
            if (cover != null)
            {
                movePos = cover.Position;
            }

            float distance = enemy.RealDistance;
            if (distance > 40f && !BotOwner.Memory.IsUnderFire)
            {
                if (RecalcPathTimer < Time.time)
                {
                    RecalcPathTimer = Time.time + 2f;
                    BotOwner.BotRun.Run(movePos, false, SAINPlugin.LoadedPreset.GlobalSettings.General.SprintReachDistance);
                    Bot.Steering.LookToMovingDirection(500f, true);
                }
            }
            else
            {
                Bot.Mover.Sprint(false);

                if (RecalcPathTimer < Time.time)
                {
                    RecalcPathTimer = Time.time + 2f;
                    BotOwner.MoveToEnemyData.TryMoveToEnemy(movePos);
                }

                if (!Bot.Steering.SteerByPriority(false))
                {
                    Bot.Steering.LookToMovingDirection();
                    //SAIN.Steering.LookToPoint(movePos + Vector3.up * 1f);
                }
            }
        }

        private bool CheckShoot(Enemy enemy)
        {
            float distance = enemy.RealDistance;
            bool enemyLookAtMe = enemy.EnemyLookingAtMe;
            float EffDist = Bot.Info.WeaponInfo.EffectiveWeaponDistance;

            if (enemy.IsVisible)
            {
                if (enemyLookAtMe)
                {
                    return true;
                }
                if (distance <= EffDist && enemy.CanShoot)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
        }
    }
}