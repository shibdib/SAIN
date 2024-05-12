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
using System.Text;
using UnityEngine;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static UnityEngine.UI.GridLayoutGroup;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class WalkToCoverAction : SAINAction
    {
        public WalkToCoverAction(BotOwner bot) : base(bot, nameof(WalkToCoverAction))
        {
        }

        private float _nextCheckSpottedTime;
        private float _nextUpdateCoverTime;
        private float _nextCheckMissingCoverPointTime;

        public override void Update()
        {
            SAIN.Mover.SetTargetMoveSpeed(1f);
            SAIN.Mover.SetTargetPose(1f);

            CoverPoint coverInUse = SAIN.Cover.CoverInUse;

            if (_nextUpdateCoverTime < Time.time)
            {
                _nextUpdateCoverTime = Time.time + 0.1f;
                if (coverInUse == null)
                {
                    var points = SAIN.Cover.CoverPoints;
                    for (int i = 0; i < points.Count; i++)
                    {
                        var coverPoint = points[i];
                        if (coverPoint != null
                            && !coverPoint.Spotted
                            && SAIN.Mover.GoToPoint(coverPoint.Position, out _, 0.2f))
                        {
                            coverInUse = coverPoint;
                            SAIN.Cover.CoverInUse = coverPoint;
                            CoverDestination = coverPoint;
                            RecalcPathTimer = Time.time + 2f;
                            break;
                        }
                    }
                }

                if (coverInUse != null 
                    && RecalcPathTimer < Time.time)
                {
                    RecalcPathTimer = Time.time + 1f;
                    if (!SAIN.Mover.GoToPoint(coverInUse.Position, out _, 0.2f))
                    {
                        SAIN.Cover.CoverInUse = null;
                        _nextUpdateCoverTime = -1f;
                    }
                }
            }

            if (coverInUse == null)
            {
                SAIN.Mover.DogFight.DogFightMove();
            }

            EngageEnemy();
        }

        private float FindTargetCoverTimer = 0f;
        private float RecalcPathTimer = 0f;

        private CoverPoint CoverDestination;
        private Vector3 DestinationPosition;
        private float SuppressTimer;

        private void EngageEnemy()
        {
            if (SAIN.Enemy?.IsVisible == false 
                && BotOwner.WeaponManager.HaveBullets 
                && SAIN.Enemy.Seen && SAIN.Enemy.TimeSinceSeen < 10f 
                && SAIN.Enemy.Path.BlindCornerToEnemy != null)
            {
                SuppressPosition(SAIN.Enemy.Path.BlindCornerToEnemy.Value);
            }
            else
            {
                SAIN.Shoot(false, Vector3.zero);
                SAIN.Steering.SteerByPriority(false);
                Shoot.Update();
            }
        }

        private void SuppressPosition(Vector3 position)
        {
            if (SuppressTimer < Time.time
                && SAIN.Shoot(true, position, true, SAINComponentClass.EShootReason.WalkToCoverSuppress))
            {
                SAIN.Enemy.EnemyIsSuppressed = true;
                if (SAIN.Info.WeaponInfo.IWeaponClass == IWeaponClass.machinegun)
                {
                    SuppressTimer = Time.time + 0.1f * Random.Range(0.75f, 1.25f);
                }
                else
                {
                    SuppressTimer = Time.time + 0.5f * Random.Range(0.66f, 1.33f);
                }
            }
        }

        public override void Start()
        {
            //SAIN.Mover.Sprint(false);
        }

        public override void Stop()
        {
            SAIN.Cover.CheckResetCoverInUse();
            SAIN.Shoot(false, Vector3.zero);
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Walk To Cover Info");
            var cover = SAIN.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);
            if (SAIN.CurrentTargetPosition != null)
            {
                stringBuilder.AppendLabeledValue("Current Target Position", $"{SAIN.CurrentTargetPosition.Value}", Color.white, Color.yellow, true);
            }
            else
            {
                stringBuilder.AppendLabeledValue("Current Target Position", null, Color.white, Color.yellow, true);
            }

            if (CoverDestination != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{CoverDestination.Status}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{CoverDestination.CoverHeight} {CoverDestination.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{CoverDestination.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(CoverDestination.Position - SAIN.Position).magnitude}", Color.white, Color.yellow, true);
            }
        }
    }
}