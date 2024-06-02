using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class RunToCoverAction : SAINAction
    {
        public RunToCoverAction(BotOwner bot) : base(bot, nameof(RunToCoverAction))
        {
        }

        private float _jumpTimer;
        private bool _shallJumpToCover;
        private bool _sprinting;
        private float _nextTryReloadTime;

        public override void Update()
        {
            if (BotOwner.WeaponManager.Reload.Reloading ||
                SAINBot.Decision.CurrentSelfDecision == SelfDecision.Reload)
            {
                BotOwner.WeaponManager.Reload.Reload();
            }

            SAINBot.Mover.SetTargetMoveSpeed(1f);
            SAINBot.Mover.SetTargetPose(1f);

            if (SAINBot.Cover.CoverInUse != null)
            {
                // Jump into cover!
                float sqrMag = (SAINBot.Cover.CoverInUse.Position - SAINBot.Position).sqrMagnitude;
                if (_shallJumpToCover
                    && _sprinting
                    && SAINBot.Player.IsSprintEnabled
                    && _moveSuccess
                    && SAINBot.Cover.CoverInUse != null
                    && sqrMag < 3f * 3f
                    && sqrMag > 1.5f * 1.5f
                    && _jumpTimer < Time.time)
                {
                    _jumpTimer = Time.time + 5f;
                    SAINBot.Mover.TryJump();
                }
            }

            if (_recalcMoveTimer < Time.time || 
                (!SAINBot.Mover.SprintController.Running && !BotOwner.Mover.IsMoving))
            {
                _moveSuccess = moveToCover(out bool sprinting, out CoverPoint coverDestination, false);
                if (!_moveSuccess)
                {
                    _moveSuccess = moveToCover(out sprinting, out coverDestination, true);
                }

                _sprinting = sprinting;
                SAINBot.Cover.CoverInUse = coverDestination;

                if (_moveSuccess)
                {
                    _recalcMoveTimer = Time.time + 4f;
                    _shallJumpToCover = EFTMath.RandomBool(2)
                        && _sprinting
                        && BotOwner.Memory.IsUnderFire
                        && SAINBot.Info.Profile.IsPMC;

                    _runDestination = coverDestination.Position;
                }
                else
                {
                    _recalcMoveTimer = Time.time + 0.5f;
                }
            }

            if (_moveSuccess && 
                _sprinting && 
                _nextTryReloadTime < Time.time && 
                SAINBot.Decision.SelfActionDecisions.LowOnAmmo(0.5f))
            {
                _nextTryReloadTime = Time.time + 2f;
                SAINBot.SelfActions.TryReload();
            }
            if (!_moveSuccess)
            {
                SAINBot.Cover.CoverInUse = null;
                SAINBot.Mover.DogFight.DogFightMove(false);
            }

            if (!SAINBot.Mover.SprintController.Running)
            {
                SAINBot.Steering.SteerByPriority();
                Shoot.Update();
            }
        }

        private void jumpToCover()
        {
            if (_shallJumpToCover && 
                _moveSuccess && 
                _sprinting && 
                SAINBot.Player.IsSprintEnabled && 
                _jumpTimer < Time.time)
            {
                CoverPoint coverInUse = SAINBot.Cover.CoverInUse;
                if (coverInUse != null)
                {
                    float sqrMag = (coverInUse.Position - SAINBot.Position).sqrMagnitude;
                    if (sqrMag < 3f * 3f && sqrMag > 1.5f * 1.5f)
                    {
                        _jumpTimer = Time.time + 5f;
                        SAINBot.Mover.TryJump();
                    }
                }
            }
        }

        private Vector3 _runDestination;
        private CoverPoint _coverDestination;

        private bool moveToCover(out bool sprinting, out CoverPoint coverDestination, bool tryWalk)
        {
            if (tryRun(SAINBot.Cover.CoverInUse, out sprinting, tryWalk))
            {
                coverDestination = SAINBot.Cover.CoverInUse;
                return true;
            }

            CoverPoint fallback = SAINBot.Cover.FallBackPoint;
            SoloDecision currentDecision = SAINBot.Decision.CurrentSoloDecision;

            if (currentDecision == SoloDecision.Retreat &&
                fallback != null &&
                tryRun(fallback, out sprinting, tryWalk))
            {
                coverDestination = fallback;
                return true;
            }

            SAINBot.Cover.SortPointsByPathDist();

            sprinting = false;
            var coverPoints = SAINBot.Cover.CoverPoints;

            for (int i = 0; i < coverPoints.Count; i++)
            {
                CoverPoint coverPoint = coverPoints[i];
                if (tryRun(coverPoint, out sprinting, tryWalk))
                {
                    coverDestination = coverPoint;
                    return true;
                }
            }

            if (SAINBot.Decision.CurrentSoloDecision == SoloDecision.AvoidGrenade)
            {
                coverPoints = SAINBot.Cover.CoverFinder.OldCoverPoints;
                for (int i = 0; i < coverPoints.Count; i++)
                {
                    CoverPoint coverPoint = coverPoints[i];

                    if (tryRun(coverPoint, out sprinting, tryWalk))
                    {
                        coverDestination = coverPoint;
                        return true;
                    }
                }
            }

            coverDestination = null;
            return false;
        }

        private bool checkIfPointGoodEnough(CoverPoint coverPoint)
        {
            if (!coverPoint.IsBad)
            {
                return true;
            }
            Vector3 coverPos = coverPoint.Position;
            Vector3 directionToCollider = coverPoint.Collider.transform.position - coverPos;
            Vector3 target = findTarget();
            Vector3 directionToTarget = target - coverPos;
            return Vector3.Dot(directionToCollider.normalized, directionToTarget.normalized) > 0.1f;
        }

        private Vector3 findTarget()
        {
            Vector3 target;
            Vector3? grenade = SAINBot.Grenade.GrenadeDangerPoint;
            if (grenade != null)
            {
                target = grenade.Value;
            }
            else if (SAINBot.CurrentTargetPosition != null)
            {
                target = SAINBot.CurrentTargetPosition.Value;
            }
            else
            {
                target = Vector3.zero;
            }
            return target;
        }

        private bool tryRun(CoverPoint coverPoint, out bool sprinting, bool tryWalk)
        {
            bool result = false;
            sprinting = false;

            if (coverPoint == null)
            {
                return false;
            }

            Vector3 destination = coverPoint.Position;

            Vector3? grenadePos = SAINBot.Grenade.GrenadeDangerPoint;
            if (grenadePos != null &&
                (grenadePos.Value - destination).sqrMagnitude < 3f.Sqr())
            {
                return false;
            }

            float distance = coverPoint.PathLength;

            if (coverPoint.IsBad &&
                distance > 8f)
            {
                return false;
            }

            // Testing new pathfinder for running
            if (!tryWalk &&
                distance >= SAINBot.Info.FileSettings.Move.RUN_TO_COVER_MIN)
            {
                //  && shallRun(destination)
                bool isUrgent =
                    BotOwner.Memory.IsUnderFire ||
                    SAINBot.Suppression.IsSuppressed ||
                    SAINBot.Decision.CurrentSelfDecision != SelfDecision.None;

                ESprintUrgency urgency = isUrgent ? ESprintUrgency.High : ESprintUrgency.Middle;
                if (SAINBot.Mover.SprintController.RunToPoint(destination, urgency))
                {
                    sprinting = true;
                    return true;
                }
            }

            if (tryWalk)
            {
                bool shallCrawl = SAINBot.Decision.CurrentSelfDecision != SelfDecision.None
                    && coverPoint.Status == CoverStatus.FarFromCover
                    && SAINBot.Mover.Prone.ShallProneHide();

                //result = SAIN.Mover.GoToPoint(destination, out _, -1, shallCrawl, true);
                result = SAINBot.Mover.GoToPoint(destination, out _, 0.5f, shallCrawl, false, true);
            }
            return result;
        }

        private bool _moveSuccess;
        private float _recalcMoveTimer;

        private void EngageEnemy()
        {
            SAINBot.Steering.SteerByPriority();
            Shoot.Update();
        }

        public override void Start()
        {
            if (SAINBot.Decision.CurrentSoloDecision == SoloDecision.AvoidGrenade
                && SAINBot.Talk.GroupTalk.FriendIsClose)
            {
                SAINBot.Talk.TalkAfterDelay(EPhraseTrigger.OnEnemyGrenade, ETagStatus.Combat, 0.33f);
            }

            _recalcMoveTimer = 0f;
            _shallJumpToCover = false;
            _sprinting = false;
            _moveSuccess = false;
        }

        public override void Stop()
        {
            SAINBot.Mover.DogFight.ResetDogFightStatus();
            SAINBot.Mover.SprintController.CancelRun();
            SAINBot.Cover.CheckResetCoverInUse();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Run To Cover Info");

            var sprint = SAINBot.Mover.SprintController;
            if (sprint.Running)
            {
                stringBuilder.AppendLabeledValue("Running Status", $"{sprint.CurrentRunStatus}", Color.white, Color.yellow, true);
            }

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

            var _coverDestination = SAINBot.Cover.CoverInUse;
            if (_coverDestination != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{_coverDestination.Status}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{_coverDestination.CoverHeight} {_coverDestination.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{_coverDestination.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(_coverDestination.Position - SAINBot.Position).magnitude}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Safe Path?", $"{_coverDestination.IsSafePath}", Color.white, Color.yellow, true);
            }
        }
    }
}