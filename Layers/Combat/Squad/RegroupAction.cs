using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Layers.Combat.Solo;
using SAIN.SAINComponent;
using UnityEngine;

namespace SAIN.Layers.Combat.Squad
{
    internal class RegroupAction : SAINAction
    {
        public RegroupAction(BotOwner bot) : base(bot, nameof(RegroupAction))
        {
        }

        public override void Update()
        {
            if (Bot.Steering.SteerByPriority())
            {
                Bot.Mover.Sprint(false);
            }
            else
            {
                Regroup();
            }
        }

        private float UpdateTimer = 0f;

        private void Regroup()
        {
            if (UpdateTimer < Time.time)
            {
                UpdateTimer = Time.time + 3f;
                MoveToLead();
            }
        }

        public override void Start()
        {
            MoveToLead();
        }

        private void MoveToLead()
        {
            var SquadLeadPos = Bot.Squad.LeaderComponent?.BotOwner?.Position;
            if (SquadLeadPos != null && Bot.Mover.GoToPoint(SquadLeadPos.Value, out _))
            {
                Bot.Mover.SetTargetPose(1f);
                Bot.Mover.SetTargetMoveSpeed(1f);
                CheckShouldSprint(SquadLeadPos.Value);
                BotOwner.DoorOpener.Update();
            }
        }

        private void CheckShouldSprint(Vector3 pos)
        {
            bool hasEnemy = Bot.HasEnemy;
            bool enemyLOS = Bot.Enemy?.InLineOfSight == true;
            float leadDist = (pos - BotOwner.Position).magnitude;
            float enemyDist = hasEnemy ? (Bot.Enemy.EnemyIPlayer.Position - BotOwner.Position).magnitude : 999f;

            bool sprint = hasEnemy && leadDist > 30f && !enemyLOS && enemyDist > 50f;

            if (Bot.Steering.SteerByPriority(false))
            {
                sprint = false;
            }

            if (_nextChangeSprintTime < Time.time)
            {
                _nextChangeSprintTime = Time.time + 1f;
                if (sprint)
                {
                    Bot.Mover.Sprint(true);
                }
                else
                {
                    Bot.Mover.Sprint(false);
                    Bot.Steering.SteerByPriority();
                }
            }
        }

        private float _nextChangeSprintTime;

        public override void Stop()
        {
        }
    }
}