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

            if (_shallJumpToCover 
                && _sprinting
                && _moveSuccess 
                && _coverDestination != null
                && BotOwner.GetPlayer.IsSprintEnabled 
                && BotOwner.Mover.DistDestination < 2f 
                && _jumpTimer < Time.time)
            {
                _jumpTimer = Time.time + 5f;
                SAIN.Mover.TryJump();
            }

            if (!BotOwner.Mover.IsMoving)
            {
                _coverDestination = null;
            }

            if (_recalcMoveTimer < Time.time && _coverDestination == null)
            {
                _moveSuccess = moveToCover(out bool sprinting, out CoverPoint coverDestination);
                _sprinting = sprinting;

                if (_moveSuccess)
                {
                    if (_sprinting 
                        && _nextTryReloadTime < Time.time 
                        && SAIN.Decision.SelfActionDecisions.LowOnAmmo(0.5f))
                    {
                        _nextTryReloadTime = Time.time + 2f;
                        SAIN.SelfActions.TryReload();
                    }

                    _recalcMoveTimer = Time.time + 0.5f;
                    _coverDestination = coverDestination;
                    _runDestination = coverDestination.GetPosition(SAIN);
                }
                else
                {
                    //RecalcTimer = Time.time + 0.1f;
                }
            }

            if (_moveSuccess 
                && _sprinting 
                && (_runDestination - SAIN.Position).sqrMagnitude < SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN * SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN)
            {
                SAIN.Mover.Sprint(false);
                _sprinting = false;
            }

            if (!_moveSuccess)
            {
                SAIN.Mover.DogFight.DogFightMove();
            }

            if (!_sprinting || !BotOwner.Mover.IsMoving)
            {
                SAIN.Steering.SteerByPriority();
                Shoot.Update();
            }
            else
            {
                SAIN.Steering.LookToMovingDirection(500f, true);
            }
        }

        private Vector3 _runDestination;

        private bool moveToCover(out bool sprinting, out CoverPoint coverDestination)
        {
            CoverPoint fallback = SAIN.Cover.FallBackPoint;
            SoloDecision currentDecision = SAIN.Memory.Decisions.Main.Current;

            if (currentDecision == SoloDecision.Retreat
                && fallback != null
                && fallback.CheckPathSafety(SAIN))
            {
                if (tryRun(fallback, out sprinting))
                {
                    coverDestination = fallback;
                    return true;
                }
            }

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
            Vector3 destination = coverPoint.GetPosition(SAIN);

            if (shallRun(destination))
            {
                result = BotOwner.BotRun.Run(destination, false);
                if (result)
                {
                    sprinting = true;
                }
            }

            if (!result)
            {
                bool shallProne = SAIN.Mover.Prone.ShallProneHide();
                bool shallCrawl = SAIN.Decision.CurrentSelfDecision != SelfDecision.None
                    && _coverDestination.GetCoverStatus(SAIN) == CoverStatus.FarFromCover
                    && shallProne;

                //result = SAIN.Mover.GoToPoint(destination, out _, -1, shallCrawl, true);
                result = SAIN.Mover.GoToPoint(destination, out _, -1, shallCrawl);
            }
            return result;
        }

        private bool shallRun(Vector3 destination) => (destination - SAIN.Position).sqrMagnitude > SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN * SAIN.Info.FileSettings.Move.RUN_TO_COVER_MIN * 2f;

        private bool _moveSuccess;
        private float _recalcMoveTimer;
        private CoverPoint _coverDestination;

        private void EngageEnemy()
        {
            SAIN.Steering.SteerByPriority();
            Shoot.Update();
        }

        public override void Start()
        {
            if (SAIN.Decision.CurrentSelfDecision == SelfDecision.RunAwayGrenade)
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.OnEnemyGrenade, ETagStatus.Combat, 0.25f);
            }

            _shallJumpToCover = EFTMath.RandomBool(8) 
                && BotOwner.Memory.IsUnderFire 
                && SAIN.Info.Profile.IsPMC;
        }

        public override void Stop()
        {
            _coverDestination = null;
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

            if (_coverDestination != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{_coverDestination.GetCoverStatus(SAIN)}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{_coverDestination.CoverHeight} {_coverDestination.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{_coverDestination.CalcPathLength(SAIN)}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(_coverDestination.GetPosition(SAIN) - SAIN.Position).magnitude}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Safe Path?", $"{_coverDestination.CheckPathSafety(SAIN)}", Color.white, Color.yellow, true);
            }
        }
    }
}