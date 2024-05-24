using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;
using static RootMotion.FinalIK.AimPoser;

namespace SAIN.Layers.Combat.Solo
{
    internal class DogFightAction : SAINAction
    {
        public DogFightAction(BotOwner bot) : base(bot, nameof(DogFightAction))
        {
        }

        public override void Update()
        {
            SAINBot.Mover.SetTargetPose(1f);
            SAINBot.Mover.SetTargetMoveSpeed(1f);
            SAINBot.Steering.SteerByPriority(false);
            SAINBot.Mover.DogFight.DogFightMove();
            Shoot.Update();
        }

        private readonly NavMeshPath navMeshPath_0 = new NavMeshPath();

        public override void Start()
        {
            SAINBot.Mover.Sprint(false);
            BotOwner.Mover.SprintPause(0.5f);
        }

        public override void Stop()
        {
            BotOwner.MovementResume();
        }
    }
}