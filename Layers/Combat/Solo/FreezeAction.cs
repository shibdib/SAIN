using EFT;
using System.Collections;

namespace SAIN.Layers.Combat.Solo
{
    internal class FreezeAction : SAINAction
    {
        public FreezeAction(BotOwner bot) : base(bot, nameof(FreezeAction))
        {
        }
        public void Toggle(bool value)
        {
            ToggleAction(value);
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
            Toggle(true);
            Bot.Mover.StopMove();
        }

        public override void Stop()
        {
            Toggle(false);
        }
    }
}