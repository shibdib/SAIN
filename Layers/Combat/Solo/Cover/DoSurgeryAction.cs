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
            _allClear = SAINBot.Medical.Surgery.ShallTrySurgery();
            if (_allClear)
            {
                SAINBot.Mover.SetTargetMoveSpeed(0f);
                SAINBot.Mover.SetTargetPose(0f);
                tryStartSurgery();
            }
            SAINBot.Steering.SteerByPriority();
        }

        private void tryStartSurgery()
        {
            var surgery = BotOwner.Medecine.SurgicalKit;
            if (_startSurgeryTime < Time.time
                && !BotOwner.Mover.IsMoving 
                && !surgery.Using
                && surgery.ShallStartUse())
            {
                SAINBot.Medical.Surgery.SurgeryStarted = true;
                surgery.ApplyToCurrentPart(new System.Action(onSurgeryDone));
            }
            if (_actionStartedTime + 20f < Time.time)
            {
                SAINBot.Player?.ActiveHealthController?.RestoreFullHealth();
                SAINBot.Decision.ResetDecisions();
            }
        }

        private void onSurgeryDone()
        {
            if (SAINBot.Enemy == null || SAINBot.Enemy.TimeSinceSeen > 60f)
            {
                SAINBot.Player?.ActiveHealthController?.RestoreFullHealth();
            }
            SAINBot.Decision.ResetDecisions();
        }

        private bool _allClear;
        

        public override void Start()
        {
            SAINBot.Mover.StopMove();
            _startSurgeryTime = Time.time + 1f;
            _actionStartedTime = Time.time;
        }

        private float _startSurgeryTime;
        private float _actionStartedTime;

        public override void Stop()
        {
            SAINBot.Cover.CheckResetCoverInUse();
            SAINBot.Medical.Surgery.SurgeryStarted = false;
            BotOwner.MovementResume();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine($"Health Status {SAINBot.Memory.Health.HealthStatus}");
            stringBuilder.AppendLine($"Surgery Started? {SAINBot.Medical.Surgery.SurgeryStarted}");
            stringBuilder.AppendLine($"Time Since Surgery Started {Time.time - SAINBot.Medical.Surgery.SurgeryStartTime}");
            stringBuilder.AppendLine($"Area Clear? {_allClear}");
            stringBuilder.AppendLine($"ShallStartUse Surgery? {BotOwner.Medecine.SurgicalKit.ShallStartUse()}");
            stringBuilder.AppendLine($"IsBleeding? {BotOwner.Medecine.FirstAid.IsBleeding}");
        }
    }
}