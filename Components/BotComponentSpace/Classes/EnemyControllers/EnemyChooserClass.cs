using EFT;
using System;
using System.Collections;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyChooserClass : SAINSubBase<SAINEnemyController>, ISAINClass
    {
        public event Action<Enemy> OnEnemyChanged;

        public Enemy ActiveEnemy
        {
            get
            {
                return _activeEnemy;
            }
            private set
            {
                if (value == _activeEnemy)
                {
                    return;
                }

                if (_activeEnemy != null)
                {
                    _activeEnemy.EnemyKnownChecker.OnEnemyKnownChanged -= onActiveEnemyForgotten;
                    if (_activeEnemy.IsValid)
                    {
                        setLastEnemy(_activeEnemy);
                    }
                }

                _activeEnemy = value;
                OnEnemyChanged?.Invoke(value);

                if (_activeEnemy != null)
                    _activeEnemy.EnemyKnownChecker.OnEnemyKnownChanged += onActiveEnemyForgotten;
            }
        }
        public Enemy LastEnemy { get; private set; }

        public EnemyChooserClass(SAINEnemyController controller) : base(controller)
        {
        }

        public void Init()
        {
            Bot.CoroutineManager.Add(enemyChooser(), nameof(enemyChooser));
            BaseClass.OnEnemyRemoved += enemyRemoved;
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            BaseClass.OnEnemyRemoved -= enemyRemoved;
        }

        private void enemyRemoved(string profileId, Enemy enemy)
        {
            if (ActiveEnemy != null &&
                ActiveEnemy.EnemyProfileId == profileId)
            {
                ActiveEnemy = null;
            }
            if (LastEnemy != null &&
                LastEnemy.EnemyProfileId == profileId)
            {
                LastEnemy = null;
            }
        }

        public void ClearEnemy()
        {
            setActiveEnemy(null);
        }

        private void onActiveEnemyForgotten(Enemy enemy, bool known)
        {
            if (known)
            {
                return;
            }
            if (ActiveEnemy == null)
            {
                Logger.LogWarning("Active Enemy is already null");
                return;
            }
            if (ActiveEnemy.EnemyProfileId != enemy.EnemyProfileId)
            {
                Logger.LogWarning("You fucked up");
                return;
            }
            setActiveEnemy(null);
        }

        private IEnumerator enemyChooser()
        {
            while (true)
            {
                assignActiveEnemy();
                checkDiscrepency();

                yield return null;
            }
        }

        private void assignActiveEnemy()
        {
            Enemy activeEnemy = findActiveEnemy();

            if (activeEnemy != null && (!activeEnemy.IsValid || !activeEnemy.EnemyPerson.Active))
            {
                Logger.LogWarning($"Tried to assign inactive or invalid player.");
                activeEnemy = null;
            }

            if (activeEnemy == null || (activeEnemy.IsValid && activeEnemy.EnemyPerson.Active))
            {
                setActiveEnemy(activeEnemy);
            }
        }

        private Enemy findActiveEnemy()
        {
            Enemy dogFightTarget = Bot.Decision.DogFightDecision.DogFightTarget;
            if (dogFightTarget?.IsValid == true && dogFightTarget.EnemyPerson.Active)
            {
                return dogFightTarget;
            }

            checkGoalEnemy(out Enemy goalEnemy);

            if (goalEnemy != null)
            {
                if (!goalEnemy.IsVisible) {
                    Enemy visibileEnemy = BaseClass.EnemyLists.First(EEnemyListType.Visible);
                    if (visibileEnemy?.IsValid == true && 
                        visibileEnemy.EnemyPerson.Active) {
                        return visibileEnemy;
                    }
                }
                return goalEnemy;
            }

            foreach (var enemy in BaseClass.Enemies.Values)
            {
                if (enemy?.IsValid == true &&
                    enemy.EnemyPerson.Active &&
                    enemy.Status.ShotAtMeRecently)
                {
                    return enemy;
                }
            }

            return null;
        }

        private void checkGoalEnemy(out Enemy enemy)
        {
            enemy = null;

            EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;
            Enemy activeEnemy = ActiveEnemy;

            // make sure the bot's goal enemy isn't dead
            if (goalEnemy?.Person != null &&
                goalEnemy.Person.HealthController.IsAlive == false)
            {
                try  { BotOwner.Memory.GoalEnemy = null; }
                catch { // Sometimes bsg code throws an error here :D
                }
                goalEnemy = null;
            }

            // Bot has no goal enemy, set active enemy to null if they aren't already, and if they aren't currently visible or shot at me
            if (goalEnemy == null)
            {
                if (activeEnemy == null)
                {
                    return;
                }
                if (activeEnemy.IsValid && 
                    activeEnemy.EnemyPerson.Active && 
                    (activeEnemy.Status.ShotAtMeRecently || activeEnemy.IsVisible))
                {
                    enemy = activeEnemy;
                }
                return;
            }

            // if the bot's active enemy already matches goal enemy, do nothing
            if (activeEnemy != null &&
                activeEnemy.EnemyInfo.ProfileId == goalEnemy.ProfileId)
            {
                enemy = activeEnemy;
                return;
            }

            // our enemy is changing.
            activeEnemy = BaseClass.CheckAddEnemy(goalEnemy?.Person);

            if (activeEnemy == null)
            {
                Logger.LogError($"{goalEnemy?.Person?.ProfileId} not SAIN enemy!");
                return;
            }

            if (activeEnemy.IsValid && activeEnemy.EnemyPerson.Active)
            {
                enemy = activeEnemy;
            }
            else
            {
                enemy = null;
            }
        }

        private void setActiveEnemy(Enemy enemy)
        {
            if (enemy == null || (enemy.IsValid && enemy.EnemyPerson.Active))
            {
                ActiveEnemy = enemy;
                setGoalEnemy(enemy?.EnemyInfo);
            }
        }

        private void setLastEnemy(Enemy activeEnemy)
        {
            bool nullActiveEnemy = activeEnemy?.EnemyPerson?.Active == true;
            bool nullLastEnemy = LastEnemy?.EnemyPerson?.Active == true;

            if (!nullLastEnemy && nullActiveEnemy)
            {
                return;
            }
            if (nullLastEnemy && !nullActiveEnemy)
            {
                LastEnemy = activeEnemy;
                return;
            }
            if (!AreEnemiesSame(activeEnemy, LastEnemy))
            {
                LastEnemy = activeEnemy;
                return;
            }
        }

        private void setGoalEnemy(EnemyInfo enemyInfo)
        {
            if (BotOwner.Memory.GoalEnemy != enemyInfo)
            {
                try
                {
                    BotOwner.Memory.GoalEnemy = enemyInfo;
                    BotOwner.CalcGoal();
                }
                catch
                {
                    // Sometimes bsg code throws an error here :D
                }
            }
        }

        public bool AreEnemiesSame(Enemy a, Enemy b)
        {
            return AreEnemiesSame(a?.EnemyIPlayer, b?.EnemyIPlayer);
        }

        public bool AreEnemiesSame(IPlayer a, IPlayer b)
        {
            return a != null
                && b != null
                && a.ProfileId == b.ProfileId;
        }

        private void checkDiscrepency()
        {
            EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null && ActiveEnemy == null)
            {
                if (_nextLogTime < Time.time)
                {
                    _nextLogTime = Time.time + 1f;

                    Logger.LogError("Bot's Goal Enemy is not null, but SAIN enemy is null.");
                    if (goalEnemy.Person == null)
                    {
                        Logger.LogError("Bot's Goal Enemy Person is null");
                        return;
                    }
                    if (goalEnemy.ProfileId == Bot.ProfileId)
                    {
                        Logger.LogError("goalEnemy.ProfileId == SAINBot.ProfileId");
                        return;
                    }
                    if (goalEnemy.ProfileId == Bot.Player.ProfileId)
                    {
                        Logger.LogError("goalEnemy.ProfileId == SAINBot.Player.ProfileId");
                        return;
                    }
                    if (goalEnemy.ProfileId == Bot.BotOwner.ProfileId)
                    {
                        Logger.LogError("goalEnemy.ProfileId == SAINBot.Player.ProfileId");
                        return;
                    }
                    Enemy sainEnemy = BaseClass.GetEnemy(goalEnemy.ProfileId, true);
                    if (sainEnemy != null)
                    {
                        setActiveEnemy(sainEnemy);
                        Logger.LogError("Got SAINEnemy from goalEnemy.ProfileId");
                        return;
                    }
                    sainEnemy = BaseClass.CheckAddEnemy(goalEnemy.Person);
                    if (sainEnemy != null)
                    {
                        setActiveEnemy(sainEnemy);
                        Logger.LogError("Got SAINEnemy from goalEnemy.Person");
                        return;
                    }
                }
            }
        }

        private float _nextLogTime;
        private Enemy _activeEnemy;
    }
}