using EFT;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class DoSurgeryAction : SAINAction
    {
        public DoSurgeryAction(BotOwner bot) : base(bot, nameof(DoSurgeryAction))
        {
        }

        public override void Update()
        {
            if (SAINBot.Medical.Surgery.AreaClearForSurgery)
            {
                SAINBot.Mover.PauseMovement(30);
                SAINBot.Mover.SprintController.CancelRun();
                SAINBot.Mover.SetTargetMoveSpeed(0f);
                SAINBot.Cover.DuckInCover();
                tryStartSurgery();
            }
            else
            {
                BotOwner.Mover.MovementResume();
                SAINBot.Mover.SetTargetMoveSpeed(1);
                SAINBot.Mover.SetTargetPose(1);

                SAINBot.Medical.Surgery.SurgeryStarted = false;
                SAINBot.Medical.TryCancelHeal();
                SAINBot.Mover.DogFight.DogFightMove(false);
            }

            if (!SAINBot.Steering.SteerByPriority(false) &&
                !SAINBot.Steering.LookToLastKnownEnemyPosition(SAINBot.Enemy))
            {
                SAINBot.Steering.LookToRandomPosition();
            }
        }

        private bool tryStartSurgery()
        {
            if (tryStart())
            {
                return true;
            }
            if (checkFullHeal())
            {
                return true;
            }
            return false;
        }

        private bool tryStart()
        {
            var surgery = BotOwner.Medecine.SurgicalKit;
            if (_startSurgeryTime < Time.time
                && !surgery.Using
                && surgery.ShallStartUse())
            {
                SAINBot.Medical.Surgery.SurgeryStarted = true;
                surgery.ApplyToCurrentPart(new System.Action(onSurgeryDone));
                return true;
            }
            return false;
        }

        private bool checkFullHeal()
        {
            if (SAINBot.Medical.Surgery.SurgeryStarted = true &&
                _actionStartedTime + 30f < Time.time)
            {
                SAINBot.Player?.ActiveHealthController?.RestoreFullHealth();
                SAINBot.Decision.ResetDecisions(true);
                return true;
            }
            return false;
        }

        private void onSurgeryDone()
        {
            SAINBot.Medical.Surgery.SurgeryStarted = false;
            _actionStartedTime = Time.time;
            _startSurgeryTime = Time.time + 1f;

            if (BotOwner.Medecine.SurgicalKit.HaveWork)
            {
                if (SAINBot.Enemy == null || SAINBot.Enemy.TimeSinceSeen > 90f)
                {
                    SAINBot.Player?.ActiveHealthController?.RestoreFullHealth();
                    SAINBot.Decision.ResetDecisions(true);
                }
                return;
            }
            SAINBot.Decision.ResetDecisions(true);
        }

        public override void Start()
        {
            SAINBot.Mover.StopMove();
            SAINBot.Mover.PauseMovement(3f);
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
            stringBuilder.AppendLine($"Area Clear? {SAINBot.Medical.Surgery.AreaClearForSurgery}");
            stringBuilder.AppendLine($"ShallStartUse Surgery? {BotOwner.Medecine.SurgicalKit.ShallStartUse()}");
            stringBuilder.AppendLine($"IsBleeding? {BotOwner.Medecine.FirstAid.IsBleeding}");
        }
    }
}