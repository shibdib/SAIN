using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Search
{
    public class SearchDeciderClass : SAINSubBase<SAINSearchClass>
    {
        public SearchDeciderClass(SAINSearchClass searchClass) : base(searchClass)
        {

        }

        public bool ShallStartSearch(bool mustHaveTarget)
        {
            calcSearchTime();
            Enemy enemy = Bot.Enemy;
            if (!WantToSearch(enemy))
            {
                return false;
            }
            bool searching = Bot.Decision.CurrentSoloDecision == SoloDecision.Search;
            if (searching)
            {
                return BaseClass.FinalDestination != null;
            }
            return BaseClass.PathFinder.HasPathToSearchTarget(mustHaveTarget);
        }

        private void calcSearchTime()
        {
            if (Bot.Decision.CurrentSoloDecision != SoloDecision.Search
                && _nextRecalcSearchTime < Time.time)
            {
                _nextRecalcSearchTime = Time.time + 120f;
                Bot.Info.CalcTimeBeforeSearch();
            }
        }

        public bool WantToSearch(Enemy enemy)
        {
            if (enemy == null)
            {
                return false;
            }
            if (!enemy.Seen && !enemy.Heard)
            {
                return false;
            }
            if (!enemy.Seen && !Bot.Info.PersonalitySettings.Search.WillSearchFromAudio)
            {
                return false;
            }
            if (!canStartSearch(enemy))
            {
                return false;
            }
            if (!shallSearch(enemy))
            {
                return false;
            }
            return true;
        }

        private bool shallSearch(Enemy enemy)
        {
            if (ShallBeStealthyDuringSearch(enemy) &&
                Bot.Decision.EnemyDecisions.UnFreezeTime > Time.time &&
                enemy.TimeSinceLastKnownUpdated > 10f)
            {
                return true;
            }

            float timeBeforeSearch = Bot.Info.TimeBeforeSearch;
            if (enemy.Status.SearchStarted)
            {
                return shallContinueSearch(enemy, timeBeforeSearch);
            }
            return shallBeginSearch(enemy, timeBeforeSearch);
        }

        public bool ShallBeStealthyDuringSearch(Enemy enemy)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Mind.SneakyBots)
            {
                return false;
            }
            if (SAINPlugin.LoadedPreset.GlobalSettings.Mind.OnlySneakyPersonalitiesSneaky &&
                !Bot.Info.PersonalitySettings.Search.Sneaky)
            {
                return false;
            }
            if (!enemy.EnemyHeardFromPeace)
            {
                return false;
            }

            float maxDist = SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaximumDistanceToBeSneaky;
            return enemy.RealDistance < maxDist;
        }

        private bool shallBeginSearchCauseLooting(Enemy enemy)
        {
            if (!enemy.Status.EnemyIsLooting)
            {
                return false;
            }
            if (_nextCheckLootTime < Time.time)
            {
                _nextCheckLootTime = Time.time + _checkLootFreq;
                return EFTMath.RandomBool(_searchLootChance);
            }
            return false;
        }

        private float _nextCheckLootTime;
        private float _checkLootFreq = 1f;
        private float _searchLootChance = 40f;

        private bool shallBeginSearch(Enemy enemy, float timeBeforeSearch)
        {
            if (shallBeginSearchCauseLooting(enemy))
            {
                enemy.Status.SearchingBecauseLooting = true;
                return true;
            }
            float myPower = Bot.Info.Profile.PowerLevel;
            if (enemy.EnemyPlayer.AIData.PowerOfEquipment < myPower * 0.5f)
            {
                return true;
            }
            if (enemy.Seen && enemy.TimeSinceSeen >= timeBeforeSearch)
            {
                return true;
            }
            if (enemy.Heard &&
                Bot.Info.PersonalitySettings.Search.WillSearchFromAudio &&
                enemy.TimeSinceHeard >= timeBeforeSearch)
            {
                return true;
            }
            return false;
        }

        private bool canStartSearch(Enemy enemy)
        {
            var searchSettings = Bot.Info.PersonalitySettings.Search;
            if (!searchSettings.WillSearchForEnemy)
            {
                return false;
            }
            if (Bot.Suppression.IsHeavySuppressed)
            {
                return false;
            }
            if (enemy.IsVisible)
            {
                return false;
            }
            return true;
        }

        private bool shallContinueSearch(Enemy enemy, float timeBeforeSearch)
        {
            if (enemy.Status.SearchingBecauseLooting)
            {
                return true;
            }
            if (enemy.Seen)
            {
                timeBeforeSearch = Mathf.Clamp(timeBeforeSearch / 3f, 0f, 120f);
                return enemy.TimeSinceSeen >= timeBeforeSearch;
            }
            if (enemy.Heard && Bot.Info.PersonalitySettings.Search.WillSearchFromAudio)
            {
                return true;
            }
            return false;
        }

        private float _nextRecalcSearchTime;
    }
}