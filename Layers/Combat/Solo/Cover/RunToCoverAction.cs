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
            SAIN.Mover.SetTargetMoveSpeed(1f);
            SAIN.Mover.SetTargetPose(1f);

            if (SAIN.Cover.CoverInUse != null)
            {
                // Jump into cover!
                float sqrMag = (SAIN.Cover.CoverInUse.Position - SAIN.Position).sqrMagnitude;
                if (_shallJumpToCover
                    && _sprinting
                    && SAIN.Player.IsSprintEnabled
                    && _moveSuccess
                    && SAIN.Cover.CoverInUse != null
                    && sqrMag < SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN * SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN * 1.2f
                    && _jumpTimer < Time.time)
                {
                    _jumpTimer = Time.time + 5f;
                    SAIN.Mover.TryJump();
                }
                // Stop sprinting if close enough so you can navigate properly
                else if (_moveSuccess
                    && (_sprinting || SAIN.Player.IsSprintEnabled)
                    && sqrMag < SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN * SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN)
                {
                    SAIN.Mover.Sprint(false);
                    _sprinting = false;
                }
            }

            if (!BotOwner.Mover.IsMoving)
            {
                //_coverDestination = null;
            }

            if (_recalcMoveTimer < Time.time)
            {
                _moveSuccess = moveToCover(out bool sprinting, out CoverPoint coverDestination);
                _sprinting = sprinting;
                SAIN.Cover.CoverInUse = coverDestination;

                if (_moveSuccess)
                {
                    _recalcMoveTimer = Time.time + 2f;
                    _shallJumpToCover = EFTMath.RandomBool(8) 
                        && _sprinting
                        && BotOwner.Memory.IsUnderFire
                        && SAIN.Info.Profile.IsPMC;

                    if (_sprinting 
                        && _nextTryReloadTime < Time.time 
                        && SAIN.Decision.SelfActionDecisions.LowOnAmmo(0.5f))
                    {
                        _nextTryReloadTime = Time.time + 2f;
                        SAIN.SelfActions.TryReload();
                    }

                    _runDestination = coverDestination.Position;
                }
                else
                {
                    _recalcMoveTimer = Time.time + 0.25f;
                }
            }

            if (!_moveSuccess)
            {
                SAIN.Cover.CoverInUse = null;
                SAIN.Mover.DogFight.DogFightMove();
            }

            SAIN.Steering.SteerByPriority();
            Shoot.Update();
        }

        private Vector3 _runDestination;
        private CoverPoint _coverDestination;

        private bool moveToCover(out bool sprinting, out CoverPoint coverDestination)
        {
            CoverPoint coverInUse = SAIN.Cover.CoverInUse;
            if (coverInUse != null)
            {
                if (_coverDestination != null && _coverDestination == coverInUse)
                {
                    coverDestination = _coverDestination;
                    sprinting = _sprinting;
                    return true;
                }
                if (tryRun(coverInUse, out sprinting))
                {
                    coverDestination = coverInUse;
                    return true;
                }
            }

            CoverPoint fallback = SAIN.Cover.FallBackPoint;
            SoloDecision currentDecision = SAIN.Memory.Decisions.Main.Current;

            if (currentDecision == SoloDecision.Retreat
                && fallback != null
                && fallback.IsSafePath)
            {
                if (tryRun(fallback, out sprinting))
                {
                    coverDestination = fallback;
                    return true;
                }
            }

            SAIN.Cover.SortPointsByPathDist();

            sprinting = false;
            var coverPoints = SAIN.Cover.CoverPoints;

            for (int i = 0; i < coverPoints.Count; i++)
            {
                CoverPoint coverPoint = coverPoints[i];

                if (tryRun(coverPoint, out sprinting))
                {
                    coverDestination = coverPoint;
                    return true;
                }
            }

            coverDestination = null;
            return false;
        }

        private bool tryRun(CoverPoint coverPoint, out bool sprinting)
        {
            bool result = false;
            sprinting = false;
            Vector3 destination = coverPoint.Position;

            // Testing new pathfinder for running
            if (shallRun(destination))
            {
                result = SAIN.Mover.SprintController.RunToPoint(destination);
                //result = BotOwner.BotRun.Run(destination, false, 0.25f);
                if (result)
                {
                    sprinting = true;
                }
            }

            if (!result)
            {
                bool shallProne = SAIN.Mover.Prone.ShallProneHide();
                bool shallCrawl = SAIN.Decision.CurrentSelfDecision != SelfDecision.None
                    && coverPoint.Status == CoverStatus.FarFromCover
                    && shallProne;

                //result = SAIN.Mover.GoToPoint(destination, out _, -1, shallCrawl, true);
                result = SAIN.Mover.GoToPoint(destination, out _, 0.25f, shallCrawl);
            }
            return result;
        }

        private bool shallRun(Vector3 destination) => (destination - SAIN.Position).sqrMagnitude > SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN * SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN * 2f;

        private bool _moveSuccess;
        private float _recalcMoveTimer;

        private void EngageEnemy()
        {
            SAIN.Steering.SteerByPriority();
            Shoot.Update();
        }

        public override void Start()
        {
            if (SAIN.Decision.CurrentSelfDecision == SelfDecision.RunAwayGrenade 
                && SAIN.Talk.GroupTalk.FriendIsClose)
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.OnEnemyGrenade, ETagStatus.Combat, 0.33f);
            }

            _shallJumpToCover = false;
        }

        public override void Stop()
        {
            SAIN.Mover.SprintController.CancelRun();
            SAIN.Cover.CheckResetCoverInUse();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Run To Cover Info");
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

            var _coverDestination = SAIN.Cover.CoverInUse;
            if (_coverDestination != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{_coverDestination.Status}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{_coverDestination.CoverHeight} {_coverDestination.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{_coverDestination.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(_coverDestination.Position - SAIN.Position).magnitude}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Safe Path?", $"{_coverDestination.IsSafePath}", Color.white, Color.yellow, true);
            }
        }
    }
}