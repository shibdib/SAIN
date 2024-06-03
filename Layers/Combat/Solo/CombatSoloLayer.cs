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
            SoloDecision Decision = _currentDecision;
            var SelfDecision = SAINBot.Decision.CurrentSelfDecision;
            _lastDecision = Decision;

            if (_doSurgeryAction)
            {
                _doSurgeryAction = false;
                return new Action(typeof(DoSurgeryAction), $"Do Surgery");
            }

            switch (Decision)
            {
                case SoloDecision.MoveToEngage:
                    return new Action(typeof(MoveToEngageAction), $"{Decision}");

                case SoloDecision.RushEnemy:
                    return new Action(typeof(RushEnemyAction), $"{Decision}");

                case SoloDecision.ThrowGrenade:
                    return new Action(typeof(ThrowGrenadeAction), $"{Decision}");

                case SoloDecision.ShiftCover:
                    return new Action(typeof(ShiftCoverAction), $"{Decision}");

                case SoloDecision.RunToCover:
                    return new Action(typeof(RunToCoverAction), $"{Decision}");

                case SoloDecision.Retreat:
                    return new Action(typeof(RunToCoverAction), $"{Decision} + {SelfDecision}");

                case SoloDecision.MoveToCover:
                    return new Action(typeof(WalkToCoverAction), $"{Decision}");

                case SoloDecision.DogFight:
                    return new Action(typeof(DogFightAction), $"{Decision}");

                case SoloDecision.ShootDistantEnemy:
                case SoloDecision.StandAndShoot:
                    return new Action(typeof(StandAndShootAction), $"{Decision}");

                case SoloDecision.HoldInCover:
                    if (SelfDecision != SelfDecision.None)
                    {
                        return new Action(typeof(HoldinCoverAction), $"{Decision} + {SelfDecision}");
                    }
                    else
                    {
                        return new Action(typeof(HoldinCoverAction), $"{Decision}");
                    }

                case SoloDecision.Search:
                    return new Action(typeof(SearchAction), $"{Decision}");

                default:
                    return new Action(typeof(StandAndShootAction), $"DEFAULT! {Decision}");
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
                && SAINBot.Decision.CurrentSelfDecision == SelfDecision.Surgery
                && SAINBot.Cover.BotIsAtCoverInUse())
            {
                _doSurgeryAction = true;
                return true;
            }

            return _currentDecision != _lastDecision;
        }

        private bool _doSurgeryAction;

        private SoloDecision _lastDecision = SoloDecision.None;
        public SoloDecision _currentDecision => SAINBot.Decision.CurrentSoloDecision;
    }
}