using EFT;
using SAIN.Layers.Combat.Solo.Cover;
using SAIN.SAINComponent;

namespace SAIN.Layers.Combat.Solo
{
    internal class CombatSoloLayer : SAINLayer
    {
        public CombatSoloLayer(BotOwner bot, int priority) : base(bot, priority, Name)
        {
        }

        public static readonly string Name = BuildLayerName<CombatSoloLayer>();

        public override Action GetNextAction()
        {
            _lastSelfDecision = _currentSelfDecision;
            _lastDecision = _currentDecision;

            if (_doSurgeryAction)
            {
                _doSurgeryAction = false;
                return new Action(typeof(DoSurgeryAction), $"Do Surgery");
            }

            switch (_lastDecision)
            {
                case SoloDecision.MoveToEngage:
                    return new Action(typeof(MoveToEngageAction), $"{_lastDecision}");

                case SoloDecision.RushEnemy:
                    return new Action(typeof(RushEnemyAction), $"{_lastDecision}");

                case SoloDecision.ThrowGrenade:
                    return new Action(typeof(ThrowGrenadeAction), $"{_lastDecision}");

                case SoloDecision.ShiftCover:
                    return new Action(typeof(ShiftCoverAction), $"{_lastDecision}");

                case SoloDecision.RunToCover:
                    return new Action(typeof(RunToCoverAction), $"{_lastDecision}");

                case SoloDecision.Retreat:
                    return new Action(typeof(RunToCoverAction), $"{_lastDecision} + {_lastSelfDecision}");

                case SoloDecision.MoveToCover:
                    return new Action(typeof(WalkToCoverAction), $"{_lastDecision}");

                case SoloDecision.DogFight:
                    return new Action(typeof(DogFightAction), $"{_lastDecision}");

                case SoloDecision.ShootDistantEnemy:
                case SoloDecision.StandAndShoot:
                    return new Action(typeof(StandAndShootAction), $"{_lastDecision}");

                case SoloDecision.HoldInCover:
                    if (_lastSelfDecision != SelfDecision.None)
                    {
                        return new Action(typeof(HoldinCoverAction), $"{_lastDecision} + {_lastSelfDecision}");
                    }
                    else
                    {
                        return new Action(typeof(HoldinCoverAction), $"{_lastDecision}");
                    }

                case SoloDecision.Search:
                    return new Action(typeof(SearchAction), $"{_lastDecision}");

                default:
                    return new Action(typeof(StandAndShootAction), $"DEFAULT! {_lastDecision}");
            }
        }

        public override bool IsActive()
        {
            bool active = SAINBot != null && _currentDecision != SoloDecision.None;
            if (active)
            {
                SAINBot.ActiveLayer = ESAINLayer.Combat;
            }
            else
            {
                SAINBot.ActiveLayer = ESAINLayer.None;
            }
            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            // this is dumb im sorry
            if (!_doSurgeryAction
                && _currentSelfDecision == SelfDecision.Surgery
                && SAINBot.Cover.BotIsAtCoverInUse())
            {
                _doSurgeryAction = true;
                return true;
            }

            if (_lastSelfDecision == SelfDecision.Surgery && 
                _currentSelfDecision != SelfDecision.Surgery )
            {
                return true;
            }

            return _currentDecision != _lastDecision;
        }

        private bool _doSurgeryAction;

        private SoloDecision _lastDecision = SoloDecision.None;
        private SelfDecision _lastSelfDecision = SelfDecision.None;
        public SoloDecision _currentDecision => SAINBot.Decision.CurrentSoloDecision;
        public SelfDecision _currentSelfDecision => SAINBot.Decision.CurrentSelfDecision;
    }
}