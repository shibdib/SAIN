using EFT;

namespace SAIN.Layers.Combat.Solo
{
    internal class FreezeAction : SAINAction
    {
        public FreezeAction(BotOwner bot) : base(bot, nameof(FreezeAction))
        {
        }

        public override void Update()
        {
            SAINBot.Mover.SetTargetPose(0f);
            if (!SAINBot.Steering.SteerByPriority(false))
            {
                SAINBot.Steering.LookToLastKnownEnemyPosition();
            }
            Shoot.Update();
        }

        public override void Start()
        {
            SAINBot.Mover.StopMove();
        }

        public override void Stop()
        {
        }
    }
}