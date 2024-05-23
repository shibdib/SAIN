using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using UnityEngine;
using UnityEngine.AI;
using SAIN.SAINComponent.Classes.Enemy;

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
            SAINEnemy enemy = SAINBot.Enemy;
            if (enemy == null)
            {
                SAINBot.Steering.SteerByPriority();
                return;
            }

            SAINBot.Mover.SetTargetPose(1f);
            SAINBot.Mover.SetTargetMoveSpeed(1f);

            if (CheckShoot(enemy))
            {
                SAINBot.Steering.SteerByPriority();
                Shoot.Update();
                return;
            }

            if (SAINBot.Decision.SelfActionDecisions.LowOnAmmo(0.66f))
            {
                SAINBot.SelfActions.TryReload();
            }

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
                SAINBot.Steering.SteerByPriority();
                Shoot.Update();
                return;
            }
            var cover = SAINBot.Cover.FindPointInDirection(movePos - SAINBot.Position, 0.5f, 3f);
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
                    SAINBot.Steering.LookToMovingDirection(500f, true);
                }
            }
            else
            {
                SAINBot.Mover.Sprint(false);

                if (RecalcPathTimer < Time.time)
                {
                    RecalcPathTimer = Time.time + 2f;
                    BotOwner.MoveToEnemyData.TryMoveToEnemy(movePos);
                }

                if (!SAINBot.Steering.SteerByPriority(false))
                {
                    SAINBot.Steering.LookToMovingDirection();
                    //SAIN.Steering.LookToPoint(movePos + Vector3.up * 1f);
                }
            }
        }

        private bool CheckShoot(SAINEnemy enemy)
        {
            float distance = enemy.RealDistance;
            bool enemyLookAtMe = enemy.EnemyLookingAtMe;
            float EffDist = SAINBot.Info.WeaponInfo.EffectiveWeaponDistance;

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