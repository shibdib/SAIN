using SAIN.SAINComponent.Classes.Enemy;
using System;
using System.Collections.Generic;

namespace SAIN.Components.BotComponentSpace.Classes.Enemy
{
    public class EnemyHearing : EnemyBase, ISAINEnemyClass
    {
        public EnemyHearing(SAINEnemy enemy) : base(enemy)
        {

        }

        public void Init()
        {

        }

        public void Update()
        {

        }

        public void Dispose()
        {

        }

        public void onEnemyForgotten(SAINEnemy enemy)
        {

        }

        public void onEnemyKnown(SAINEnemy enemy)
        {

        }

        public float DispersionModifier
        {
            get
            {
                return 1f;
            }
        }

        private float angleModifier()
        {
            return 1f;
        }

        private float distanceModifier()
        {
            return 1f;
        }

        private float soundTypeModifier(SAINSoundType soundType)
        {
            return 1f;
        }

        private float weaponTypeModifier()
        {
            return 1f;
        }
    }
}
