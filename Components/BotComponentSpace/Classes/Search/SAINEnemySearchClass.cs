using SAIN.SAINComponent.Classes.Enemy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Search
{
    public class SAINEnemySearchClass : EnemyBase
    {
        public bool Searching => _searchCoroutine != null;

        private bool checkReset()
        {
            if (_checkResetTime < Time.time)
            {
                _checkResetTime = Time.time + 1f;

                if (!Enemy.IsCurrentEnemy)
                {
                    StopSearch();
                    return true;
                }
                Vector3? lastKnown = Enemy.LastKnownPosition;
                if (lastKnown == null)
                {
                    StopSearch();
                    return true;
                }
                if ((lastKnown.Value - _searchDestination).magnitude > 1f)
                {
                    resetSearch();
                    return true;
                }
            }
            return false;
        }

        private IEnumerator search()
        {
            while (true)
            {
                if (checkReset())
                {
                    break;
                }


                yield return null;
            }
        }

        private void resetSearch()
        {
            StopSearch();
            StartSearch();
        }

        public void StartSearch()
        {
            if (!Searching)
            {
                _cornersToEnemy.Clear();
                _cornersToEnemy.AddRange(Enemy.Path.PathToEnemy.corners);
                _searchDestination = Enemy.LastKnownPosition.Value;
                _searchCoroutine = Bot.StartCoroutine(search());
            }
        }

        public void StopSearch()
        {
            if (Searching)
            {
                Bot.StopCoroutine(_searchCoroutine);
                _searchCoroutine = null;
                _cornersToEnemy.Clear();
                _searchDestination = Vector3.zero;
            }
        }

        private void checkStopSearch(SAINEnemy enemy, float time)
        {
            if (Searching && 
                enemy.EnemyProfileId == Enemy.EnemyProfileId)
            {
                StopSearch();
            }
        }

        public void Init()
        {
            Bot.OnBotDisabled += StopSearch;
            Bot.EnemyController.OnEnemyForgotten += checkStopSearch;
        }

        public void Dispose()
        {
            Bot.OnBotDisabled -= StopSearch;
            Bot.EnemyController.OnEnemyForgotten -= checkStopSearch;
        }

        public SAINEnemySearchClass(SAINEnemy enemy) : base(enemy)
        {
        }

        private readonly List<Vector3> _cornersToEnemy = new List<Vector3>();
        private float _checkResetTime;
        private Coroutine _searchCoroutine;
        private Vector3 _searchDestination;
    }
}