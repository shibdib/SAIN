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
            Bot.Mover.SetTargetMoveSpeed(1f);
            Bot.Mover.SetTargetPose(1f);

            CoverPoint coverInUse = Bot.Cover.CoverInUse;

            if (_nextUpdateCoverTime < Time.time)
            {
                _nextUpdateCoverTime = Time.time + 0.1f;
                if (coverInUse == null)
                {
                    var points = Bot.Cover.CoverPoints;
                    for (int i = 0; i < points.Count; i++)
                    {
                        var coverPoint = points[i];
                        if (coverPoint != null
                            && !coverPoint.Spotted
                            && Bot.Mover.GoToPoint(coverPoint.Position, out _, 0.2f))
                        {
                            coverInUse = coverPoint;
                            Bot.Cover.CoverInUse = coverPoint;
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
                    if (!Bot.Mover.GoToPoint(coverInUse.Position, out _, 0.2f))
                    {
                        Bot.Cover.CoverInUse = null;
                        _nextUpdateCoverTime = -1f;
                    }
                }
            }

            if (coverInUse == null)
            {
                Bot.Mover.DogFight.DogFightMove();
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
            if (Bot.Enemy?.IsVisible == false 
                && BotOwner.WeaponManager.HaveBullets 
                && Bot.Enemy.TimeSinceLastKnownUpdated < 8f 
                && Bot.Enemy.Path.BlindCornerToEnemy != null
                && Bot.Enemy.LastKnownPosition != null
                && (Bot.Enemy.Path.BlindCornerToEnemy.Value - Bot.Enemy.LastKnownPosition.Value).sqrMagnitude < 3f * 3f)
            {
                SuppressPosition(Bot.Enemy.Path.BlindCornerToEnemy.Value);
            }
            else
            {
                Bot.Shoot(false, Vector3.zero);
                Bot.Steering.SteerByPriority(false);
                Shoot.Update();
            }
        }

        private void SuppressPosition(Vector3 position)
        {
            if (SuppressTimer < Time.time
                && Bot.Shoot(true, position, true, Bot.EShootReason.WalkToCoverSuppress))
            {
                Bot.Enemy.EnemyStatus.EnemyIsSuppressed = true;
                if (Bot.Info.WeaponInfo.IWeaponClass == IWeaponClass.machinegun)
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
            Bot.Cover.CheckResetCoverInUse();
            Bot.Shoot(false, Vector3.zero);
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Walk To Cover Info");
            var cover = Bot.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);
            if (Bot.CurrentTargetPosition != null)
            {
                stringBuilder.AppendLabeledValue("Current Target Position", $"{Bot.CurrentTargetPosition.Value}", Color.white, Color.yellow, true);
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
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(CoverDestination.Position - Bot.Position).magnitude}", Color.white, Color.yellow, true);
            }
        }
    }
}