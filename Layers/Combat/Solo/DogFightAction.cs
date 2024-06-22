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
using System.Collections;

namespace SAIN.Layers.Combat.Solo
{
    internal class DogFightAction : SAINAction
    {
        public DogFightAction(BotOwner bot) : base(bot, nameof(DogFightAction))
        {
        }

        public override void Update()
        {
        }

        private IEnumerator dogFight()
        {
            while (true)
            {
                if (Bot == null || !Bot.BotActive)
                {
                    break;
                }

                Bot.Mover.SetTargetPose(1f);
                Bot.Mover.SetTargetMoveSpeed(1f);
                Bot.Steering.SteerByPriority();
                Bot.Mover.DogFight.DogFightMove(true);
                Shoot.Update();

                yield return null;
            }
        }

        public override void Start()
        {
            Bot.Mover.Sprint(false);
            BotOwner.Mover.SprintPause(0.5f);

            _coroutine = Bot.StartCoroutine(dogFight());
        }

        public override void Stop()
        {
            Bot.StopCoroutine(_coroutine);
            _coroutine = null;

            Bot.Mover.DogFight.ResetDogFightStatus();
            BotOwner.MovementResume();
        }

        private Coroutine _coroutine;
    }
}