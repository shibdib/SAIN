using EFT;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class WalkToCoverAction : SAINAction, ISAINAction
    {
        public WalkToCoverAction(BotOwner bot) : base(bot, nameof(WalkToCoverAction))
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override IEnumerator ActionCoroutine()
        {
            while (Active)
            {
                Bot.Mover.SetTargetMoveSpeed(1f);
                Bot.Mover.SetTargetPose(1f);

                if (Bot.Cover.CoverPoints.Count == 0)
                {
                    Bot.Mover.DogFight.DogFightMove(false);
                    EngageEnemy();
                    yield return null;
                    continue;
                }

                if (_nextUpdateCoverTime < Time.time)
                {
                    _nextUpdateCoverTime = Time.time + 0.1f;

                    findCover();
                    reCheckCover();
                }

                if (Bot.Cover.CoverInUse == null)
                {
                    Bot.Mover.DogFight.DogFightMove(false);
                }

                EngageEnemy();
                yield return null;
            }
        }

        private float _nextUpdateCoverTime;

        public override void Update()
        {
        }

        private void findCover()
        {
            CoverPoint coverInUse = Bot.Cover.CoverInUse;
            if (coverInUse == null || coverInUse.IsBad)
            {
                if (shallFallback())
                {
                    RecalcPathTimer = Time.time + 1f;
                    return;
                }

                Bot.Cover.SortPointsByPathDist();

                var points = Bot.Cover.CoverPoints;
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
            return Bot.Decision.CurrentSelfDecision != SelfDecision.None &&
                checkMoveToCover(Bot.Cover.FallBackPoint);
        }

        private void reCheckCover()
        {
            CoverPoint coverInUse = Bot.Cover.CoverInUse;
            if (coverInUse != null
                && RecalcPathTimer < Time.time)
            {
                RecalcPathTimer = Time.time + 1f;
                if (!checkMoveToCover(coverInUse))
                {
                    Bot.Cover.CoverInUse = null;
                    _nextUpdateCoverTime = -1f;
                }
            }
        }

        private bool checkMoveToCover(CoverPoint coverPoint)
        {
            if (coverPoint != null &&
                !coverPoint.Spotted &&
                !coverPoint.IsBad &&
                Bot.Mover.GoToPoint(coverPoint.Position, out _, 0.2f, false, true, true))
            {
                Bot.Cover.CoverInUse = coverPoint;
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
            if (Bot.Enemy?.IsVisible == false
                && BotOwner.WeaponManager.HaveBullets
                && Bot.Enemy.TimeSinceLastKnownUpdated < 30f)
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
                Bot.ManualShoot.TryShoot(false, Vector3.zero);
            }

            if (!Bot.Steering.SteerByPriority(false))
            {
                Bot.Steering.LookToLastKnownEnemyPosition(Bot.Enemy);
            }
            Shoot.Update();
        }

        private bool suppressing;

        private Vector3? findSuppressTarget()
        {
            Vector3? lastKnown = Bot.Enemy?.LastKnownPosition;
            if (lastKnown != null)
            {
                const float maxRange = 10f * 10f;

                Vector3? blindCorner = Bot.Enemy.Path.BlindCornerToEnemy;
                if (blindCorner != null
                    && (blindCorner.Value - lastKnown.Value).sqrMagnitude < maxRange)
                {
                    return blindCorner;
                }

                Vector3? lastCorner = Bot.Enemy.Path.LastCornerToEnemy;
                if (lastCorner != null && Bot.Enemy.Path.CanSeeLastCornerToEnemy
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
            if (_suppressTime < Time.time)
            {
                bool shot = Bot.ManualShoot.TryShoot(true, position, true, EShootReason.WalkToCoverSuppress);
                if (shot)
                {
                    Bot.Enemy.Status.EnemyIsSuppressed = true;
                    if (Bot.Info.WeaponInfo.IWeaponClass == IWeaponClass.machinegun)
                    {
                        _suppressTime = Time.time + 0.05f * Random.Range(0.75f, 1.25f);
                    }
                    else
                    {
                        _suppressTime = Time.time + 0.25f * Random.Range(0.66f, 1.33f);
                    }
                }
            }
        }

        public override void Start()
        {
            Toggle(true);
        }

        public override void Stop()
        {
            Toggle(false);

            Bot.Mover.DogFight.ResetDogFightStatus();
            Bot.Cover.CheckResetCoverInUse();
            if (suppressing)
            {
                suppressing = false;
                Bot.ManualShoot.TryShoot(false, Vector3.zero);
            }
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

            if (_coverDestination != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{_coverDestination.Status}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{_coverDestination.CoverHeight} {_coverDestination.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{_coverDestination.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(_coverDestination.Position - Bot.Position).magnitude}", Color.white, Color.yellow, true);
            }
        }
    }
}