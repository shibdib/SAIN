using EFT;
using SAIN.Layers.Combat.Solo;

namespace SAIN.Layers.Combat.Squad
{
    internal class CombatSquadLayer : SAINLayer
    {
        public static readonly string Name = BuildLayerName<CombatSquadLayer>();

        public CombatSquadLayer(BotOwner bot, int priority) : base(bot, priority, Name)
        {
        }

        public override Action GetNextAction()
        {
            var Decision = SquadDecision;
            LastActionDecision = Decision;
            switch (Decision)
            {
                case SquadDecision.Regroup:
                    return new Action(typeof(RegroupAction), $"{Decision}");

                case SquadDecision.Suppress:
                    return new Action(typeof(SuppressAction), $"{Decision}");

                case SquadDecision.Search:
                    return new Action(typeof(SearchAction), $"{Decision}");

                case SquadDecision.GroupSearch:
                    if (SAIN.Squad.IAmLeader)
                    {
                        return new Action(typeof(SearchAction), $"{Decision} : Lead Search Party");
                    }
                    return new Action(typeof(FollowSearchParty), $"{Decision} : Follow Squad Leader");

                case SquadDecision.Help:
                    return new Action(typeof(SearchAction), $"{Decision}");

                case SquadDecision.PushSuppressedEnemy:
                    return new Action(typeof(RushEnemyAction), $"{Decision}");

                default:
                    return new Action(typeof(RegroupAction), $"DEFAULT!");
            }
        }

        public override bool IsActive()
        {
            if (SAIN == null) return false;
            //if (SAIN.SAINEnabled == false) return false;

            if (SAIN.Decision.CurrentSquadDecision != SquadDecision.None
                && SAIN.Decision.CurrentSelfDecision == SelfDecision.None)
            {
                if (SAIN.Cover.CoverInUse != null)
                {
                    SAIN.Cover.CoverInUse = null;
                }
                return true;
            }
            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            if (SAIN == null) return true;
            //if (SAIN.SAINEnabled == false) return true;

            return SquadDecision != LastActionDecision;
        }

        private SquadDecision LastActionDecision = SquadDecision.None;
        public SquadDecision SquadDecision => SAIN.Decision.CurrentSquadDecision;
    }
}