using EFT;

namespace SAIN.Layers.Combat.Solo
{
    public class StandAndShootAction : SAINAction
    {
        public StandAndShootAction(BotOwner bot) : base(bot, nameof(StandAndShootAction))
        {
        }

        public override void Update()
        {
            SAINBot.Steering.SteerByPriority();
            SAINBot.Mover.Pose.SetPoseToCover();
            Shoot.Update();
        }

        public override void Start()
        {
            SAINBot.Mover.StopMove();
            SAINBot.Mover.Lean.HoldLean(0.5f);
            BotOwner.Mover.SprintPause(0.5f);
            shallResume = SAINBot.Decision.CurrentSoloDecision == SoloDecision.ShootDistantEnemy;
        }

        bool shallResume = false;

        public override void Stop()
        {
            if (shallResume)
                BotOwner.Mover.MovementResume();
        }
    }
}