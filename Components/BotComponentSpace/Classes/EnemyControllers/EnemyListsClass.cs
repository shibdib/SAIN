using SAIN.Helpers;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyListsClass : BotSubClassBase<SAINEnemyController>, ISAINClass
    {
        public readonly Dictionary<EEnemyListType, EnemyList> EnemyLists = new Dictionary<EEnemyListType, EnemyList>();

        public EnemyListsClass(SAINEnemyController controller) : base(controller)
        {
            foreach (var type in _types)
            {
                EnemyLists.Add(type, new EnemyList());
            }
        }

        public EnemyList GetEnemyList(EEnemyListType type) {
            EnemyLists.TryGetValue(type, out EnemyList enemyList);
            return enemyList;
        }

        public Enemy First(EEnemyListType type) {
            return EnemyLists[type].First();
        }

        public int HumanCount(EEnemyListType type) {
            return EnemyLists[type].Humans;
        }

        public int TotalCount(EEnemyListType type) {
            return EnemyLists[type].Count;
        }

        public int BotCount(EEnemyListType type) {
            return EnemyLists[type].Bots;
        }

        public void Init()
        {
            Bot.EnemyController.Events.OnEnemyAdded += enemyAdded;
            Bot.EnemyController.Events.OnEnemyRemoved += enemyRemoved;
        }

        private void enemyAdded(Enemy enemy)
        {
            subOrUnSub(true, enemy);
        }

        private void enemyRemoved(string profileID, Enemy enemy)
        {
            subOrUnSub(false, enemy);
            foreach (var list in EnemyLists.Values)
            {
                list.RemoveEnemy(enemy);
            }
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            var controller = Bot.EnemyController;
            if (controller != null)
            {
                controller.Events.OnEnemyAdded -= enemyAdded;
                controller.Events.OnEnemyRemoved -= enemyRemoved;
            }

            foreach (var list in EnemyLists.Values)
            {
                list.Clear();
            }
            EnemyLists.Clear();
        }


        private void subOrUnSub(bool value, Enemy enemy)
        {
            switch (value)
            {
                case true:

                    enemy.Events.OnEnemyKnownChanged.OnToggle +=
                        EnemyLists[EEnemyListType.Known].AddOrRemoveEnemy;

                    enemy.Events.OnActiveThreatChanged.OnToggle +=
                        EnemyLists[EEnemyListType.ActiveThreats].AddOrRemoveEnemy;

                    enemy.Events.OnVisionChange.OnToggle +=
                        EnemyLists[EEnemyListType.Visible].AddOrRemoveEnemy;

                    enemy.Events.OnEnemyLineOfSightChanged.OnToggle +=
                        EnemyLists[EEnemyListType.InLineOfSight].AddOrRemoveEnemy;

                    break;

                case false:

                    enemy.Events.OnEnemyKnownChanged.OnToggle -=
                        EnemyLists[EEnemyListType.Known].AddOrRemoveEnemy;

                    enemy.Events.OnActiveThreatChanged.OnToggle -=
                        EnemyLists[EEnemyListType.ActiveThreats].AddOrRemoveEnemy;

                    enemy.Events.OnVisionChange.OnToggle -=
                        EnemyLists[EEnemyListType.Visible].AddOrRemoveEnemy;

                    enemy.Events.OnEnemyLineOfSightChanged.OnToggle -=
                        EnemyLists[EEnemyListType.InLineOfSight].AddOrRemoveEnemy;

                    break;
            }
        }

        private static readonly EEnemyListType[] _types = EnumValues.GetEnum<EEnemyListType>();
    }
}