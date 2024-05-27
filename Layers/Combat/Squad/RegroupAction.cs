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
            var SquadLeadPos = SAINBot.Squad.LeaderComponent?.Position;
            if (SquadLeadPos != null)
            {
                SAINBot.Mover.GoToPoint(SquadLeadPos.Value, out _);
                CheckShouldSprint(SquadLeadPos.Value);
            }
            SAINBot.Mover.SetTargetPose(1f);
            SAINBot.Mover.SetTargetMoveSpeed(1f);
            SAINBot.DoorOpener.Update();
        }

        private float UpdateTimer = 0f;

        public override void Start()
        {
        }

        private void CheckShouldSprint(Vector3 pos)
        {
            bool hasEnemy = SAINBot.HasEnemy;
            bool enemyLOS = SAINBot.Enemy?.InLineOfSight == true;
            float leadDist = (pos - BotOwner.Position).magnitude;
            float enemyDist = hasEnemy ? (SAINBot.Enemy.EnemyIPlayer.Position - BotOwner.Position).magnitude : 999f;

            bool sprint = 
                hasEnemy && 
                leadDist > 30f && 
                !enemyLOS && 
                enemyDist > 50f;

            if (SAINBot.Steering.SteerByPriority(false))
            {
                sprint = false;
            }

            if (_nextChangeSprintTime < Time.time)
            {
                _nextChangeSprintTime = Time.time + 1f;
                if (sprint)
                {
                    SAINBot.Mover.Sprint(true);
                }
                else
                {
                    SAINBot.Mover.Sprint(false);
                    SAINBot.Steering.SteerByPriority();
                }
            }
        }

        private float _nextChangeSprintTime;

        public override void Stop()
        {
        }
    }
}