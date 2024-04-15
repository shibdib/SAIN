using BepInEx.Logging;
using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.BaseClasses;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyController : SAINBase, ISAINClass
    {
        public SAINEnemyController(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        private void UpdateEnemies()
        {
            foreach (var keyPair in Enemies)
            {
                string id = keyPair.Key;
                var enemy = keyPair.Value;
                var enemyPerson = enemy?.EnemyPerson;

                if (enemyPerson?.PlayerNull == true)
                {
                    EnemyIDsToRemove.Add(id);
                }
                // Redundant Checks
                // Common checks between PMC and bots
                else if (enemy == null || enemy.EnemyPlayer == null || enemy.EnemyPlayer.HealthController?.IsAlive == false)
                {
                    EnemyIDsToRemove.Add(id);
                }
                // Checks specific to bots
                else if (enemy.EnemyPlayer.IsAI && (
                    enemy.EnemyPlayer.AIData?.BotOwner == null ||
                    enemy.EnemyPlayer.AIData.BotOwner.ProfileId == BotOwner.ProfileId ||
                    enemy.EnemyPlayer.AIData.BotOwner.BotState != EBotState.Active))
                {
                    EnemyIDsToRemove.Add(id);
                }
                else
                {
                    enemy.Update();
                    if (enemy.LastHeardPosition != null)
                    {
                        DebugGizmos.Line(enemy.LastHeardPosition.Value, SAIN.Position, Color.yellow, 0.01f, false, Time.deltaTime, true);
                    }
                    if (enemy.LastSeenPosition != null)
                    {
                        DebugGizmos.Line(enemy.LastSeenPosition.Value, SAIN.Position, Color.red, 0.01f, false, Time.deltaTime, true);
                    }
                }
            }

            foreach (string idToRemove in EnemyIDsToRemove)
            {
                Enemies.Remove(idToRemove);
            }

            EnemyIDsToRemove.Clear();
        }

        public SAINEnemy ClosestHeardEnemy { get; private set; }

        public SAINEnemy FindClosestHeardEnemy()
        {
            if (findClosestHeardTimer < Time.time)
            {
                findClosestHeardTimer = Time.time + 0.5f;
                float closestEnemyDist = float.MaxValue;
                ClosestHeardEnemy = null;
                foreach (var enemy in SAIN.EnemyController.Enemies)
                {
                    float enemyDist = (enemy.Value.EnemyPosition - SAIN.Position).sqrMagnitude;
                    if (enemy.Value?.HeardRecently == true && enemyDist < closestEnemyDist)
                    {
                        closestEnemyDist = enemyDist;
                        ClosestHeardEnemy = enemy.Value;
                    }
                }
            }
            return ClosestHeardEnemy;
        }

        private float findClosestHeardTimer;

        public void Update()
        {
            UpdateEnemies();
            CheckAddEnemy();
            if (ClosestHeardEnemy != null && ClosestHeardEnemy.HeardRecently == false)
            {
                ClosestHeardEnemy = null;
            }
        }

        public void Dispose()
        {
            Enemies?.Clear();
        }

        public SAINEnemy GetEnemy(string id)
        {
            if (Enemies.ContainsKey(id))
            {
                return Enemies[id];
            }
            return null;
        }

        public bool HasEnemy => ActiveEnemy?.EnemyPerson?.IsActive == true;

        public SAINEnemy ActiveEnemy { get; private set; }

        public void ClearEnemy()
        {
            ActiveEnemy = null;
            ClosestHeardEnemy = null;
        }

        public void CheckAddEnemy()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            IPlayer IPlayer = goalEnemy?.Person;
            
            bool addEnemy = goalEnemy != null 
                && CheckPlayerNull(goalEnemy.Person) == false;

            if (addEnemy)
            {
                string id = IPlayer.ProfileId;

                // Check if the dictionary contains a previous SAINEnemy
                if (!Enemies.ContainsKey(id))
                {
                    SAINPersonClass enemySAINPerson = GetSAINPerson(IPlayer);
                    Enemies.Add(id, new SAINEnemy(SAIN, enemySAINPerson, goalEnemy));
                }
                ActiveEnemy = Enemies[id];
            }
            else
            {
                ActiveEnemy = null;
            }
        }

        public SAINEnemy CheckAddEnemy(IPlayer IPlayer)
        {
            if (CheckPlayerNull(IPlayer) == false)
            {
                string id = IPlayer.ProfileId;

                if (BotOwner.BotsGroup.IsPlayerEnemy(IPlayer) && BotOwner.EnemiesController.IsEnemy(Player) == false)
                {
                    BotOwner.BotsGroup.AddEnemy(Player, EBotEnemyCause.addPlayer);
                }

                if (!Enemies.ContainsKey(id) && BotOwner.EnemiesController.EnemyInfos.ContainsKey(IPlayer))
                {
                    EnemyInfo enemyInfo = BotOwner.EnemiesController.EnemyInfos[IPlayer];
                    SAINPersonClass enemySAINPerson = GetSAINPerson(IPlayer);
                    SAINEnemy enemy = new SAINEnemy(SAIN, enemySAINPerson, enemyInfo);
                    Enemies.Add(id, enemy);
                    return enemy;
                }
                if (Enemies.ContainsKey(id))
                {
                    return Enemies[id];
                }
                if (!BotOwner.EnemiesController.EnemyInfos.ContainsKey(IPlayer))
                {
                    //Logger.LogError("Player is not in bots enemy Infos!");
                }
            }
            return null;
        }

        private bool CheckPlayerNull(IPlayer player)
        {
            bool isNull = false;
            if (player == null)
            {
                isNull = true;
            }
            else if (player.IsAI && (player.AIData?.BotOwner == null || player.AIData.BotOwner.BotState != EBotState.Active))
            {
                isNull = true;
            }
            else if (player.ProfileId == SAIN.ProfileId)
            {
                isNull = true;
            }
            else if (!player.HealthController.IsAlive)
            {
                isNull = true;
            }
            return isNull;
        }

        public bool IsMainPlayerActiveEnemy()
        {
            return ActiveEnemy != null && ActiveEnemy.EnemyIPlayer != null && ActiveEnemy.EnemyIPlayer.IsYourPlayer;
        }

        public bool IsMainPlayerAnEnemy()
        {
            Player mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
            if (mainPlayer != null)
            {
                string profileID = mainPlayer.ProfileId;
                if (IsPlayerAnEnemy(profileID))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsMainPlayerLookAtMe()
        {
            if (GameWorldHandler.SAINMainPlayer != null)
            {
                var person = GameWorldHandler.SAINMainPlayer.SAINPerson;
                Vector3 mainPlayerHeadPos = person.Transform.Head;

                Vector3 lookDir = person.Transform.LookDirection;

                Vector3 botChestPos = SAIN.Person.Transform.Chest;
                Vector3 botDir = (botChestPos - mainPlayerHeadPos);

                if (Vector3.Dot(lookDir, botDir.normalized) > 0.75f)
                {
                    if (CheckMainPlayerVisionTimer < Time.time)
                    {
                        CheckMainPlayerVisionTimer = Time.time + 1f;
                        MainPlayerWasLookAtMe = !Physics.Raycast(mainPlayerHeadPos, botDir, botDir.magnitude, LayerMaskClass.HighPolyWithTerrainMask);
                    }
                }
                else
                {
                    MainPlayerWasLookAtMe = false;
                }
            }
            else
            {
                MainPlayerWasLookAtMe = false;
            }
            return MainPlayerWasLookAtMe;
        }

        private float CheckMainPlayerVisionTimer;
        private bool MainPlayerWasLookAtMe;

        public bool IsPlayerAnEnemy(string profileID)
        {
            return Enemies.ContainsKey(profileID) && Enemies[profileID] != null;
        }

        private static SAINPersonClass GetSAINPerson(IPlayer IPlayer)
        {
            Player player = Singleton<GameWorld>.Instance?.GetAlivePlayerByProfileID(IPlayer.ProfileId);

            if (player == null)
            {
                Logger.LogError("player Null!");
            }

            SAINPersonComponent _SAINPersonComponent = player?.gameObject.GetOrAddComponent<SAINPersonComponent>();
            SAINPersonClass enemySAINPerson = _SAINPersonComponent?.SAINPerson;

            if (enemySAINPerson == null)
            {
                Logger.LogError("enemySAINPerson Null!");
                return new SAINPersonClass(IPlayer);
            }

            return enemySAINPerson;
        }

        public readonly Dictionary<string, SAINEnemy> Enemies = new Dictionary<string, SAINEnemy>();
        private readonly List<string> EnemyIDsToRemove = new List<string>();
    }
}
