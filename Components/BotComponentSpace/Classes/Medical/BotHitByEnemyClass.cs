using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.SAINComponent.Classes
{
    public class BotHitByEnemyClass : BotMedicalBase, IBotClass
    {
        public Enemy EnemyWhoLastShotMe { get; private set; }

        public BotHitByEnemyClass(SAINBotMedicalClass medical) : base(medical)
        {
        }

        public void Init()
        {
            Bot.EnemyController.Events.OnEnemyRemoved += clearEnemy;
        }

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
            var player = damageInfo.Player?.iPlayer;
            if (player == null) {
                return;
            }
            Enemy enemy = Bot.EnemyController.GetEnemy(player.ProfileId, true);
            if (enemy == null) {
                return;
            }
            EnemyWhoLastShotMe = enemy;
            enemy.Status.GetHit(damageInfo);
        }

        private void clearEnemy(string profileId, Enemy enemy)
        {
            if (enemy == EnemyWhoLastShotMe)
                EnemyWhoLastShotMe = null;
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyRemoved -= clearEnemy;
        }
    }
}