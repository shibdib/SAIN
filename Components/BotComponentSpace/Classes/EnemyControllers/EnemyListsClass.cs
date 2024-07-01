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
            UpdatePresetSettings(SAINPlugin.LoadedPreset);
            Bot.EnemyController.OnEnemyAdded += enemyAdded;
            Bot.EnemyController.OnEnemyRemoved += enemyRemoved;
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
                controller.OnEnemyAdded -= enemyAdded;
                controller.OnEnemyRemoved -= enemyRemoved;
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

                    enemy.EnemyKnownChecker.OnEnemyKnownChanged +=
                        EnemyLists[EEnemyListType.Known].AddOrRemoveEnemy;

                    enemy.ActiveThreatChecker.OnActiveThreatChanged +=
                        EnemyLists[EEnemyListType.ActiveThreats].AddOrRemoveEnemy;

                    enemy.Vision.OnVisionChange +=
                        EnemyLists[EEnemyListType.Visible].AddOrRemoveEnemy;

                    enemy.Vision.EnemyVisionChecker.OnEnemyLineOfSightChanged +=
                        EnemyLists[EEnemyListType.InLineOfSight].AddOrRemoveEnemy;

                    break;

                case false:

                    enemy.EnemyKnownChecker.OnEnemyKnownChanged -=
                        EnemyLists[EEnemyListType.Known].AddOrRemoveEnemy;

                    enemy.ActiveThreatChecker.OnActiveThreatChanged -=
                        EnemyLists[EEnemyListType.ActiveThreats].AddOrRemoveEnemy;

                    enemy.Vision.OnVisionChange -=
                        EnemyLists[EEnemyListType.Visible].AddOrRemoveEnemy;

                    enemy.Vision.EnemyVisionChecker.OnEnemyLineOfSightChanged -=
                        EnemyLists[EEnemyListType.InLineOfSight].AddOrRemoveEnemy;

                    break;
            }
        }

        private static readonly EEnemyListType[] _types = EnumValues.GetEnum<EEnemyListType>();
    }
}