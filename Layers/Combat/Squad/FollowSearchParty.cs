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
            if (!SAIN.Mover.SprintController.Running)
            {
                Shoot.Update();

                if (!SAIN.Steering.SteerByPriority(false) 
                    && SAIN.CurrentTargetPosition != null)
                {
                    SAIN.Steering.LookToPoint(SAIN.CurrentTargetPosition.Value);
                }
            }

            var leader = SAIN.Squad.SquadInfo?.LeaderComponent;
            if (leader != null)
            {
                _leaderPosition = leader.Position;
                if (_nextUpdatePosTime < Time.time)
                {
                    _nextUpdatePosTime = Time.time + 1f;

                    float sqrMag = (_leaderPosition - SAIN.Position).sqrMagnitude;
                    if (sqrMag < 5f * 5f)
                    {
                        SAIN.Mover.StopMove();
                        return;
                    }
                    if (sqrMag > 30f * 30f)
                    {
                        SAIN.Mover.SprintController.RunToPoint(_leaderPosition);
                    }
                    else if (sqrMag > 10f * 10f)
                    {
                        SAIN.Mover.GoToPoint(_leaderPosition, out _, 5f);
                    }

                    if (sqrMag < 20f * 20f && SAIN.Mover.SprintController.Running)
                    {
                        SAIN.Mover.SprintController.CancelRun();
                        SAIN.Mover.GoToPoint(_leaderPosition, out _, 5f);
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
            SAIN.Mover.SprintController.CancelRun();
        }
    }
}