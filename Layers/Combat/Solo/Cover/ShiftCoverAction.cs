using EFT;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class ShiftCoverAction : SAINAction
    {
        public ShiftCoverAction(BotOwner bot) : base(bot, nameof(ShiftCoverAction))
        {
        }

        public override void Update()
        {
            SAINBot.Steering.SteerByPriority();
            Shoot.Update();
            if (NewPoint == null
                && FindPointToGo())
            {
                SAINBot.Mover.SetTargetMoveSpeed(GetSpeed());
                SAINBot.Mover.SetTargetPose(GetPose());
            }
            else if (NewPoint != null && NewPoint.Status == CoverStatus.InCover)
            {
                SAINBot.Decision.EnemyDecisions.ShiftCoverComplete = true;
            }
            else if (NewPoint != null)
            {
                SAINBot.Mover.SetTargetMoveSpeed(GetSpeed());
                SAINBot.Mover.SetTargetPose(GetPose());
                SAINBot.Mover.GoToPoint(NewPoint.Position, out _);
            }
            else
            {
                SAINBot.Decision.EnemyDecisions.ShiftCoverComplete = true;
            }
        }

        private float GetSpeed()
        {
            var settings = SAINBot.Info.PersonalitySettings;
            return SAINBot.HasEnemy ? settings.Cover.MoveToCoverHasEnemySpeed : settings.Cover.MoveToCoverNoEnemySpeed;
        }

        private float GetPose()
        {
            var settings = SAINBot.Info.PersonalitySettings;
            return SAINBot.HasEnemy ? settings.Cover.MoveToCoverHasEnemyPose : settings.Cover.MoveToCoverNoEnemyPose;
        }

        private bool FindPointToGo()
        {
            if (NewPoint != null)
            {
                return true;
            }

            var coverInUse = SAINBot.Cover.CoverInUse;
            if (coverInUse != null)
            {
                if (NewPoint == null)
                {
                    if (!UsedPoints.Contains(coverInUse))
                    {
                        UsedPoints.Add(coverInUse);
                    }

                    List<CoverPoint> coverPoints = SAINBot.Cover.CoverFinder.CoverPoints;

                    for (int i = 0; i < coverPoints.Count; i++)
                    {
                        CoverPoint shiftCoverTarget = coverPoints[i];

                        if (shiftCoverTarget.CoverHeight > coverInUse.CoverHeight
                            && !UsedPoints.Contains(shiftCoverTarget))
                        {
                            for (int j = 0; j < UsedPoints.Count; j++)
                            {
                                if ((UsedPoints[j].Position - shiftCoverTarget.Position).sqrMagnitude > 5f
                                    && SAINBot.Mover.GoToPoint(shiftCoverTarget.Position, out _))
                                {
                                    SAINBot.Cover.CoverInUse = shiftCoverTarget;
                                    NewPoint = shiftCoverTarget;
                                    return true;
                                }
                            }
                        }
                    }
                }
                if (NewPoint == null)
                {
                    SAINBot.Decision.EnemyDecisions.ShiftCoverComplete = true;
                }
            }
            return false;
        }

        public override void Start()
        {
            SAINBot.Decision.EnemyDecisions.ShiftCoverComplete = false;
        }

        private readonly List<CoverPoint> UsedPoints = new List<CoverPoint>();
        private CoverPoint NewPoint;

        public override void Stop()
        {
            SAINBot.Cover.CheckResetCoverInUse();
            NewPoint = null;
            UsedPoints.Clear();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Shift Cover Info");
            var cover = SAINBot.Cover;
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

            if (NewPoint != null)
            {
                stringBuilder.AppendLine("Cover In Use");
                stringBuilder.AppendLabeledValue("Status", $"{NewPoint.Status}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{NewPoint.CoverHeight} {NewPoint.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{NewPoint.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(NewPoint.Position - SAINBot.Position).magnitude}", Color.white, Color.yellow, true);
            }
        }
    }
}