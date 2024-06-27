using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyController : SAINBase, ISAINClass
    {
        public event Action<Enemy> OnEnemyAdded;
        public event Action<string, Enemy> OnEnemyRemoved;
        public event Action<Enemy> OnEnemyForgotten;
        public event Action<Player> OnEnemyKilled;
        public event Action<bool> OnPeaceChanged;
        public event Action<Enemy, SAINSoundType, bool> OnEnemyHeard;

        public Enemy ActiveEnemy => _enemyChooser.ActiveEnemy;
        public Enemy LastEnemy => _enemyChooser.LastEnemy;

        public EnemyListsClass EnemyLists { get; private set; }
        public Dictionary<string, Enemy> Enemies { get; } = new Dictionary<string, Enemy>();
        public bool AtPeace => _atPeace && Time.time - _timePeaceStart > 3f;

        public SAINEnemyController(BotComponent sain) : base(sain)
        {
            EnemyLists = new EnemyListsClass(this);
            _enemyUpdater = new EnemyUpdaterClass(this);
            _enemyChooser = new EnemyChooserClass(this);
        }

        public void Init()
        {
            EnemyLists.Init();

            var knownEnemies = EnemyLists.GetEnemyList(EEnemyListType.Known);
            knownEnemies.OnListEmptyOrGetFirst += SetAtPeace;
            knownEnemies.OnListEmptyOrGetFirstHuman += changeHaveHumanActive;

            var enemiesInLOS = EnemyLists.GetEnemyList(EEnemyListType.InLineOfSight);
            enemiesInLOS.OnListEmptyOrGetFirstHuman += changeHumaninLOS;

            BotOwner.Memory.OnAddEnemy += enemyAdded;
            addAllEnemies();

            _enemyUpdater.Init();
            _enemyChooser.Init();
        }

        public bool ActiveHumanEnemy { get; private set; }

        public bool HumanEnemyInLineofSight { get; private set; }

        private void changeHaveHumanActive(bool hasHumanActive) {
            ActiveHumanEnemy = hasHumanActive;
        }

        private void changeHumaninLOS(bool humanInSight) {
            HumanEnemyInLineofSight = humanInSight;
        }

        private void addAllEnemies() {
            foreach (var person in BotOwner.BotsGroup.Enemies.Keys)
            {
                tryAddEnemy(person);
            }
        }

        public void Update()
        {
            compareEnemyLists();

            EnemyLists.Update();
            _enemyUpdater.Update();
            _enemyChooser.Update();

            updateDebug();
        }

        private void compareEnemyLists()
        {
            if (Enemies.Count != BotOwner.BotsGroup.Enemies.Count)
            {
                addAllEnemies();
            }
        }

        public void SetAtPeace(bool value)
        {
            bool wasAtPeace = _atPeace;
            _atPeace = value;

            if (wasAtPeace != _atPeace)
            {
                if (_atPeace)
                    _timePeaceStart = Time.time;

                OnPeaceChanged?.Invoke(_atPeace);
            }
        }

        private void updateDebug()
        {
            var enemy = ActiveEnemy;
            if (enemy != null)
            {
                if (SAINPlugin.DebugMode && SAINPlugin.DrawDebugGizmos)
                {
                    if (enemy.KnownPlaces.LastHeardPosition != null)
                    {
                        if (debugLastHeardPosition == null)
                        {
                            debugLastHeardPosition = DebugGizmos.Line(enemy.KnownPlaces.LastHeardPosition.Value, Bot.Position, Color.yellow, 0.01f, false, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdatePositionLine(enemy.KnownPlaces.LastHeardPosition.Value, Bot.Position, debugLastHeardPosition);
                    }
                    if (enemy.KnownPlaces.LastSeenPosition != null)
                    {
                        if (debugLastSeenPosition == null)
                        {
                            debugLastSeenPosition = DebugGizmos.Line(enemy.KnownPlaces.LastSeenPosition.Value, Bot.Position, Color.red, 0.01f, false, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdatePositionLine(enemy.KnownPlaces.LastSeenPosition.Value, Bot.Position, debugLastSeenPosition);
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

        public void Dispose()
        {
            EnemyLists?.Dispose();
            _enemyUpdater?.Dispose();
            _enemyChooser?.Dispose();

            BotMemoryClass memory = BotOwner?.Memory;
            if (memory != null)
            {
                memory.OnAddEnemy -= enemyAdded;
            }

            foreach (var enemy in Enemies)
            {
                enemy.Value?.Dispose();
            }
            Enemies?.Clear();
        }

        public Enemy GetEnemy(string profileID, bool mustBeActive)
        {
            if (!Enemies.TryGetValue(profileID, out Enemy enemy))
            {
                return null;
            }
            if (enemy == null || !enemy.IsValid)
            {
                Enemies.Remove(profileID);
                removeEnemyInfo(enemy);
                return null;
            }
            if (mustBeActive && !enemy.EnemyPerson.Active)
            {
                return null;
            }
            return enemy;
        }

        private void removeEnemyInfo(Enemy enemy)
        {
            if (enemy == null)
            {
                return;
            }
            if (enemy.EnemyIPlayer != null &&
                BotOwner.EnemiesController.EnemyInfos.ContainsKey(enemy.EnemyIPlayer))
            {
                BotOwner.EnemiesController.Remove(enemy.EnemyIPlayer);
                return;
            }

            EnemyInfo badInfo = null;
            foreach (var enemyInfo in BotOwner.EnemiesController.EnemyInfos.Values)
            {
                if (enemyInfo?.Person != null &&
                    enemyInfo.ProfileId == enemy.EnemyProfileId)
                {
                    badInfo = enemyInfo;
                    break;
                }
            }

            if (badInfo?.Person != null)
            {
                BotOwner.EnemiesController.Remove(badInfo.Person);
            }
        }

        public void ClearEnemy() => _enemyChooser.ClearEnemy();

        private void enemyKilled(Player player, IPlayer lastAggressor, DamageInfo lastDamageInfo, EBodyPart lastBodyPart)
        {
            if (player != null)
            {
                player.OnPlayerDead -= enemyKilled;

                if (lastAggressor != null &&
                    lastAggressor.ProfileId == Bot.ProfileId)
                {
                    OnEnemyKilled?.Invoke(player);
                }
            }
        }

        public void RemoveEnemy(string id)
        {
            if (Enemies.TryGetValue(id, out Enemy enemy))
            {
                OnEnemyRemoved?.Invoke(id, enemy);
                enemy.Dispose();
                Enemies.Remove(id);
                removeEnemyInfo(enemy);

                enemy.OnEnemyHeard -= enemyHeard;
                enemy.EnemyKnownChecker.OnEnemyKnownChanged -= enemyKnownChanged;
                if (enemy.EnemyPlayerComponent != null)
                {
                    enemy.EnemyPlayerComponent.OnComponentDestroyed -= RemoveEnemy;
                }
                //Logger.LogDebug($"Removed [{id}] from [{BotOwner?.name}'s] Enemy List");
            }
        }

        private void enemyHeard(Enemy enemy, SAINSoundType soundType, bool wasGunFire)
        {
            OnEnemyHeard?.Invoke(enemy, soundType, wasGunFire);
        }

        public Enemy CheckAddEnemy(IPlayer IPlayer)
        {
            return tryAddEnemy(IPlayer);
        }

        private void enemyAdded(IPlayer player)
        {
            tryAddEnemy(player);
        }

        private Enemy tryAddEnemy(IPlayer enemyPlayer)
        {
            if (enemyPlayer == null)
            {
                //Logger.LogDebug("Cannot add null player as an enemy.");
                return null;
            }
            if (!enemyPlayer.HealthController.IsAlive)
            {
                //Logger.LogDebug("Cannot add dead player as an enemy.");
                return null;
            }
            if (enemyPlayer.ProfileId == Bot.Player.ProfileId)
            {
                string debugString = $"Cannot add enemy {getBotInfo(enemyPlayer)} that matches this bot {getBotInfo(Player)}: ";
                debugString = findSourceDebug(debugString);
                Logger.LogDebug(debugString);
                return null;
            }

            if (enemyPlayer.IsAI && enemyPlayer.AIData?.BotOwner == null)
            {
                Logger.LogDebug("Cannot add ai as enemy with null Botowner");
                return null;
            }

            PlayerComponent enemyPlayerComponent = GameWorldComponent.Instance.PlayerTracker.GetPlayerComponent(enemyPlayer.ProfileId);
            if (enemyPlayerComponent == null)
            {
                Logger.LogDebug("Cannot add ai as enemy with null Player Component");
                return null;
            }

            if (Enemies.TryGetValue(enemyPlayer.ProfileId, out Enemy sainEnemy))
            {
                return sainEnemy;
            }

            if (!BotOwner.EnemiesController.EnemyInfos.TryGetValue(enemyPlayer, out EnemyInfo enemyInfo))
            {
                //string debugString = $"Player {enemyPlayer.Profile.Nickname} : Side: {enemyPlayer.Profile.Side} is not in Botowner's {Player.Profile.Nickname} : Side: {Player.Profile.Side} EnemyInfos dictionary.: ";
                //debugString = findSourceDebug(debugString);
                //Logger.LogDebug(debugString);
                return null;
            }

            return createEnemy(enemyPlayerComponent, enemyInfo);
        }

        private Enemy createEnemy(PlayerComponent enemyPlayerComponent, EnemyInfo enemyInfo)
        {
            Enemy enemy = new Enemy(Bot, enemyPlayerComponent, enemyInfo);
            enemy.Init();

            enemyPlayerComponent.OnComponentDestroyed += RemoveEnemy;
            enemyPlayerComponent.Player.OnPlayerDead += enemyKilled;
            enemy.EnemyKnownChecker.OnEnemyKnownChanged += enemyKnownChanged;
            enemy.OnEnemyHeard += enemyHeard;

            Enemies.Add(enemy.EnemyProfileId, enemy);
            OnEnemyAdded?.Invoke(enemy);

            return enemy;
        }

        private void enemyKnownChanged(Enemy enemy, bool known)
        {
            if (!known)
                OnEnemyForgotten?.Invoke(enemy);
        }

        private string getBotInfo(Player player)
        {
            return $" [{player.Profile.Nickname}, {player.Profile.Info.Settings.Role}, {player.ProfileId}] ";
        }

        private string getBotInfo(IPlayer player)
        {
            return $" [{player.Profile.Nickname}, {player.Profile.Info.Settings.Role}, {player.ProfileId}] ";
        }

        private string findSourceDebug(string debugString)
        {
            StackTrace stackTrace = new StackTrace();
            debugString += $" StackTrace: [{stackTrace.ToString()}]";
            return debugString;
        }

        public bool IsPlayerAnEnemy(string profileID)
        {
            return !profileID.IsNullOrEmpty() && Enemies.ContainsKey(profileID);
        }

        public bool IsPlayerFriendly(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                return false;
            }
            if (iPlayer.ProfileId == Bot.ProfileId)
            {
                return true;
            }

            if (Enemies.ContainsKey(iPlayer.ProfileId))
            {
                return false;
            }

            // Check that the source isn't from a member of the bot's group.
            if (iPlayer.AIData.IsAI && 
                BotOwner.BotsGroup.Contains(iPlayer.AIData.BotOwner))
            {
                return true;
            }

            // Checks if the player is not an active enemy and that they are a neutral party
            if (!BotOwner.BotsGroup.IsPlayerEnemy(iPlayer)
                && BotOwner.BotsGroup.Neutrals.ContainsKey(iPlayer))
            {
                return true;
            }

            // Check that the source isn't an ally
            if (BotOwner.BotsGroup.Allies.Contains(iPlayer))
            {
                return true;
            }

            if (iPlayer.IsAI &&
                iPlayer.AIData?.BotOwner?.Memory.GoalEnemy?.ProfileId == Bot.ProfileId)
            {
                return false;
            }

            if (!BotOwner.BotsGroup.Enemies.ContainsKey(iPlayer))
            {
                return true;
            }
            return false;
        }

        private readonly EnemyChooserClass _enemyChooser;
        private readonly EnemyUpdaterClass _enemyUpdater;

        private bool _atPeace;
        private float _timePeaceStart;
        private GameObject debugLastSeenPosition;
        private GameObject debugLastHeardPosition;
    }
}