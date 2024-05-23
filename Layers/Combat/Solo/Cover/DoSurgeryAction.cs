using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using System.Text;
using UnityEngine;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using UnityEngine.AI;
using SAIN.Helpers;
using System.Collections;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class DoSurgeryAction : SAINAction
    {
        public DoSurgeryAction(BotOwner bot) : base(bot, nameof(DoSurgeryAction))
        {
        }

        public override void Update()
        {
            _allClear = Bot.Medical.Surgery.ShallTrySurgery();
            if (_allClear)
            {
                Bot.Mover.SetTargetMoveSpeed(0f);
                Bot.Mover.SetTargetPose(0f);
                tryStartSurgery();
            }

            Bot.Steering.SteerByPriority();
        }

        private void tryStartSurgery()
        {
            var surgery = BotOwner.Medecine.SurgicalKit;
            if (_startSurgeryTime < Time.time
                && !BotOwner.Mover.IsMoving 
                && !surgery.Using
                && surgery.ShallStartUse())
            {
                Bot.Medical.Surgery.SurgeryStarted = true;
                surgery.ApplyToCurrentPart();
            }
        }

        private bool _allClear;
        

        public override void Start()
        {
            Bot.Mover.StopMove();
            _startSurgeryTime = Time.time + 1f;
        }

        private float _startSurgeryTime;

        public override void Stop()
        {
            Bot.Cover.CheckResetCoverInUse();
            Bot.Medical.Surgery.SurgeryStarted = false;
            BotOwner.MovementResume();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine($"Health Status {Bot.Memory.Health.HealthStatus}");
            stringBuilder.AppendLine($"Surgery Started? {Bot.Medical.Surgery.SurgeryStarted}");
            stringBuilder.AppendLine($"Time Since Surgery Started {Time.time - Bot.Medical.Surgery.SurgeryStartTime}");
            stringBuilder.AppendLine($"Area Clear? {_allClear}");
            stringBuilder.AppendLine($"ShallStartUse Surgery? {BotOwner.Medecine.SurgicalKit.ShallStartUse()}");
            stringBuilder.AppendLine($"IsBleeding? {BotOwner.Medecine.FirstAid.IsBleeding}");
        }
    }
}