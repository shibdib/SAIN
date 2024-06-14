using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using SAIN.Helpers;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class HoldinCoverAction : SAINAction
    {
        public HoldinCoverAction(BotOwner bot) : base(bot, nameof(HoldinCoverAction))
        {
        }

        private bool Stopped;

        public override void Update()
        {
            if (CoverInUse == null)
            {
                SAINBot.Steering.SteerByPriority();
                Shoot.Update();
                SAINBot.Cover.DuckInCover();
                return;
            }

            if (!Stopped && !CoverInUse.Spotted && (CoverInUse.Position - BotOwner.Position).sqrMagnitude < 0.1f)
            {
                SAINBot.Mover.StopMove();
                Stopped = true;
            }

            SAINBot.Steering.SteerByPriority();
            Shoot.Update();
            SAINBot.Cover.DuckInCover();

            if (SAINBot.Enemy != null 
                && SAINBot.Player.MovementContext.CanProne
                && SAINBot.Player.PoseLevel <= 0.1 
                && SAINBot.Enemy.IsVisible 
                && BotOwner.WeaponManager.Reload.Reloading)
            {
                SAINBot.Mover.Prone.SetProne(true);
            }

            if (SAINBot.Suppression.IsSuppressed)
            {
                ChangeLeanTimer = Time.time + 2f * Random.Range(0.66f, 1.33f);
                SAINBot.Mover.FastLean(LeanSetting.None);
                CurrentLean = LeanSetting.None;
            }
            else
            {
                if (!ShallHoldLean() && ChangeLeanTimer < Time.time)
                {
                    ChangeLeanTimer = Time.time + 2f * Random.Range(0.66f, 1.33f);
                    LeanSetting newLean;
                    switch (CurrentLean)
                    {
                        case LeanSetting.Left:
                        case LeanSetting.Right:
                            newLean = LeanSetting.None;
                            break;

                        default:
                            newLean = EFTMath.RandomBool() ? LeanSetting.Left : LeanSetting.Right;
                            break;
                    }
                    CurrentLean = newLean;
                    SAINBot.Mover.FastLean(newLean);
                }
            }
        }

        private bool ShallHoldLean()
        {
            bool holdLean = false;

            if (SAINBot.Suppression.IsSuppressed)
            {
                return false;
            }

            if (SAINBot.HasEnemy && SAINBot.Enemy.IsVisible && SAINBot.Enemy.CanShoot)
            {
                if (SAINBot.Enemy.IsVisible && SAINBot.Enemy.CanShoot)
                {
                    holdLean = true;
                }
                else if (SAINBot.Enemy.TimeSinceSeen < 3f)
                {
                    holdLean = true;
                }
            }
            return holdLean;
        }

        private void Lean(LeanSetting setting, bool holdLean)
        {
            if (holdLean)
            {
                return;
            }
            CurrentLean = setting;
            SAINBot.Mover.FastLean(setting);
        }

        private LeanSetting CurrentLean;
        private float ChangeLeanTimer;

        private CoverPoint CoverInUse;

        public override void Start()
        {
            ChangeLeanTimer = Time.time + 2f;
            CoverInUse = SAINBot.Cover.CoverInUse;
        }

        public override void Stop()
        {
            SAINBot.Cover.CheckResetCoverInUse();
            SAINBot.Mover.Prone.SetProne(false);
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Hold In Cover Info");
            var cover = SAINBot.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("Current Cover Status", $"{CoverInUse?.Status}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Current Cover Height", $"{CoverInUse?.CoverHeight}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Current Cover Value", $"{CoverInUse?.CoverValue}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);
            if (SAINBot.CurrentTargetPosition != null)
            {
                stringBuilder.AppendLabeledValue("Current Target Position", $"{SAINBot.CurrentTargetPosition.Value}", Color.white, Color.yellow, true);
            }
            else
            {
                stringBuilder.AppendLabeledValue("Current Target Position", null, Color.white, Color.yellow, true);
            }

            if (CoverInUse != null)
            {
                stringBuilder.AppendLine("Cover In Use");
                stringBuilder.AppendLabeledValue("Status", $"{CoverInUse.Status}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{CoverInUse.CoverHeight} {CoverInUse.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{CoverInUse.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(CoverInUse.Position - SAINBot.Position).magnitude}", Color.white, Color.yellow, true);
            }

        }
    }
}