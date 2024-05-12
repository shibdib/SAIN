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
            SAIN.Mover.SetTargetMoveSpeed(0f);
            SAIN.Mover.SetTargetPose(0f);

            _allClear = SAIN.Medical.Surgery.ShallTrySurgery();
            if (_allClear)
            {
                tryStartSurgery();
            }

            SAIN.Steering.SteerByPriority();
        }

        private void tryStartSurgery()
        {
            var surgery = BotOwner.Medecine.SurgicalKit;
            if (_startSurgeryTime < Time.time
                && !BotOwner.Mover.IsMoving 
                && !surgery.Using
                && surgery.ShallStartUse())
            {
                SAIN.Medical.Surgery.SurgeryStarted = true;
                surgery.ApplyToCurrentPart();
            }
        }

        private bool _allClear;
        

        public override void Start()
        {
            SAIN.Mover.StopMove();
            _startSurgeryTime = Time.time + 1f;
        }

        private float _startSurgeryTime;

        public override void Stop()
        {
            SAIN.Cover.CheckResetCoverInUse();
            SAIN.Medical.Surgery.SurgeryStarted = false;
            BotOwner.MovementResume();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine($"Health Status {SAIN.Memory.HealthStatus}");
            stringBuilder.AppendLine($"Surgery Started? {SAIN.Medical.Surgery.SurgeryStarted}");
            stringBuilder.AppendLine($"Time Since Surgery Started {Time.time - SAIN.Medical.Surgery.SurgeryStartTime}");
            stringBuilder.AppendLine($"Area Clear? {_allClear}");
            stringBuilder.AppendLine($"ShallStartUse Surgery? {BotOwner.Medecine.SurgicalKit.ShallStartUse()}");
            stringBuilder.AppendLine($"IsBleeding? {BotOwner.Medecine.FirstAid.IsBleeding}");
        }
    }
}