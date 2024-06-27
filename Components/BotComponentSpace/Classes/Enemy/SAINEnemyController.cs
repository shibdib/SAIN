using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyController : SAINBase, ISAINClass
    {
        public bool HasEnemy => ActiveEnemy?.EnemyPerson?.Active == true;
        public bool HasLastEnemy => LastEnemy?.EnemyPerson?.Active == true;
        public Enemy ActiveEnemy { get; private set; }
        public Enemy LastEnemy { get; private set; }
        public System.Action<Player> OnEnemyKilled { get; set; }

        public readonly Dictionary<string, Enemy> Enemies = new Dictionary<string, Enemy>();
        public bool NoEnemyContact => _atPeace && Time.time - _timePeaceStart > 3f;
        public bool ActiveHumanEnemy { get; private set; }

        public readonly List<Enemy> ActiveEnemies = new List<Enemy>();
        public List<string> HumansInLineOfSight { get; } = new List<string>();
        public Action<string> OnEnemyRemoved { get; set; }
        public Action<Enemy, float> OnEnemyForgotten { get; set; }

        public SAINEnemyController(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            Bot.CoroutineManager.Add(enemyVisionCheck(), nameof(enemyVisionCheck));
            BotOwner.Memory.OnAddEnemy += AddEnemy;
            allAllEnemies();
        }

        private void allAllEnemies()
        {
            foreach (var person in BotOwner.BotsGroup.Enemies.Keys)
            {
                AddEnemy(person);
            }
        }

        private IEnumerator enemyVisionCheck()
        {
            while (true)
            {
                int enemiesChecked = 0;
                int enemiesInLOS = 0;
                int enemiesVisible = 0;

                foreach (var enemy in Enemies.Values)
                {
                    if (enemy?.IsValid == true)
                    {
                        enemy.Vision.EnemyVisionChecker.CheckVision(out bool didCheck);

                        if (!didCheck)
                            continue;

                        enemiesChecked++;

                        if (enemy.InLineOfSight)
                            enemiesInLOS++;

                        if (enemy.IsVisible)
                            enemiesVisible++;
                    }
                }

                if (_nextlog < Time.time)
                {
                    _nextlog = Time.time + 10;
                    //Logger.LogDebug($"Checked {enemiesChecked} enemies out of {Enemies.Count} total enemies. Total in LOS [{enemiesInLOS}] Total visible [{enemiesVisible}] Total EnemyInfos {BotOwner.BotsGroup.Enemies.Count}");
                    //Logger.LogDebug($"Active Enemy: {ActiveEnemy?.EnemyPerson.Name}");
                    //Logger.LogDebug($"Current Decision: {Bot.Decision.CurrentSoloDecision}");
                }
                yield return null;
            }
        }

        private float _nextlog;

        private readonly List<Enemy> _localList = new List<Enemy>();

        public void Update()
        {
            compareEnemyLists();
            updateAllEnemies();
            removeInvalidEnemies();
            checkHumanLOS();
            AssignActiveEnemy();
            checkActiveEnemies();
            checkIsAtPeace();
            updateDebug();

            if (ActiveEnemy != null &&
                !ActiveEnemy.EnemyPerson.Active)
            {
                setActiveEnemy(null);
            }

            checkDiscrepency();
        }

        private void compareEnemyLists()
        {
            if (Enemies.Count != BotOwner.BotsGroup.Enemies.Count)
            {
                allAllEnemies();
            }
        }

        private void checkIsAtPeace()
        {
            bool wasAtPeace = _atPeace;
            if (ActiveEnemies.Count > 0)
            {
                if (_atPeace)
                    _atPeace = false;
            }
            else if (!_atPeace)
            {
                _atPeace = true;
                _timePeaceStart = Time.time;
            }
        }

        private void checkDiscrepency()
        {
            EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null && ActiveEnemy == null)
            {
                if (_nextLogTime < Time.time)
                {
                    _nextLogTime = Time.time + 1f;
                    Logger.LogError("Bot's Goal Enemy is not null, but SAIN enemy is null.");
                    if (goalEnemy.Person == null)
                    {
                        Logger.LogError("Bot's Goal Enemy Person is null");
                        return;
                    }
                    if (goalEnemy.ProfileId == Bot.ProfileId)
                    {
                        Logger.LogError("goalEnemy.ProfileId == SAINBot.ProfileId");
                        return;
                    }
                    if (goalEnemy.ProfileId == Bot.Player.ProfileId)
                    {
                        Logger.LogError("goalEnemy.ProfileId == SAINBot.Player.ProfileId");
                        return;
                    }
                    if (goalEnemy.ProfileId == Bot.BotOwner.ProfileId)
                    {
                        Logger.LogError("goalEnemy.ProfileId == SAINBot.Player.ProfileId");
                        return;
                    }
                    Enemy sainEnemy = GetEnemy(goalEnemy.ProfileId, true);
                    if (sainEnemy != null)
                    {
                        setActiveEnemy(sainEnemy);
                        Logger.LogError("Got SAINEnemy from goalEnemy.ProfileId");
                        return;
                    }
                    sainEnemy = CheckAddEnemy(goalEnemy.Person);
                    if (sainEnemy != null)
                    {
                        setActiveEnemy(sainEnemy);
                        Logger.LogError("Got SAINEnemy from goalEnemy.Person");
                        return;
                    }
                }
            }
        }

        private void checkActiveEnemies()
        {
            if (_nextCheckActiveTime > Time.time)
            {
                return;
            }

            _nextCheckActiveTime = Time.time + 0.25f;

            if (ActiveEnemies.Count > 0)
            {
                ActiveEnemies.RemoveAll(x => x == null || !x.IsValid);
            }

            ActiveHumanEnemy = false;

            foreach (Enemy enemy in Enemies.Values)
            {
                bool inList = ActiveEnemies.Contains(enemy);
                bool activeThreat = enemy.ActiveThreat;

                if (activeThreat && !inList)
                {
                    ActiveEnemies.Add(enemy);
                }
                else if (!activeThreat && inList)
                {
                    ActiveEnemies.Remove(enemy);
                }

                if (!ActiveHumanEnemy &&
                    activeThreat &&
                    !enemy.IsAI)
                {
                    ActiveHumanEnemy = true;
                }
            }

            if (_nextLogActiveTime < Time.time && ActiveEnemies.Count > 0)
            {
                _nextLogActiveTime = Time.time + 10f;
                //Logger.LogDebug($"[{SAINBot.name}] Active Enemies: [{ActiveEnemies.Count}]");
                foreach (var enemy in ActiveEnemies)
                {
                    //Logger.LogDebug($"{SAINBot.Player.name} : {enemy.EnemyPlayer.Profile.Nickname} : {enemy.EnemyPlayer.name} : {enemy.Player.ProfileId} : Time Since Active: [{enemy.TimeSinceActive}]");
                }
            }
        }

        private Enemy checkIfAnyEnemyVisible()
        {
            foreach (var enemy in Enemies.Values)
            {
                if (enemy.IsVisible)
                {
                    return enemy;
                }
            }
            return null;
        }

        private void updateAllEnemies()
        {
            foreach (var enemy in Enemies.Values)
            {
                enemy.Update();
            }
        }

        private void checkHumanLOS()
        {
            if (_nextCheckHumanTime > Time.time)
            {
                return;
            }
            _nextCheckHumanTime = Time.time + _checkHumanFreq;

            HumansInLineOfSight.Clear();
            foreach (var enemy in Enemies.Values)
            {
                if (!enemy.IsAI &&
                    enemy.InLineOfSight)
                {
                    HumansInLineOfSight.Add(enemy.EnemyProfileId);
                }
            }
        }

        private void removeInvalidEnemies()
        {
            foreach (var keyPair in Enemies)
            {
                if (keyPair.Value?.IsValid == true)
                    continue;

                _idsToRemove.Add(keyPair.Key);
            }

            if (_idsToRemove.Count == 0)
            {
                return;
            }

            foreach (var id in _idsToRemove)
            {
                removeEnemy(id);
            }
            Logger.LogAndNotifyInfo($"Removed {_idsToRemove.Count} Invalid Enemies");
            _idsToRemove.Clear();
        }

        private void updateDebug()
        {
            if (ActiveEnemy != null)
            {
                if (SAINPlugin.DebugMode && SAINPlugin.DrawDebugGizmos)
                {
                    if (ActiveEnemy.LastHeardPosition != null)
                    {
                        if (debugLastHeardPosition == null)
                        {
                            debugLastHeardPosition = DebugGizmos.Line(ActiveEnemy.LastHeardPosition.Value, Bot.Position, Color.yellow, 0.01f, false, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdatePositionLine(ActiveEnemy.LastHeardPosition.Value, Bot.Position, debugLastHeardPosition);
                    }
                    if (ActiveEnemy.LastSeenPosition != null)
                    {
                        if (debugLastSeenPosition == null)
                        {
                            debugLastSeenPosition = DebugGizmos.Line(ActiveEnemy.LastSeenPosition.Value, Bot.Position, Color.red, 0.01f, false, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdatePositionLine(ActiveEnemy.LastSeenPosition.Value, Bot.Position, debugLastSeenPosition);
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
            BotMemoryClass memory = BotOwner?.Memory;
            if (memory != null)
            {
                memory.OnAddEnemy -= AddEnemy;
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

        public void ClearEnemy()
        {
            setActiveEnemy(null);
        }

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

        private void removeEnemy(string id)
        {
            if (ActiveEnemy != null &&
                ActiveEnemy.EnemyProfileId == id)
            {
                ActiveEnemy = null;
            }
            if (LastEnemy != null &&
                LastEnemy.EnemyProfileId == id)
            {
                LastEnemy = null;
            }

            Enemy dogFightTarget = Bot.Decision.DogFightDecision.DogFightTarget;
            if (dogFightTarget != null &&
                dogFightTarget.EnemyProfileId == id)
            {
                Bot.Decision.DogFightDecision.DogFightTarget = null;
            }

            if (Enemies.TryGetValue(id, out Enemy enemy))
            {
                OnEnemyRemoved?.Invoke(id);

                if (!enemy.IsAI)
                    HumansInLineOfSight.Remove(id);

                ActiveEnemies.Remove(enemy);
                enemy.Dispose();
                Enemies.Remove(id);
                removeEnemyInfo(enemy);
                if (enemy.EnemyPlayerComponent != null)
                {
                    enemy.EnemyPlayerComponent.OnComponentDestroyed -= removeEnemy;
                }
                //Logger.LogDebug($"Removed [{id}] from [{BotOwner?.name}'s] Enemy List");
            }
        }

        private void AssignActiveEnemy()
        {
            Enemy dogFightTarget = Bot.Decision.DogFightDecision.DogFightTarget;
            if (dogFightTarget?.IsValid == true)
            {
                setActiveEnemy(dogFightTarget);
                setGoalEnemy(dogFightTarget.EnemyInfo);
                return;
            }
            if (ActiveEnemy?.IsVisible == false)
            {
                Enemy visibileEnemy = checkIfAnyEnemyVisible();
                if (visibileEnemy != null)
                {
                    setActiveEnemy(visibileEnemy);
                    setGoalEnemy(visibileEnemy.EnemyInfo);
                    return;
                }
            }
            checkGoalEnemy();
            checkShotAtMe();
        }

        private void checkShotAtMe()
        {
            if (ActiveEnemy != null)
            {
                return;
            }
            foreach (var enemy in Enemies.Values)
            {
                if (enemy?.IsValid == true && enemy.Status.ShotAtMeRecently)
                {
                    setActiveEnemy(enemy);
                    return;
                }
            }
        }

        private void setGoalEnemy(EnemyInfo enemyInfo)
        {
            if (BotOwner.Memory.GoalEnemy != enemyInfo)
            {
                try
                {
                    BotOwner.Memory.GoalEnemy = enemyInfo;
                    BotOwner.CalcGoal();
                }
                catch
                {
                    // Sometimes bsg code throws an error here :D
                }
            }
        }

        private void checkGoalEnemy()
        {
            EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                Enemy enemy = ActiveEnemy;
                if (enemy != null)
                {
                    if (!enemy.IsValid)
                    {
                        setActiveEnemy(null);
                        return;
                    }
                    if (!enemy.Status.ShotAtMeRecently &&
                        !enemy.IsVisible)
                    {
                        setActiveEnemy(null);
                        return;
                    }
                }

                return;
            }

            if (goalEnemy?.Person != null &&
                goalEnemy.Person.HealthController.IsAlive == false)
            {
                setGoalEnemy(null);
                return;
            }
            setEnemyFromEnemyInfo(goalEnemy);
        }

        private void setEnemyFromEnemyInfo(EnemyInfo enemyInfo)
        {
            Enemy sainEnemy = CheckAddEnemy(enemyInfo?.Person);
            if (sainEnemy != null)
            {
                setActiveEnemy(sainEnemy);
            }
            else
            {
                Logger.LogError($"{enemyInfo?.Person?.ProfileId} not SAIN enemy!");
            }
        }

        private void setActiveEnemy(Enemy enemy)
        {
            if (enemy == null || (enemy.IsValid && enemy.EnemyPerson.Active))
            {
                if (ActiveEnemy != null &&
                    ActiveEnemy.IsValid &&
                    ActiveEnemy != enemy)
                {
                    setLastEnemy(ActiveEnemy);
                }

                ActiveEnemy = enemy;
            }
        }

        private void setLastEnemy(Enemy activeEnemy)
        {
            bool nullActiveEnemy = activeEnemy?.EnemyPerson?.Active == true;
            bool nullLastEnemy = LastEnemy?.EnemyPerson?.Active == true;

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

        public bool AreEnemiesSame(Enemy a, Enemy b)
        {
            return AreEnemiesSame(a?.EnemyIPlayer, b?.EnemyIPlayer);
        }

        public bool AreEnemiesSame(IPlayer a, IPlayer b)
        {
            return a != null
                && b != null
                && a.ProfileId == b.ProfileId;
        }

        public Enemy CheckAddEnemy(IPlayer IPlayer)
        {
            return tryAddEnemy(IPlayer);
        }

        private void AddEnemy(IPlayer player)
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
            enemyPlayerComponent.OnComponentDestroyed += removeEnemy;
            enemyPlayerComponent.Player.OnPlayerDead += enemyKilled;
            Enemies.Add(enemy.EnemyProfileId, enemy);
            return enemy;
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

        private float _nextLogTime;
        private float _nextCheckActiveTime;
        private bool _atPeace;
        private float _timePeaceStart;
        private float _nextLogActiveTime;
        private float _nextCheckHumanTime;
        private const float _checkHumanFreq = 0.25f;
        private readonly List<string> _idsToRemove = new List<string>();
        private GameObject debugLastSeenPosition;
        private GameObject debugLastHeardPosition;
    }
}