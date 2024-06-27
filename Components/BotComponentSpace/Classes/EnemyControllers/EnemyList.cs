using System;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyList : List<Enemy>
    {
        public event Action<bool> OnListEmptyOrGetFirst;
        public event Action<bool> OnListEmptyOrGetFirstHuman;

        public void AddOrRemoveEnemy(Enemy enemy, bool value)
        {
            if (value)
            {
                this.AddEnemy(enemy);
            }
            else
            {
                this.RemoveEnemy(enemy);
            }
        }

        private void sortByLastUpdated()
        {
            this.Sort((x, y) => x.KnownPlaces.TimeSinceLastKnownUpdated.CompareTo(y.KnownPlaces.TimeSinceLastKnownUpdated));
        }

        public Enemy First()
        {
            switch (this.Count)
            {
                case 0: return null;
                case 1: break;
                default:
                    sortByLastUpdated();
                    break;
            }
            return this[0];
        }

        public void AddEnemy(Enemy enemy)
        {
            this.Add(enemy);

            if (this.Count == 1) {
                OnListEmptyOrGetFirst?.Invoke(true);
            }

            if (!enemy.IsAI)
            {
                Humans++;
                if (Humans == 1) {
                    OnListEmptyOrGetFirstHuman?.Invoke(true);
                }
            }
            else
            {
                Bots++;
            }
        }

        public bool RemoveEnemy(Enemy enemy)
        {
            if (enemy == null) 
                return false;

            bool removed = this.Remove(enemy);

            if (removed)  {
                if (!enemy.IsAI)
                {
                    Humans--;
                    if (Humans == 0)
                    {
                        OnListEmptyOrGetFirstHuman?.Invoke(false);
                    }
                }
                else
                {
                    Bots--;
                }
            }

            if (removed && this.Count == 0) {
                OnListEmptyOrGetFirst?.Invoke(false);
            }

            return removed;
        }

        public int Humans { get; private set; }
        public int Bots { get; private set; }
    }
}