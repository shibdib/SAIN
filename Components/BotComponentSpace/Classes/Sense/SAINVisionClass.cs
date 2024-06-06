using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Sense;
using System.Reflection;
using UnityEngine;

// Found in Botowner.Looksensor
using LookAllData = GClass522;
using EnemyVisionCheck = GClass501;
using ShootPosition = GClass524;
using System.Collections.Generic;
using System;
using SAIN.SAINComponent.Classes.Enemy;

namespace SAIN.SAINComponent.Classes
{
    public class SAINVisionClass : SAINBase, ISAINClass
    {
        public SAINVisionClass(BotComponent component) : base(component)
        {
            FlashLightDazzle = new FlashLightDazzleClass(component);
            BotLook = new SAINBotLookClass(component);
        }

        public void Init()
        {
        }

        public void Update()
        {
            FlashLightDazzle.CheckIfDazzleApplied(Bot.Enemy);
        }

        public static float GetRatioPartsVisible(EnemyInfo enemyInfo, out int visibleCount)
        {
            var enemyParts = enemyInfo.AllActiveParts;
            int partCount = enemyParts.Count + 1;
            visibleCount = 0;

            var bodyPartData = enemyInfo.BodyData().Value;
            if (bodyPartData.IsVisible || bodyPartData.LastVisibilityCastSucceed)
            {
                visibleCount++;
            }

            foreach (var part in enemyParts)
            {
                if (part.Value.LastVisibilityCastSucceed || part.Value.IsVisible)
                {
                    visibleCount++;
                }
            }


            if (_nextLogTime < Time.time && visibleCount > 0 && !enemyInfo.Person.IsAI)
            {
                _nextLogTime = Time.time + 0.1f;
                //Logger.LogDebug($"Visible Ratio for Enemy Parts: [{(float)visibleCount / (float)partCount}] for Bot [{enemyInfo.Owner.name}]");
            }

            return (float)visibleCount / (float)partCount;
        }

        private static float _nextLogTime;

        public void Dispose()
        {
        }

        public FlashLightDazzleClass FlashLightDazzle { get; private set; }

        public SAINBotLookClass BotLook { get; private set; }
    }

    public class SAINBotLookClass : SAINBase
    {
        public SAINBotLookClass(BotComponent component) : base(component)
        {
            _lookAllData = new LookAllData();
        }

        private readonly LookAllData _lookAllData;

        public int UpdateLook(bool forAI)
        {
            int numUpdated = 0;
            if (BotOwner.LeaveData != null &&
                !BotOwner.LeaveData.LeaveComplete)
            {
                _lookAllData.Reset();

                numUpdated = UpdateLookForEnemies(_lookAllData, forAI);

                LookAllData data = _lookAllData;
                for (int i = 0; i < data.reportsData.Count; i++)
                {
                    EnemyVisionCheck enemyVision = data.reportsData[i];
                    BotOwner.BotsGroup.ReportAboutEnemy(enemyVision.enemy, enemyVision.VisibleOnlyBuSence);
                }
                if (data.reportsData.Count > 0)
                {
                    BotOwner.Memory.SetLastTimeSeeEnemy();
                }
                if (data.shallRecalcGoal)
                {
                    BotOwner.CalcGoal();
                }
                _lookAllData.Reset();
            }
            return numUpdated;
        }

        private int UpdateLookForEnemies(LookAllData lookAll, bool forAI)
        {
            int updated = 0;
            foreach (SAINEnemy enemy in Bot.EnemyController.Enemies.Values)
            {
                if (enemy?.IsValid == true &&
                    enemy.NextCheckLookTime < Time.time &&
                    enemy.IsAI == forAI)
                {
                    updated++;
                    float timeAdd = enemy.IsAI ? 0.2f : 0.1f;
                    if (!enemy.EnemyPerson.IsActive)
                    {
                        //timeAdd = 0.5f;
                    }
                    timeAdd *= UnityEngine.Random.Range(0.66f, 1.33f);
                    enemy.NextCheckLookTime = Time.time + timeAdd;
                    enemy.EnemyInfo.CheckLookEnemy(lookAll);
                }
            }
            return updated;
        }
    }
}