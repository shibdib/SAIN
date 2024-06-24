using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections.Generic;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public class EnemyHearing : EnemyBase, ISAINEnemyClass
    {
        public BotSound LastSoundHeard { get; set; }

        public EnemyHearing(Enemy enemy) : base(enemy)
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

        public void onEnemyForgotten(Enemy enemy)
        {

        }

        public void onEnemyKnown(Enemy enemy)
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
