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

        private static readonly float StartSurgery_SeenRecentTime = 90f;

        public bool ShallTrySurgery()
        {
            const float useSurgDist = 50f;
            bool useSurgery = false;

            if (_canStartSurgery)
            {
                var enemy = SAINBot.Enemy;
                if (enemy == null)
                {
                    if (SAINBot.CurrentTargetPosition == null)
                    {
                        useSurgery = true;
                    }
                    else if ((SAINBot.CurrentTargetPosition.Value - SAINBot.Position).sqrMagnitude > useSurgDist * useSurgDist)
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

        public bool _canStartSurgery => BotOwner.Medecine.SurgicalKit.ShallStartUse() && !BotOwner.Medecine.FirstAid.IsBleeding;

        private bool checkAllClear(bool surgeryStarted)
        {
            if (_nextCheckEnemiesTime < Time.time)
            {
                float timeAdd = surgeryStarted ? 0.5f : 0.1f;
                _nextCheckEnemiesTime = Time.time + timeAdd;

                float minPathDist = surgeryStarted ? 15f : 50f;
                float minTimeSinceSeen = surgeryStarted ? 3f : 60f;

                _allClear = checkEnemies(minPathDist, minTimeSinceSeen);
            }
            return _allClear;
        }

        private bool checkEnemies(float minPathDist, float minTimeSinceSeen)
        {
            bool allClear = true;
            var enemies = SAINBot.EnemyController.Enemies;
            foreach (var enemy in enemies.Values)
            {
                if (!checkThisEnemy(enemy, minPathDist, minTimeSinceSeen))
                {
                    allClear = false;
                    break;
                }
            }
            return allClear;
        }

        private bool checkThisEnemy(SAINEnemy enemy, float minPathDist, float minTimeSinceSeen)
        {
            if (enemy?.EnemyPlayer?.HealthController.IsAlive == true
                && (enemy.Seen || enemy.Heard)
                && enemy.TimeSinceLastKnownUpdated < 360f)
            {
                if (enemy.IsVisible)
                {
                    return false;
                }
                if (enemy.Seen && enemy.TimeSinceSeen < minTimeSinceSeen)
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