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
            if (!SAINBot.Mover.SprintController.Running)
            {
                Shoot.Update();
                SAINBot.Steering.SteerByPriority();
            }

            var leader = SAINBot.Squad.SquadInfo?.LeaderComponent;
            if (leader != null)
            {
                _leaderPosition = leader.Position;
                if (_nextUpdatePosTime < Time.time)
                {
                    _nextUpdatePosTime = Time.time + 1f;

                    float sqrMag = (_leaderPosition - SAINBot.Position).sqrMagnitude;
                    if (sqrMag < 5f * 5f)
                    {
                        SAINBot.Mover.StopMove();
                        return;
                    }
                    if (sqrMag > 30f * 30f)
                    {
                        SAINBot.Mover.SprintController.RunToPoint(_leaderPosition, SAINComponent.Classes.Mover.ESprintUrgency.Middle);
                    }
                    else if (sqrMag > 10f * 10f)
                    {
                        SAINBot.Mover.GoToPoint(_leaderPosition, out _, 5f);
                    }

                    if (sqrMag < 20f * 20f && SAINBot.Mover.SprintController.Running)
                    {
                        SAINBot.Mover.SprintController.CancelRun();
                        SAINBot.Mover.GoToPoint(_leaderPosition, out _, 5f);
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
            SAINBot.Mover.SprintController.CancelRun();
        }
    }
}