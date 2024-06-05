using BepInEx.Logging;
using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.SubComponents;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.Helpers;

namespace SAIN.SAINComponent.Classes
{
    public class SAINMedical : SAINBase, ISAINClass
    {
        public EHitReaction HitReaction { get; private set; }

        public SAINMedical(BotComponent sain) : base(sain)
        {
            Surgery = new BotSurgery(sain);
        }

        public BotSurgery Surgery { get; private set; }

        public void TryCancelHeal()
        {
            if (_nextCancelTime < Time.time)
            {
                _nextCancelTime = Time.time + _cancelFreq;
                BotOwner.Medecine?.FirstAid?.CancelCurrent();
            }
        }

        private float _nextCancelTime;
        private float _cancelFreq = 1f;

        public void Init()
        {
            if (Player != null)
            {
                Player.BeingHitAction += GetHit;
            }
            Surgery.Init();
        }

        public void Update()
        {
            Surgery.Update();
        }

        public void Dispose()
        {
            if (Player != null)
            {
                Player.BeingHitAction -= GetHit;
            }
            Surgery.Dispose();
        }

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
        }
    }

    public class BotSurgery : SAINBase, ISAINClass
    {
        public BotSurgery(BotComponent sain) : base(sain)
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

        public bool SurgeryStarted
        {
            get
            {
                return _surgeryStarted;
            }
            set
            {
                if (_surgeryStarted != value && value)
                {
                    SurgeryStartTime = Time.time;
                }
                _surgeryStarted = value;
            }
        }

        public float SurgeryStartTime { get; private set; }

        private bool _surgeryStarted;

        public bool AreaClearForSurgery
        {
            get
            {
                if (_nextCheckClearTime < Time.time)
                {
                    _nextCheckClearTime = Time.time + _checkClearFreq;
                    _areaClear = ShallTrySurgery();
                }
                return _areaClear;
            }
        }

        private bool _areaClear;
        private float _nextCheckClearTime;
        private float _checkClearFreq = 0.5f;

        public bool ShallTrySurgery()
        {
            const float useSurgDist = 100f;
            bool useSurgery = false;

            if (_canStartSurgery)
            {
                var enemy = Bot.Enemy;
                if (Bot.EnemyController.NoEnemyContact)
                {
                    if (Bot.CurrentTargetPosition == null)
                    {
                        useSurgery = true;
                    }
                    else if ((Bot.CurrentTargetPosition.Value - Bot.Position).sqrMagnitude > useSurgDist.Sqr())
                    {
                        useSurgery = true;
                    }
                }
                else
                {
                    useSurgery = checkAllClear(SurgeryStarted);
                }
            }

            return useSurgery;
        }

        public bool _canStartSurgery => BotOwner?.Medecine?.SurgicalKit?.ShallStartUse() == true && BotOwner?.Medecine?.FirstAid?.IsBleeding == false;

        private bool checkAllClear(bool surgeryStarted)
        {
            if (_nextCheckEnemiesTime < Time.time)
            {
                float timeAdd = surgeryStarted ? 0.5f : 0.1f;
                _nextCheckEnemiesTime = Time.time + timeAdd;

                float minPathDist = surgeryStarted ? 50f : 100f;
                float minTimeSinceLastKnown = surgeryStarted ? 30f : 60f;

                _allClear = checkEnemies(minPathDist, minTimeSinceLastKnown);
            }
            return _allClear;
        }

        private bool checkEnemies(float minPathDist, float minTimeSinceLastKnown)
        {
            bool allClear = true;
            var enemies = Bot.EnemyController.Enemies;
            foreach (var enemy in enemies.Values)
            {
                if (!checkThisEnemy(enemy, minPathDist, minTimeSinceLastKnown))
                {
                    allClear = false;
                    break;
                }
            }
            return allClear;
        }

        private bool checkThisEnemy(SAINEnemy enemy, float minPathDist, float minTimeSinceLastKnown)
        {
            if (enemy?.EnemyPlayer?.HealthController.IsAlive == true
                && (enemy.Seen || enemy.Heard)
                && enemy.TimeSinceLastKnownUpdated < 360f)
            {
                if (enemy.IsVisible)
                {
                    return false;
                }
                if (enemy.LastKnownPosition == null)
                {
                    return true;
                }
                if (enemy.TimeSinceLastKnownUpdated < minTimeSinceLastKnown)
                {
                    return false;
                }
                if (enemy.Path.PathDistance < minPathDist)
                {
                    return false;
                }
            }
            return true;
        }

        private bool _allClear;
        private float _nextCheckEnemiesTime;
    }
}