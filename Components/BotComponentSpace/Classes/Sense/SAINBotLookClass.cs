using EFT;
using SAIN.SAINComponent.Classes.Enemy;
using System.Collections.Generic;
using UnityEngine;

// Found in Botowner.Looksensor
using EnemyVisionCheck = GClass501;
using LookAllData = GClass522;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotLookClass : SAINBase
    {
        public SAINBotLookClass(BotComponent component) : base(component)
        {
            LookData = new LookAllData();
        }

        public void Init()
        {
            _enemies = Bot.EnemyController.Enemies;
        }

        private Dictionary<string, SAINEnemy> _enemies;
        public readonly LookAllData LookData;

        public int UpdateLook()
        {
            if (BotOwner.LeaveData == null ||
                BotOwner.LeaveData.LeaveComplete)
            {
                return 0;
            }

            int numUpdated = UpdateLookForEnemies(LookData);
            UpdateLookData(LookData);
            return numUpdated;
        }

        public void UpdateLookData(LookAllData lookData)
        {
            for (int i = 0; i < lookData.reportsData.Count; i++) {
                EnemyVisionCheck enemyVision = lookData.reportsData[i];
                BotOwner.BotsGroup.ReportAboutEnemy(enemyVision.enemy, enemyVision.VisibleOnlyBuSence);
            }

            if (lookData.reportsData.Count > 0)
                BotOwner.Memory.SetLastTimeSeeEnemy();

            if (lookData.shallRecalcGoal)
                BotOwner.CalcGoal();

            lookData.Reset();
        }

        private int UpdateLookForEnemies(LookAllData lookAll)
        {
            int updated = 0;
            _cachedList.Clear();
            _cachedList.AddRange(_enemies.Values);
            foreach (SAINEnemy enemy in _cachedList) {

                if (!shallCheckEnemy(enemy))
                    continue;

                if (checkEnemy(enemy, lookAll)) {
                    updated++;
                }
            }
            _cachedList.Clear();
            return updated;
        }

        private readonly List<SAINEnemy> _cachedList = new List<SAINEnemy>();

        private bool shallCheckEnemy(SAINEnemy enemy)
        {
            if (enemy?.IsValid != true)
                return false;

            if (!enemy.InLineOfSight)
            {
                if (enemy.EnemyInfo.IsVisible)
                    enemy.EnemyInfo.SetVisible(false);
                return false;
            }
            return true;
        }

        private bool checkEnemy(SAINEnemy enemy, LookAllData lookAll)
        {
            float delay = getDelay(enemy);
            if (enemy.LastCheckLookTime + delay > Time.time)
            {
                return false;
            }

            enemy.LastCheckLookTime = Time.time;
            enemy.EnemyInfo.CheckLookEnemy(lookAll);
            return true;
        }

        private float getDelay(SAINEnemy enemy)
        {
            float delay;
            if (enemy.EnemyPerson.IsActive) {
                delay = enemy.IsAI ? 0.2f : 0.1f;
            }
            else {
                delay = 0.5f;
            }
            return delay * UnityEngine.Random.Range(0.9f, 1.1f);
        }

    }
}