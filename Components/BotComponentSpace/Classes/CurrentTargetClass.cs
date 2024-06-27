using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using UnityEngine;
using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.SAINComponent.Classes
{
    public class CurrentTargetClass : SAINBase
    {
        public CurrentTargetClass(BotComponent bot) : base(bot) { }

        public void Update()
        {
            updateCurrentTarget();
            UpdateGoalTarget();
        }

        private void updateCurrentTarget()
        {
            _nextGetTargetTime += Time.deltaTime;
            if (_nextGetTargetTime > 0.05f)
            {
                _nextGetTargetTime = 0f;
                _currentTarget = getTarget();
                if (_currentTarget != null )
                {
                    Vector3 dir = _currentTarget.Value - Position;
                    CurrentTargetDirection = dir;
                    CurrentTargetDistance = dir.magnitude;
                }
                else
                {
                    CurrentTargetDirection = null;
                    CurrentTargetDistance = float.MaxValue;
                }
            }
        }

        private Enemy Enemy => Bot.EnemyController.ActiveEnemy;
        private Vector3 Position => Bot.Position;

        private void UpdateGoalTarget()
        {
            if (_updateGoalTargetTime < Time.time)
            {
                _updateGoalTargetTime = Time.time + 0.5f;

                var goalTarget = BotOwner.Memory.GoalTarget;
                var Target = goalTarget?.Position;
                if (Target != null)
                {
                    if ((Target.Value - Position).sqrMagnitude < 2f ||
                        goalTarget.CreatedTime > 120f)
                    {
                        goalTarget.Clear();
                        BotOwner.CalcGoal();
                    }
                }
            }
        }

        private float _updateGoalTargetTime;

        public Vector3? GoalTargetPosition => BotOwner.Memory.GoalTarget.Position;

        public Vector3? CurrentTargetPosition => _currentTarget;

        public Vector3? CurrentTargetDirection { get; private set; }

        public float CurrentTargetDistance { get; private set; }

        private float _nextGetTargetTime;
        private Vector3? _currentTarget;

        private Vector3? getTarget()
        {
            Vector3? target = 
                getVisibleEnemyPos() ?? 
                getLastHitPosition() ?? 
                getUnderFirePosition() ??
                getEnemylastKnownPos();

            return target;
        }

        private Vector3? getVisibleEnemyPos()
        {
            Enemy enemy = Enemy;
            if (enemy != null)
            {
                Vector3 pos = enemy.EnemyPosition;
                if (enemy.IsVisible)
                {
                    return pos;
                }
                if (enemy.Seen && enemy.TimeSinceSeen < 1f)
                {
                    //return pos;
                }
            }
            return null;
        }

        private Vector3? getLastHitPosition()
        {
            if (Bot.Medical.TimeSinceShot > 5f)
            {
                return null;
            }

            Enemy enemy = Bot.Medical.HitByEnemy.EnemyWhoLastShotMe;
            if (enemy == null || !enemy.IsValid ||  enemy.IsCurrentEnemy)
            {
                return null;
            }

            return enemy.LastKnownPosition ?? enemy.Status.LastShotPosition;
        }

        private Vector3? getUnderFirePosition()
        {
            if (!BotOwner.Memory.IsUnderFire)
            {
                return null;
            }

            Enemy enemy = Bot.Memory.LastUnderFireEnemy;
            if (enemy == null ||
                !enemy.IsValid ||
                enemy.IsCurrentEnemy)
            {
                return null;
            }
            return enemy.LastKnownPosition ?? Bot.Memory.UnderFireFromPosition;
        }

        private Vector3? getEnemylastKnownPos()
        {
            Enemy enemy = Enemy;
            if (enemy != null)
            {
                var lastKnownPlace = enemy.KnownPlaces.LastKnownPlace;
                if (lastKnownPlace != null)
                {
                    return lastKnownPlace.Position;
                }
                if (enemy.KnownPlaces.LastSeenPlace != null)
                {
                    return enemy.KnownPlaces.LastSeenPlace.Position;
                }
                if (enemy.KnownPlaces.LastHeardPlace != null)
                {
                    return enemy.KnownPlaces.LastHeardPlace.Position;
                }
            }
            return null;
        }
    }
}
