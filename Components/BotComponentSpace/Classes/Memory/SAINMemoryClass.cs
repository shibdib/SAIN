using EFT;
using HarmonyLib;
using JetBrains.Annotations;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Memory
{
    public class SAINMemoryClass : BotBaseClass, ISAINClass
    {
        public readonly EnemyTargetsClass EnemyTargets = new EnemyTargetsClass();

        public SAINMemoryClass(BotComponent sain) : base(sain)
        {
            Health = new HealthTracker(sain);
            Location = new LocationTracker(sain);
        }

        public void Init()
        {
            Bot.EnemyController.OnEnemyRemoved += clearEnemy;
        }

        private void clearEnemy(string profileId, Enemy enemy)
        {
            if (LastUnderFireEnemy == enemy)
            {
                LastUnderFireEnemy = null;
                resetUnderFire();
            }
        }

        public void Update()
        {
            Health.Update();
            Location.Update();
            checkResetUnderFire();
        }

        public Action<Enemy> OnEnemyHeardFromPeace { get; set; }

        public void EnemyWasHeard(Enemy enemy)
        {
            if (Bot.HasEnemy || !BotOwner.Memory.IsPeace)
            {
                return;
            }
            OnEnemyHeardFromPeace?.Invoke(enemy);
        }

        public void SetUnderFire(IPlayer source, Vector3 position)
        {
            if (source != null)
            {
                try
                {
                    BotOwner.Memory.SetUnderFire(source);
                }
                catch { }

                LastUnderFireSource = source;
                UnderFireFromPosition = position;

                var enemy = Bot.EnemyController.GetEnemy(source.ProfileId, true);

                if (enemy != null)
                    LastUnderFireEnemy = enemy;
            }
        }

        private void checkResetUnderFire()
        {
            if (_nextCheckDeadTime < Time.time)
            {
                _nextCheckDeadTime = Time.time + 0.5f; 
                resetUnderFire();
            }
        }

        private void resetUnderFire()
        {
            if (BotOwner.Memory.IsUnderFire &&
                (LastUnderFireSource == null || LastUnderFireSource.HealthController.IsAlive == false))
            {
                _underFireTimeField.SetValue(BotOwner.Memory, Time.time);
            }
        }

        private float _nextCheckDeadTime;

        private static FieldInfo _underFireTimeField;

        public IPlayer LastUnderFireSource { get; private set; }
        public Enemy LastUnderFireEnemy { get; private set; }

        public void Dispose()
        {
            Bot.EnemyController.OnEnemyRemoved -= clearEnemy;
        }

        public Vector3 UnderFireFromPosition { get; set; }

        //public DecisionWrapper Decisions { get; private set; }

        public SAINExtract Extract { get; private set; } = new SAINExtract();

        public HealthTracker Health { get; private set; }

        public LocationTracker Location { get; private set; }

        static SAINMemoryClass()
        {
            _underFireTimeField = AccessTools.Field(typeof(BotMemoryClass), "float_4");
        }
    }
}