using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyUpdaterComponent : MonoBehaviour
    {
        public void Init(BotComponent bot)
        {
            Bot = bot;
        }

        private BotComponent Bot;

        private void Update()
        {
            if (Bot == null || Bot.EnemyController == null || !Bot.BotActive)
            {
                return;
            }

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
        }

        private void LateUpdate()
        {
            if (Bot == null || Bot.EnemyController == null || !Bot.BotActive)
            {
                return;
            }

            foreach (var kvp in Enemies)
            {
                checkValid(kvp.Key, kvp.Value);
            }
            removeInvalid();
        }

        private bool checkValid(string id, Enemy enemy)
        {
            if (enemy == null || enemy.CheckValid() == false)
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
                    Bot.EnemyController.RemoveEnemy(id);
                }
                Logger.LogWarning($"Removed {_idsToRemove.Count} Invalid Enemies");
                _idsToRemove.Clear();
            }
        }

        private Dictionary<string, Enemy> Enemies => Bot.EnemyController.Enemies;
        private readonly List<string> _idsToRemove = new List<string>();
    }
}