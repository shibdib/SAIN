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
            Bot.Mover.SetTargetPose(0f);
            if (!Bot.Steering.SteerByPriority(false))
            {
                Bot.Steering.LookToLastKnownEnemyPosition(Bot.Enemy);
            }
            Shoot.Update();
        }

        public override void Start()
        {
            Bot.Mover.StopMove();
        }

        public override void Stop()
        {
        }
    }
}