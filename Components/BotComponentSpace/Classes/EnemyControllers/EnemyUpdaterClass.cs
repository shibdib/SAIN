using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyUpdaterClass : SAINSubBase<SAINEnemyController>, ISAINClass
    {
        public EnemyUpdaterClass(SAINEnemyController controller) : base(controller)
        {

        }

        public void Init()
        {
            //Bot.CoroutineManager.Add(enemyUpdater(), nameof(enemyUpdater));
            //Bot.CoroutineManager.Add(enemyVisionCheck(), nameof(enemyVisionCheck));
        }

        public void Update()
        {
            foreach (var kvp in Enemies)
            {
                string profileId = kvp.Key;
                Enemy enemy = kvp.Value;
                if (!checkValid(profileId, enemy))
                    continue;

                enemy.Update();
                enemy.Vision.EnemyVisionChecker.CheckVision(out _);
            }

            removeInvalid();

            if (Bot.BotActive)
            {
                // Very clumsy way of checking this, but I just want to make sure.
                // Remove later once its confirmed working fine.
                if (_timeLastUpdated + 1f > Time.time)
                {
                    //Logger.LogError($"Enemy Updater is not running!");
                    //Bot.CoroutineManager.Remove(nameof(enemyUpdater));
                    //Bot.CoroutineManager.Add(enemyUpdater(), nameof(enemyUpdater));
                }
                if (_timeLastVisionChecked + 1f > Time.time)
                {
                    //Logger.LogError($"Enemy Vision Checker is not running!");
                    //Bot.CoroutineManager.Remove(nameof(enemyVisionCheck));
                    //Bot.CoroutineManager.Add(enemyVisionCheck(), nameof(enemyVisionCheck));
                }
            }
        }

        public void Dispose()
        {
        }

        private float _timeLastUpdated;
        private float _timeLastVisionChecked;

        private IEnumerator enemyUpdater()
        {
            while (true)
            {
                // Remove Later once its confirmed working.
                _timeLastUpdated = Time.time;

                foreach (var kvp in Enemies)
                {
                    string profileId = kvp.Key;
                    Enemy enemy = kvp.Value;
                    if (!checkValid(profileId, enemy))
                        continue;

                    enemy.Update();
                }

                removeInvalid();
                yield return null;
            }
        }

        private bool checkValid(string id, Enemy enemy)
        {
            if (enemy == null || enemy.IsValid == false)
            {
                _idsToRemove.Add(id);
                return false;
            }
            return true;
        }

        private void removeInvalid()
        {
            if (_idsToRemove.Count > 0)
            {
                foreach (var id in _idsToRemove)
                {
                    BaseClass.RemoveEnemy(id);
                }
                Logger.LogAndNotifyInfo($"Removed {_idsToRemove.Count} Invalid Enemies");
                _idsToRemove.Clear();
            }
        }

        private IEnumerator enemyVisionCheck()
        {
            while (true)
            {
                // Remove Later once its confirmed working.
                _timeLastVisionChecked = Time.time;

                foreach (var enemy in Enemies.Values)
                {
                    enemy?.Vision.EnemyVisionChecker.CheckVision(out _);
                }
                yield return null;
            }
        }

        private Dictionary<string, Enemy> Enemies => BaseClass.Enemies;
        private readonly List<string> _idsToRemove = new List<string>();
    }
}