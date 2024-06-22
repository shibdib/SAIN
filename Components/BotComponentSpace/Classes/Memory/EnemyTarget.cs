using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.SAINComponent.Classes.Memory
{
    public class EnemyTarget
    {
        public EnemyTarget(EEnemyTargetType type)
        {
            Type = type;
        }

        public readonly EEnemyTargetType Type;

        public Enemy Current { get; private set; }

        public Enemy Last { get; private set; }

        public void ClearEnemy(string profileID)
        {
            if (Current != null &&
                Current.EnemyProfileId == profileID)
            {
                Current = null;
            }
            if (Last != null &&
                Last.EnemyProfileId == profileID)
            {
                Last = null;
            }
        }

        public void SetEnemy(Enemy replacementEnemy)
        {
            // if both our current and replacement are null. Do nothing.
            if (Current == null && replacementEnemy == null)
            {
                return;
            }

            // if our replacement enemy is the same as our current enemy, do nothing.
            if (areEnemiesSame(Current, replacementEnemy))
            {
                return;
            }

            // if we do not have a current enemy, no need to set them to our last.
            if (Current == null &&
                replacementEnemy != null)
            {
                Logger.LogDebug($"Setting [{replacementEnemy.EnemyPerson.Name}] as Current enemy for [{Current.BotOwner.name}]");
                Current = replacementEnemy;
                return;
            }

            // Sanity check
            if (areEnemiesSame(Last, Current))
            {
                Logger.LogWarning($"{Current.BotOwner.name}'s last enemy [{Last.EnemyPerson.Name}] is the same as their current enemy! [{Current.EnemyPerson.Name}] You fucked up the logic somewhere!");
                return;
            }

            // if our replacement enemy is null, but our active enemy we are replacing is not null...
            // set our active enemy to our last enemy
            if (replacementEnemy == null &&
                Current != null)
            {
                Logger.LogDebug($"Setting [{Current.EnemyPerson.Name}] as Last enemy for [{Current.BotOwner.name}]");
                Last = Current;
            }
        }

        private bool areEnemiesSame(Enemy a, Enemy b)
        {
            return
                a != null &&
                b != null &&
                a.EnemyProfileId == b.EnemyProfileId;
        }
    }
}
