using EFT;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.SAINComponent.SubComponents
{
    public class SightCheckerComponent : MonoBehaviour
    {
        private BotOwner BotOwner;
        private SAINComponentClass SAIN;

        private void Awake()
        {
            BotOwner = GetComponent<BotOwner>();
            SAIN = GetComponent<SAINComponentClass>();
        }

        private void Update()
        {
            if (SAIN == null || !SAIN.BotActive || SAIN.GameIsEnding)
            {
                StopAllCoroutines();
                return;
            }
            if (AISightChecker == null)
            {
                AISightChecker = StartCoroutine(checkSightForAIEnemies());
            }
            if (HumanSightChecker == null)
            {
                HumanSightChecker = StartCoroutine(checkSightForHumanEnemies());
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private Coroutine AISightChecker;
        private Coroutine HumanSightChecker;

        private IEnumerator checkSightForAIEnemies()
        {
            while (true)
            {
                var enemies = SAIN?.EnemyController?.Enemies;
                if (enemies != null)
                {
                    _localAIEnemyList.AddRange(enemies);
                    foreach (var enemy in _localAIEnemyList.Values)
                    {
                        if (enemy.IsValid && enemy.IsAI)
                        {
                            enemy.Vision.CheckLineOfSight(true, enemy != SAIN.Enemy);
                            yield return null;
                        }
                    }
                    _localAIEnemyList.Clear();
                }
                yield return null;
            }
        }

        private IEnumerator checkSightForHumanEnemies()
        {
            while (true)
            {
                var enemies = SAIN?.EnemyController?.Enemies;
                if (enemies != null)
                {
                    _localHumanEnemyList.AddRange(enemies);
                    foreach (var enemy in _localHumanEnemyList.Values)
                    {
                        if (enemy.IsValid && !enemy.IsAI)
                        {
                            enemy.Vision.CheckLineOfSight(false, false);
                            yield return null;
                        }
                    }
                    _localHumanEnemyList.Clear();
                }
                yield return null;
            }
        }

        private Dictionary<string, SAINEnemy> _localAIEnemyList = new Dictionary<string, SAINEnemy>();
        private Dictionary<string, SAINEnemy> _localHumanEnemyList = new Dictionary<string, SAINEnemy>();

        public void Dispose()
        {
            _nextCheckTimes.Clear();
            Destroy(this);
        }

        public bool SimpleSightCheck(Vector3 target, Vector3 source)
        {
            Vector3 direction = target - source;
            return !Physics.Raycast(source, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);
        }

        public bool CheckLineOfSight(Player player)
        {
            PlayerSightResult info = GetInfo(player);
            if (info == null)
            {
                return false;
            }

            info.PartHits = 0;
            info.TotalParts = 0;
            info.InSight = false;

            foreach (var part in player.MainParts.Values)
            {
                info.TotalParts++;

                Vector3 headPos = BotOwner.LookSensor._headPoint;
                Vector3 direction = part.Position - headPos;
                if (!Physics.Raycast(headPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    info.PartHits++;
                    info.InSight = true;
                    break;
                }
            }
            return info.InSight;
        }

        private PlayerSightResult GetInfo(Player player)
        {
            if (player == null) return null;

            if (!_nextCheckTimes.ContainsKey(player.ProfileId))
            {
                _nextCheckTimes.Add(player.ProfileId, new PlayerSightResult());
                player.OnPlayerDeadOrUnspawn += RemoveInfo;
            }
            return _nextCheckTimes[player.ProfileId];
        }

        private void RemoveInfo(Player player)
        {
            if (player != null)
            {
                _nextCheckTimes.Remove(player.ProfileId);
                player.OnPlayerDeadOrUnspawn -= RemoveInfo;
            }
        }

        private readonly Dictionary<string, PlayerSightResult> _nextCheckTimes = new Dictionary<string, PlayerSightResult>();
    }

    public sealed class PlayerSightResult
    {
        public int PartHits;
        public int TotalParts;
        public bool InSight;
    }
}
