using System.Collections;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyUpdaterClass : SAINSubBase<SAINEnemyController>, ISAINClass
    {
        public EnemyUpdaterClass(SAINEnemyController controller) : base(controller)
        {

        }

        public void Init()
        {
            Bot.CoroutineManager.Add(enemyUpdater(), nameof(enemyUpdater));
            Bot.CoroutineManager.Add(enemyVisionCheck(), nameof(enemyVisionCheck));
        }


        public void Update()
        {

        }


        public void Dispose()
        {

        }

        private IEnumerator enemyUpdater()
        {
            while (true)
            {
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