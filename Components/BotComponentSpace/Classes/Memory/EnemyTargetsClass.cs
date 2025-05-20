using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.Memory
{
    public class EnemyTargetsClass
    {
        public void SetEnemy(EEnemyTargetType type, Enemy enemy)
        {
            if (EnemyTargets.TryGetValue(type, out EnemyTarget enemyTarget))
            {
                enemyTarget.SetEnemy(enemy);
            }
            else
            {
                enemyTarget = new EnemyTarget(type);
                enemyTarget.SetEnemy(enemy);
                EnemyTargets.Add(type, enemyTarget);
            }
        }

        public Enemy GetCurrent(EEnemyTargetType type)
        {
            if (EnemyTargets.TryGetValue(type, out EnemyTarget enemyTarget))
            {
                return enemyTarget.Current;
            }
            return null;
        }

        public Enemy GetLast(EEnemyTargetType type)
        {
            if (EnemyTargets.TryGetValue(type, out EnemyTarget enemyTarget))
            {
                return enemyTarget.Last;
            }
            return null;
        }

        public bool HasEnemy => PrimaryEnemy?.EnemyPerson?.Active == true;
        public bool HasLastEnemy => LastPrimaryEnemy?.EnemyPerson?.Active == true;

        public Enemy PrimaryEnemy
        {
            get
            {
                return GetCurrent(EEnemyTargetType.ActiveEnemy);
            }
            set
            {
                SetEnemy(EEnemyTargetType.ActiveEnemy, value);
            }
        }

        public Enemy LastPrimaryEnemy { get; private set; }

        public readonly Dictionary<EEnemyTargetType, EnemyTarget> EnemyTargets = new();

        public bool IsHumanPlayerActiveEnemy => PrimaryEnemy?.IsAI == true;
    }
}
