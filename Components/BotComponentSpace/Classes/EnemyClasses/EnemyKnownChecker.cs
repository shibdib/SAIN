using SAIN.SAINComponent.Classes.EnemyClasses;
using System;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public class EnemyKnownChecker : EnemyBase, ISAINClass
    {
        public event Action<Enemy, bool> OnEnemyKnownChanged;

        public bool EnemyKnown {  get; private set; }

        public EnemyKnownChecker(Enemy enemy) : base(enemy)
        {

        }

        public void Init()
        {
            Bot.BotActivation.OnBotStateChanged += botStateChanged;
        }

        public void Update()
        {
            checkShallKnowEnemy();
        }

        public void Dispose()
        {
            Bot.BotActivation.OnBotStateChanged -= botStateChanged;
        }

        private void checkShallKnowEnemy()
        {
            bool enemyKnown = shallKnowEnemy();
            setEnemyKnown(enemyKnown);
        }

        private void botStateChanged(bool botActive)
        {
            if (!botActive)
            {
                setEnemyKnown(false);
            }
        }

        private void setEnemyKnown(bool enemyKnown)
        {
            if (EnemyKnown != enemyKnown)
            {
                bool wasKnown = EnemyKnown;
                EnemyKnown = enemyKnown;
                OnEnemyKnownChanged?.Invoke(Enemy, enemyKnown);
            }
        }

        private bool shallKnowEnemy()
        {
            if (!Enemy.IsValid)
            {
                return false;
            }
            if (!EnemyPlayerComponent.IsActive)
            {
                return false;
            }

            if (Enemy.KnownPlaces.TimeSinceLastKnownUpdated < Bot.Info.ForgetEnemyTime)
            {
                return true;
            }
            if (BotIsSearchingForMe())
            {
                return true;
            }
            return false;
        }

        public bool BotIsSearchingForMe()
        {
            if (!isBotSearching())
            {
                return false;
            }
            Enemy searchTarget = Bot.Search.SearchTarget;
            if (searchTarget != null && searchTarget == Enemy)
            {
                return !Enemy.KnownPlaces.SearchedAllKnownLocations;
            }
            return false;
        }

        private bool isBotSearching()
        {
            if (Bot.Decision.CurrentSoloDecision == SoloDecision.Search)
            {
                return true;
            }
            var squadDecision = Bot.Decision.CurrentSquadDecision;
            if (squadDecision == SquadDecision.Search ||
                squadDecision == SquadDecision.GroupSearch)
            {
                return true;
            }
            return false;
        }
    }
}
