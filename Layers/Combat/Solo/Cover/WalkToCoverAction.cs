using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class WalkToCoverAction : SAINAction
    {
        public WalkToCoverAction(BotOwner bot) : base(bot, nameof(WalkToCoverAction))
        {
        }

        private float _nextUpdateCoverTime;

        public override void Update()
        {
            SAINBot.Mover.SetTargetMoveSpeed(1f);
            SAINBot.Mover.SetTargetPose(1f);

            if (SAINBot.Cover.CoverPoints.Count == 0)
            {
                SAINBot.Mover.DogFight.DogFightMove(false);
                EngageEnemy();
                return;
            }

            if (_nextUpdateCoverTime < Time.time)
            {
                _nextUpdateCoverTime = Time.time + 0.1f;

                findCover();
                reCheckCover();
            }

            if (SAINBot.Cover.CoverInUse == null)
            {
                SAINBot.Mover.DogFight.DogFightMove(false);
            }

            EngageEnemy();
        }

        private void findCover()
        {
            CoverPoint coverInUse = SAINBot.Cover.CoverInUse;
            if (coverInUse == null || coverInUse.IsBad)
            {
                if (shallFallback())
                {
                    RecalcPathTimer = Time.time + 1f;
                    return;
                }

                SAINBot.Cover.SortPointsByPathDist();

                var points = SAINBot.Cover.CoverPoints;
                for (int i = 0; i < points.Count; i++)
                {
                    var coverPoint = points[i];
                    if (checkMoveToCover(coverPoint))
                    {
                        RecalcPathTimer = Time.time + 1f;
                        return;
                    }
                }
            }
        }

        private bool shallFallback()
        {
            return SAINBot.Decision.CurrentSelfDecision != SelfDecision.None &&
                checkMoveToCover(SAINBot.Cover.FallBackPoint);
        }

        private void reCheckCover()
        {
            CoverPoint coverInUse = SAINBot.Cover.CoverInUse;
            if (coverInUse != null
                && RecalcPathTimer < Time.time)
            {
                RecalcPathTimer = Time.time + 1f;
                if (!checkMoveToCover(coverInUse))
                {
                    SAINBot.Cover.CoverInUse = null;
                    _nextUpdateCoverTime = -1f;
                }
            }
        }

        private bool checkMoveToCover(CoverPoint coverPoint)
        {
            if (coverPoint != null &&
                !coverPoint.Spotted &&
                !coverPoint.IsBad &&
                SAINBot.Mover.GoToPoint(coverPoint.Position, out _, 0.2f))
            {
                SAINBot.Cover.CoverInUse = coverPoint;
                _coverDestination = coverPoint;
                return true;
            }
            return false;
        }

        private float RecalcPathTimer = 0f;

        private CoverPoint _coverDestination;
        private float _suppressTime;

        private void EngageEnemy()
        {
            if (SAINBot.Enemy?.IsVisible == false
                && BotOwner.WeaponManager.HaveBullets
                && SAINBot.Enemy.TimeSinceLastKnownUpdated < 30f)
            {
                Vector3? suppressTarget = findSuppressTarget();
                if (suppressTarget != null)
                {
                    SuppressPosition(suppressTarget.Value);
                    return;
                }
            }

            if (suppressing && _suppressTime < Time.time)
            {
                suppressing = false;
                SAINBot.ManualShoot.Shoot(false, Vector3.zero);
            }

            if (!SAINBot.Steering.SteerByPriority(false))
            {
                SAINBot.Steering.LookToLastKnownEnemyPosition(SAINBot.Enemy);
            }
            Shoot.Update();
        }

        private bool suppressing;

        private Vector3? findSuppressTarget()
        {
            Vector3? lastKnown = SAINBot.Enemy?.LastKnownPosition;
            if (lastKnown != null)
            {
                float maxRange = 3f.Sqr();

                Vector3? blindCorner = SAINBot.Enemy.Path.BlindCornerToEnemy;
                if (blindCorner != null
                    && (blindCorner.Value - lastKnown.Value).sqrMagnitude < maxRange)
                {
                    return blindCorner;
                }

                Vector3? lastCorner = SAINBot.Enemy.Path.LastCornerToEnemy;
                if (lastCorner != null
                    && (lastCorner.Value - lastKnown.Value).sqrMagnitude < maxRange)
                {
                    return lastCorner;
                }
            }
            return null;
        }

        private void SuppressPosition(Vector3 position)
        {
            suppressing = true;
            if (_suppressTime < Time.time
                && SAINBot.ManualShoot.Shoot(true, position, true, EShootReason.WalkToCoverSuppress))
            {
                SAINBot.Enemy.EnemyStatus.EnemyIsSuppressed = true;
                if (SAINBot.Info.WeaponInfo.IWeaponClass == IWeaponClass.machinegun)
                {
                    _suppressTime = Time.time + 0.1f * Random.Range(0.75f, 1.25f);
                }
                else
                {
                    _suppressTime = Time.time + 0.5f * Random.Range(0.66f, 1.33f);
                }
            }
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
            SAINBot.Mover.DogFight.ResetDogFightStatus();
            SAINBot.Cover.CheckResetCoverInUse();
            if (suppressing)
            {
                suppressing = false;
                SAINBot.ManualShoot.Shoot(false, Vector3.zero);
            }
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Walk To Cover Info");
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

            if (_coverDestination != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{_coverDestination.Status}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{_coverDestination.CoverHeight} {_coverDestination.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{_coverDestination.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(_coverDestination.Position - SAINBot.Position).magnitude}", Color.white, Color.yellow, true);
            }
        }
    }
}