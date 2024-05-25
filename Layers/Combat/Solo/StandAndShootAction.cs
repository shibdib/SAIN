using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.Components;
using UnityEngine;
using UnityEngine.AI;

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

            return;

            if (SAINBot.Cover.BotIsAtCoverInUse())
            {
                return;
            }
            else
            {
                bool prone = SAINBot.Mover.Prone.ShallProne(true);
                SAINBot.Mover.Prone.SetProne(prone);
            }
        }

        public override void Start()
        {
            SAINBot.Mover.StopMove();
            SAINBot.Mover.Lean.HoldLean(0.5f);
            BotOwner.Mover.SprintPause(0.5f);
            _stopMoveTime = Time.time + 0.5f;
        }

        private float _stopMoveTime;

        public override void Stop()
        {
            BotOwner.Mover.MovementResume();
        }
    }
}