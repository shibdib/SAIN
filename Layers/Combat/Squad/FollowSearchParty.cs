using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Layers.Combat.Solo;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using UnityEngine;

namespace SAIN.Layers.Combat.Squad
{
    internal class FollowSearchParty : SAINAction
    {
        public FollowSearchParty(BotOwner bot) : base(bot, nameof(FollowSearchParty))
        {
        }

        public override void Update()
        {
            if (!Bot.Mover.SprintController.Running)
            {
                Shoot.Update();
                Bot.Steering.SteerByPriority();
            }

            var leader = Bot.Squad.SquadInfo?.LeaderComponent;
            if (leader != null)
            {
                _leaderPosition = leader.Position;
                if (_nextUpdatePosTime < Time.time)
                {
                    _nextUpdatePosTime = Time.time + 1f;

                    float sqrMag = (_leaderPosition - Bot.Position).sqrMagnitude;
                    if (sqrMag < 5f * 5f)
                    {
                        Bot.Mover.StopMove();
                        return;
                    }
                    if (sqrMag > 30f * 30f)
                    {
                        Bot.Mover.SprintController.RunToPoint(_leaderPosition, SAINComponent.Classes.Mover.ESprintUrgency.Middle);
                    }
                    else if (sqrMag > 10f * 10f)
                    {
                        Bot.Mover.GoToPoint(_leaderPosition, out _, 5f);
                    }

                    if (sqrMag < 20f * 20f && Bot.Mover.SprintController.Running)
                    {
                        Bot.Mover.SprintController.CancelRun();
                        Bot.Mover.GoToPoint(_leaderPosition, out _, 5f);
                    }
                }
            }
        }

        private float _nextUpdatePosTime;
        private Vector3 _leaderPosition;

        public override void Start()
        {
        }

        public override void Stop()
        {
            Bot.Mover.SprintController.CancelRun();
        }
    }
}