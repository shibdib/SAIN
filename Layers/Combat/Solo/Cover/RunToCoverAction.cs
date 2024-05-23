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
            Bot.Mover.SetTargetMoveSpeed(1f);
            Bot.Mover.SetTargetPose(1f);

            if (Bot.Cover.CoverInUse != null)
            {
                // Jump into cover!
                float sqrMag = (Bot.Cover.CoverInUse.Position - Bot.Position).sqrMagnitude;
                if (_shallJumpToCover
                    && _sprinting
                    && Bot.Player.IsSprintEnabled
                    && _moveSuccess
                    && Bot.Cover.CoverInUse != null
                    && sqrMag < Bot.Info.FileSettings.Move.RUN_TO_COVER_MIN * Bot.Info.FileSettings.Move.RUN_TO_COVER_MIN * 1.2f
                    && _jumpTimer < Time.time)
                {
                    _jumpTimer = Time.time + 5f;
                    Bot.Mover.TryJump();
                }
                // Stop sprinting if close enough so you can navigate properly
                else if (_moveSuccess
                    && (_sprinting || Bot.Player.IsSprintEnabled)
                    && sqrMag < Bot.Info.FileSettings.Move.RUN_TO_COVER_MIN * Bot.Info.FileSettings.Move.RUN_TO_COVER_MIN)
                {
                    Bot.Mover.Sprint(false);
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
                Bot.Cover.CoverInUse = coverDestination;

                if (_moveSuccess)
                {
                    _recalcMoveTimer = Time.time + 2f;
                    _shallJumpToCover = EFTMath.RandomBool(8) 
                        && _sprinting
                        && BotOwner.Memory.IsUnderFire
                        && Bot.Info.Profile.IsPMC;

                    if (_sprinting 
                        && _nextTryReloadTime < Time.time 
                        && Bot.Decision.SelfActionDecisions.LowOnAmmo(0.5f))
                    {
                        _nextTryReloadTime = Time.time + 2f;
                        Bot.SelfActions.TryReload();
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
                Bot.Cover.CoverInUse = null;
                Bot.Mover.DogFight.DogFightMove();
            }

            Bot.Steering.SteerByPriority();
            Shoot.Update();
        }

        private Vector3 _runDestination;
        private CoverPoint _coverDestination;

        private bool moveToCover(out bool sprinting, out CoverPoint coverDestination)
        {
            CoverPoint coverInUse = Bot.Cover.CoverInUse;
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

            CoverPoint fallback = Bot.Cover.FallBackPoint;
            SoloDecision currentDecision = Bot.Memory.Decisions.Main.Current;

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

            Bot.Cover.SortPointsByPathDist();

            sprinting = false;
            var coverPoints = Bot.Cover.CoverPoints;

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
                result = Bot.Mover.SprintController.RunToPoint(destination);
                //result = BotOwner.BotRun.Run(destination, false, 0.25f);
                if (result)
                {
                    sprinting = true;
                }
            }

            if (!result)
            {
                bool shallProne = Bot.Mover.Prone.ShallProneHide();
                bool shallCrawl = Bot.Decision.CurrentSelfDecision != SelfDecision.None
                    && coverPoint.Status == CoverStatus.FarFromCover
                    && shallProne;

                //result = SAIN.Mover.GoToPoint(destination, out _, -1, shallCrawl, true);
                result = Bot.Mover.GoToPoint(destination, out _, 0.25f, shallCrawl);
            }
            return result;
        }

        private bool shallRun(Vector3 destination) => (destination - Bot.Position).sqrMagnitude > Bot.Info.FileSettings.Move.RUN_TO_COVER_MIN * Bot.Info.FileSettings.Move.RUN_TO_COVER_MIN * 2f;

        private bool _moveSuccess;
        private float _recalcMoveTimer;

        private void EngageEnemy()
        {
            Bot.Steering.SteerByPriority();
            Shoot.Update();
        }

        public override void Start()
        {
            if (Bot.Decision.CurrentSelfDecision == SelfDecision.RunAwayGrenade 
                && Bot.Talk.GroupTalk.FriendIsClose)
            {
                Bot.Talk.TalkAfterDelay(EPhraseTrigger.OnEnemyGrenade, ETagStatus.Combat, 0.33f);
            }

            _shallJumpToCover = false;
        }

        public override void Stop()
        {
            Bot.Mover.SprintController.CancelRun();
            Bot.Cover.CheckResetCoverInUse();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Run To Cover Info");
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

            var _coverDestination = Bot.Cover.CoverInUse;
            if (_coverDestination != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{_coverDestination.Status}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{_coverDestination.CoverHeight} {_coverDestination.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{_coverDestination.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(_coverDestination.Position - Bot.Position).magnitude}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Safe Path?", $"{_coverDestination.IsSafePath}", Color.white, Color.yellow, true);
            }
        }
    }
}