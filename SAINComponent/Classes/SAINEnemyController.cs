using BepInEx.Logging;
using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent;
using SAIN.SAINComponent.BaseClasses;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyController : SAINBase, ISAINClass
    {
        public bool HasEnemy => ActiveEnemy?.EnemyPerson?.IsActive == true;
        public bool HasLastEnemy => LastEnemy?.EnemyPerson?.IsActive == true;
        public SAINEnemy ActiveEnemy { get; private set; }
        public SAINEnemy LastEnemy { get; private set; }
        public bool IsHumanPlayerActiveEnemy => ActiveEnemy != null && ActiveEnemy.EnemyIPlayer != null && ActiveEnemy.EnemyIPlayer.AIData?.IsAI == false;

        public readonly Dictionary<string, SAINEnemy> Enemies = new Dictionary<string, SAINEnemy>();

        public SAINEnemyController(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            BotMemoryClass memory = BotOwner?.Memory;
            if (memory != null)
            {
                memory.OnGoalEnemyChanged += CheckAddEnemy;
                memory.OnAddEnemy += AddEnemy;
            }
            else
            {
                Logger.LogAndNotifyError("Botowner Null in EnemyController Init");
            }
        }

        public void Update()
        {
            CheckAddEnemy();
            UpdateEnemies();
            UpdateDebug();
        }

        private readonly Dictionary<string, SAINEnemy> _localEnemiesList = new Dictionary<string, SAINEnemy>();

        private void UpdateEnemies()
        {
            var activeEnemy = ActiveEnemy;
            if (activeEnemy != null)
            {
                if (activeEnemy.IsValid)
                {
                    activeEnemy.Update();
                }
            }
            removeInvalidEnemies();
            if (_enemyUpdateCoroutine == null)
            {
                _enemyUpdateCoroutine = SAIN.StartCoroutine(updateValidEnemies(5));
            }
        }

        private void removeInvalidEnemies()
        {
            foreach (var keyPair in Enemies)
            {
                var enemy = keyPair.Value;
                if (enemy?.IsValid != true)
                {
                    _idsToRemove.Add(keyPair.Key);
                }
            }
            foreach (var id in _idsToRemove)
            {
                RemoveEnemy(id);
            }
            _idsToRemove.Clear();
        }

        private readonly List<string> _idsToRemove = new List<string>();

        private Coroutine _enemyUpdateCoroutine;

        private IEnumerator updateValidEnemies(int maxPerFrame)
        {
            while (true)
            {
                int count = 0;
                _localEnemiesList.AddRange(Enemies);
                foreach (var keyPair in _localEnemiesList)
                {
                    var enemy = keyPair.Value;
                    if (enemy?.IsValid == true && !enemy.IsCurrentEnemy)
                    {
                        enemy.Update();
                        count++;
                        if (count >= maxPerFrame)
                        {
                            count = 0;
                            yield return null;
                        }
                    }
                }
                _localEnemiesList.Clear();
                yield return null;
            }
        }

        private void UpdateDebug()
        {
            if (ActiveEnemy != null)
            {
                if (SAINPlugin.DebugMode && SAINPlugin.DrawDebugGizmos)
                {
                    if (ActiveEnemy.LastHeardPosition != null)
                    {
                        if (debugLastHeardPosition == null)
                        {
                            debugLastHeardPosition = DebugGizmos.Line(ActiveEnemy.LastHeardPosition.Value, SAIN.Position, Color.yellow, 0.01f, false, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdatePositionLine(ActiveEnemy.LastHeardPosition.Value, SAIN.Position, debugLastHeardPosition);
                    }
                    if (ActiveEnemy.LastSeenPosition != null)
                    {
                        if (debugLastSeenPosition == null)
                        {
                            debugLastSeenPosition = DebugGizmos.Line(ActiveEnemy.LastSeenPosition.Value, SAIN.Position, Color.red, 0.01f, false, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdatePositionLine(ActiveEnemy.LastSeenPosition.Value, SAIN.Position, debugLastSeenPosition);
                    }
                }
                else if (debugLastHeardPosition != null || debugLastSeenPosition != null)
                {
                    GameObject.Destroy(debugLastHeardPosition);
                    GameObject.Destroy(debugLastSeenPosition);
                }
            }
            else if (debugLastHeardPosition != null || debugLastSeenPosition != null)
            {
                GameObject.Destroy(debugLastHeardPosition);
                GameObject.Destroy(debugLastSeenPosition);
            }
        }

        private GameObject debugLastSeenPosition;
        private GameObject debugLastHeardPosition;

        public void Dispose()
        {
            BotMemoryClass memory = BotOwner?.Memory;
            if (memory != null)
            {
                memory.OnAddEnemy -= AddEnemy;
                memory.OnGoalEnemyChanged -= CheckAddEnemy;
            }

            foreach (var enemy in Enemies)
            {
                enemy.Value?.Dispose();
            }
            Enemies?.Clear();
        }

        public SAINEnemy GetEnemy(string id)
        {
            if (Enemies.TryGetValue(id, out SAINEnemy enemy))
            {
                return enemy;
            }
            return null;
        }

        public void ClearEnemy()
        {
            setActiveEnemy(null);
        }

        public void RemoveEnemy(string id)
        {
            removeActiveEnemy(id);
            removeLastEnemy(id);
            if (Enemies.TryGetValue(id, out SAINEnemy enemy))
            {
                enemy.Dispose();
                Enemies.Remove(id);
                //Logger.LogDebug($"Removed [{id}] from [{BotOwner?.name}'s] Enemy List");
            }
        }

        private void removeActiveEnemy(string id)
        {
            if (ActiveEnemy?.EnemyPerson != null
                && ActiveEnemy.EnemyPerson.ProfileId == id)
            {
                ActiveEnemy = null;
            }
        }

        private void removeLastEnemy(string id)
        {
            if (LastEnemy?.EnemyPerson != null
                && LastEnemy.EnemyPerson.ProfileId == id)
            {
                LastEnemy = null;
            }
        }


        private void CheckAddEnemy(BotOwner botOwner)
        {
            _newEnemy = true;
        }

        private bool _newEnemy;

        private void CheckAddEnemy()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null)
            {
                if (_newEnemy)
                {
                    _newEnemy = false;
                    IPlayer iPlayer = goalEnemy?.Person;
                    AddEnemy(iPlayer);
                    SAINEnemy sainEnemy = CheckAddEnemy(iPlayer);
                    if (sainEnemy != null)
                    {
                        setActiveEnemy(sainEnemy);
                    }
                }
            }
            else if (ActiveEnemy != null)
            {
                setActiveEnemy(null);
            }
        }

        private void setActiveEnemy(SAINEnemy activeEnemy)
        {
            setLastEnemy(activeEnemy);
            ActiveEnemy = activeEnemy;
        }

        private void setLastEnemy(SAINEnemy activeEnemy)
        {
            bool nullActiveEnemy = activeEnemy?.EnemyPerson?.IsActive == true;
            bool nullLastEnemy = LastEnemy?.EnemyPerson?.IsActive == true;

            if (!nullLastEnemy && nullActiveEnemy)
            {
                return;
            }
            if (nullLastEnemy && !nullActiveEnemy)
            {
                LastEnemy = activeEnemy;
                return;
            }
            if (!AreEnemiesSame(activeEnemy, LastEnemy))
            {
                LastEnemy = activeEnemy;
                return;
            }
        }

        public bool AreEnemiesSame(SAINEnemy a, SAINEnemy b)
        {
            IPlayer lastPlayer = a?.EnemyIPlayer;
            IPlayer activePlayer = b?.EnemyIPlayer;
            return lastPlayer != null
                && activePlayer != null
                && lastPlayer.ProfileId == activePlayer.ProfileId;
        }

        public SAINEnemy CheckAddEnemy(IPlayer IPlayer)
        {
            AddEnemy(IPlayer);
            if (IPlayer != null 
                && Enemies.TryGetValue(IPlayer.ProfileId, out SAINEnemy enemy))
            {
                return enemy;
            }
            return null;
        }

        private void AddEnemy(IPlayer player)
        {
            if (player == null 
                || !player.HealthController.IsAlive
                || Enemies.ContainsKey(player.ProfileId) 
                || player.ProfileId == SAIN.ProfileId 
                || player.IsAI && player.AIData?.BotOwner == null)
            {
                return;
            }

            if (BotOwner.EnemiesController.EnemyInfos.TryGetValue(player, out EnemyInfo enemyInfo))
            {
                SAINPersonClass enemySAINPerson = GetSAINPerson(player);
                SAINEnemy newEnemy = new SAINEnemy(SAIN, enemySAINPerson, enemyInfo);
                player.HealthController.DiedEvent += newEnemy.DeleteInfo;
                Enemies.Add(player.ProfileId, newEnemy);
                //Logger.LogDebug($"Added [{player.ProfileId}] to [{BotOwner?.name}'s] Enemy List");
            }
        }

        public bool IsHumanPlayerLookAtMe(out Player lookingPlayer)
        {
            if (CheckMainPlayerVisionTimer < Time.time)
            {
                CheckMainPlayerVisionTimer = Time.time + 0.25f;
                MainPlayerWasLookAtMe = false;
                _lookingPlayer = null;

                var gameworld = GameWorldHandler.SAINGameWorld?.GameWorld;
                if (gameworld != null)
                {
                    var players = gameworld.AllAlivePlayersList;
                    if (players != null)
                    {
                        foreach (var player in players)
                        {
                            if (SAIN.Memory.VisiblePlayers.Contains(player))
                            {
                                Vector3 lookDir = player.LookDirection;
                                Vector3 playerHeadPos = player.MainParts[BodyPartType.head].Position;

                                Vector3 botChestPos = SAIN.Person.Transform.Chest;
                                Vector3 botDir = (botChestPos - playerHeadPos);

                                if (Vector3.Dot(lookDir, botDir.normalized) > 0.75f)
                                {
                                    MainPlayerWasLookAtMe = true;
                                    _lookingPlayer = player;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            lookingPlayer = _lookingPlayer;
            return MainPlayerWasLookAtMe;
        }

        private Player _lookingPlayer;
        private float CheckMainPlayerVisionTimer;
        private bool MainPlayerWasLookAtMe;

        public bool IsPlayerAnEnemy(string profileID)
        {
            return Enemies.ContainsKey(profileID) && Enemies[profileID] != null;
        }

        private static SAINPersonClass GetSAINPerson(IPlayer IPlayer)
        {
            if (IPlayer == null)
            {
                return null;
            }

            Player player = Singleton<GameWorld>.Instance?.GetAlivePlayerByProfileID(IPlayer.ProfileId);
            if (player == null)
            {
                return null;
            }

            SAINPersonComponent _SAINPersonComponent = player?.gameObject.GetOrAddComponent<SAINPersonComponent>();

            if (_SAINPersonComponent?.SAINPerson == null)
            {
                _SAINPersonComponent?.Init(IPlayer);
            }

            return _SAINPersonComponent?.SAINPerson;
        }
    }
}
