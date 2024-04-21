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
                return;
            }

            if (!Stopped && !CoverInUse.GetSpotted(SAIN) && (CoverInUse.GetPosition(SAIN) - BotOwner.Position).sqrMagnitude < 0.33f)
            {
                SAIN.Mover.StopMove();
                Stopped = true;
            }

            SAIN.Steering.SteerByPriority();
            Shoot.Update();
            SAIN.Cover.DuckInCover();

            if (SAIN.Suppression.IsSuppressed)
            {
                ChangeLeanTimer = Time.time + 2f * Random.Range(0.66f, 1.33f);
                SAIN.Mover.FastLean(LeanSetting.None);
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
                    SAIN.Mover.FastLean(newLean);
                }
            }
        }

        private bool ShallHoldLean()
        {
            bool holdLean = false;

            if (SAIN.Suppression.IsSuppressed)
            {
                return false;
            }

            if (SAIN.HasEnemy && SAIN.Enemy.IsVisible && SAIN.Enemy.CanShoot)
            {
                if (SAIN.Enemy.IsVisible && SAIN.Enemy.CanShoot)
                {
                    holdLean = true;
                }
                else if (SAIN.Enemy.TimeSinceSeen < 3f)
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
            SAIN.Mover.FastLean(setting);
        }

        private LeanSetting CurrentLean;
        private float ChangeLeanTimer;

        private CoverPoint CoverInUse;

        public override void Start()
        {
            ChangeLeanTimer = Time.time + 2f;
            CoverInUse = SAIN.Cover.CoverInUse;
        }

        public override void Stop()
        {
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            DebugOverlay.AddCoverInfo(SAIN, stringBuilder);
        }
    }
}