using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class SAINEnemyController : SAINBase, ISAINClass
    {
        public bool HasEnemy => ActiveEnemy?.EnemyPerson?.IsActive == true;
        public bool HasLastEnemy => LastEnemy?.EnemyPerson?.IsActive == true;
        public SAINEnemy ActiveEnemy { get; private set; }
        public SAINEnemy LastEnemy { get; private set; }

        public readonly Dictionary<string, SAINEnemy> Enemies = new Dictionary<string, SAINEnemy>();

        public SAINEnemyController(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            BotMemoryClass memory = BotOwner?.Memory;
            if (memory != null)
            {
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
            updateEnemies();
            checkDiscrepency();
            checkActiveEnemies();
            updateDebug();
            checkIsAtPeace();
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

        private bool _atPeace;
        private float _timePeaceStart;
        public bool NoEnemyContact => _atPeace && Time.time - _timePeaceStart > 3f;

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
                    SAINEnemy sainEnemy = GetEnemy(goalEnemy.ProfileId);
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

        private float _nextLogTime;

        private float _nextCheckActiveTime;

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
            foreach (SAINEnemy enemy in Enemies.Values)
            {
                if (enemy != null)
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

        public bool ActiveHumanEnemy { get; private set; }

        private float _nextLogActiveTime;

        public readonly List<SAINEnemy> ActiveEnemies = new List<SAINEnemy>();

        private SAINEnemy checkIfAnyEnemyVisible()
        {
            if (ActiveEnemy?.IsVisible == true)
            {
                return null;
            }
            foreach (var enemy in Enemies.Values)
            {
                if (enemy?.IsValid == true && enemy.IsVisible)
                {
                    return enemy;
                }
            }
            return null;
        }

        private readonly List<SAINEnemy> _localAIEnemiesList = new List<SAINEnemy>();
        private readonly List<SAINEnemy> _localHumanEnemiesList = new List<SAINEnemy>();

        private void updateEnemies()
        {
            var activeEnemy = ActiveEnemy;
            if (activeEnemy?.IsValid == true)
            {
                //activeEnemy.Update();
            }

            removeInvalidEnemies();
            updateAllEnemies();

            if (ActiveEnemy != null && !ActiveEnemy.EnemyPerson.IsActive)
            {
                setActiveEnemy(null);
            }
        }

        private void updateAllEnemies()
        {
            foreach (var enemy in Enemies.Values)
            {
                if (enemy?.IsValid == true)
                {
                    enemy.Update();
                }
            }
        }

        private void removeInvalidEnemies()
        {
            foreach (var keyPair in Enemies)
            {
                var enemy = keyPair.Value;
                if (enemy?.IsValid == true)
                {
                    continue;
                }
                _idsToRemove.Add(keyPair.Key);
            }
            foreach (var id in _idsToRemove)
            {
                RemoveEnemy(id);
            }
            _idsToRemove.Clear();
        }

        private readonly List<string> _idsToRemove = new List<string>();

        private IEnumerator updateValidHumanEnemies(int maxPerFrame)
        {
            while (true)
            {
                int count = 0;
                foreach (var enemy in Enemies)
                {
                    if (enemy.Value?.IsAI == false)
                    {
                        _localHumanEnemiesList.Add(enemy.Value);
                    }
                }
                //Logger.LogDebug($"Updating {_localHumanEnemiesList.Count} Human Enemies for [{BotOwner?.name}]");
                foreach (var enemy in _localHumanEnemiesList)
                {
                    if (enemy?.IsValid == true)
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
                _localHumanEnemiesList.Clear();
                yield return null;
            }
        }

        private IEnumerator updateValidAIEnemies(int maxPerFrame)
        {
            while (true)
            {
                int count = 0;
                foreach (var enemy in Enemies)
                {
                    if (enemy.Value?.IsAI == true)
                    {
                        _localAIEnemiesList.Add(enemy.Value);
                    }
                }
                //Logger.LogDebug($"Updating {_localAIEnemiesList.Count} AI Enemies for [{BotOwner?.name}]");
                foreach (var enemy in _localAIEnemiesList)
                {
                    if (enemy?.IsValid == true)
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
                _localAIEnemiesList.Clear();
                yield return null;
            }
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
                    Object.Destroy(debugLastHeardPosition);
                    Object.Destroy(debugLastSeenPosition);
                }
            }
            else if (debugLastHeardPosition != null || debugLastSeenPosition != null)
            {
                Object.Destroy(debugLastHeardPosition);
                Object.Destroy(debugLastSeenPosition);
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
                if (enemy.EnemyPlayer == null)
                {
                    Enemies.Remove(id);
                    removeEnemyInfo(enemy);
                    return null;
                }
                return enemy;
            }
            return null;
        }

        private void removeEnemyInfo(SAINEnemy enemy)
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
            removeEnemyInfo(badInfo);
        }

        private void removeEnemyInfo(EnemyInfo enemyInfo)
        {
            if (enemyInfo?.Person == null)
            {
                return;
            }
            BotOwner.EnemiesController.Remove(enemyInfo?.Person);
        }

        public void ClearEnemy()
        {
            setActiveEnemy(null);
        }

        public void RemoveEnemy(string id)
        {
            removeActiveEnemy(id);
            removeLastEnemy(id);
            removeDogFightTarget(id);

            if (Enemies.TryGetValue(id, out SAINEnemy enemy))
            {
                enemy.Dispose();
                Enemies.Remove(id);
                removeEnemyInfo(enemy);
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
            if (LastEnemy != null && LastEnemy.EnemyProfileId == id)
            {
                LastEnemy = null;
            }
        }

        private void removeDogFightTarget(string id)
        {
            SAINEnemy dogFightTarget = Bot.Decision.DogFightDecision.DogFightTarget;
            if (dogFightTarget?.EnemyPerson != null
                && dogFightTarget.EnemyPerson.ProfileId == id)
            {
                Bot.Decision.DogFightDecision.DogFightTarget = null;
            }
        }

        private void CheckAddEnemy()
        {
            SAINEnemy dogFightTarget = Bot.Decision.DogFightDecision.DogFightTarget;
            if (dogFightTarget?.IsValid == true)
            {
                setActiveEnemy(dogFightTarget);
                setGoalEnemy(dogFightTarget.EnemyInfo);
                return;
            }
            checkGoalEnemy();
            checkVisibleEnemies();
            checkShotAtMe();
        }

        private void checkVisibleEnemies()
        {
            if (ActiveEnemy?.IsVisible == false)
            {
                SAINEnemy visibileEnemy = checkIfAnyEnemyVisible();
                if (visibileEnemy != null)
                {
                    setActiveEnemy(visibileEnemy);
                    setGoalEnemy(visibileEnemy.EnemyInfo);
                }
            }
        }

        private void checkShotAtMe()
        {
            if (ActiveEnemy != null)
            {
                return;
            }
            foreach (var enemy in Enemies.Values)
            {
                if (enemy?.IsValid == true && enemy.EnemyStatus.ShotAtMeRecently)
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
            SAINEnemy sainEnemy = ActiveEnemy;
            if (goalEnemy == null)
            {
                if (sainEnemy != null &&
                    !sainEnemy.EnemyStatus.ShotAtMeRecently &&
                    !sainEnemy.IsVisible)
                {
                    setActiveEnemy(null);
                }

                return;
            }

            if (goalEnemy?.Person != null && goalEnemy.Person.HealthController.IsAlive == false)
            {
                setGoalEnemy(null);
            }
            else
            {
                setEnemyFromEnemyInfo(goalEnemy);
            }
        }

        private void setEnemyFromEnemyInfo(EnemyInfo enemyInfo)
        {
            SAINEnemy sainEnemy = CheckAddEnemy(enemyInfo?.Person);
            if (sainEnemy != null)
            {
                setActiveEnemy(sainEnemy);
            }
            else
            {
                Logger.LogError($"{enemyInfo?.Person?.ProfileId} not SAIN enemy!");
            }
        }

        private void setActiveEnemy(SAINEnemy enemy)
        {
            if (enemy == null || (enemy.IsValid && enemy.EnemyPerson.IsActive))
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
            return AreEnemiesSame(a?.EnemyIPlayer, b?.EnemyIPlayer);
        }

        public bool AreEnemiesSame(EnemyInfo a, SAINEnemy b)
        {
            return AreEnemiesSame(a?.Person, b?.EnemyIPlayer);
        }

        public bool AreEnemiesSame(EnemyInfo a, EnemyInfo b)
        {
            return AreEnemiesSame(a?.Person, b?.Person);
        }

        public bool AreEnemiesSame(IPlayer a, IPlayer b)
        {
            return a != null
                && b != null
                && a.ProfileId == b.ProfileId;
        }

        public SAINEnemy CheckAddEnemy(IPlayer IPlayer)
        {
            return tryAddEnemy(IPlayer);
        }

        private void AddEnemy(IPlayer player)
        {
            tryAddEnemy(player);
        }

        private SAINEnemy tryAddEnemy(IPlayer player)
        {
            if (player == null)
            {
                //Logger.LogDebug("Cannot add null player as an enemy.");
                return null;
            }
            if (!player.HealthController.IsAlive)
            {
                //Logger.LogDebug("Cannot add dead player as an enemy.");
                return null;
            }
            if (Enemies.TryGetValue(player.ProfileId, out SAINEnemy enemy))
            {
                return enemy;
            }
            if (player.ProfileId == Bot.Player.ProfileId)
            {
                string debugString = $"Cannot add enemy {getBotInfo(player)} that matches this bot {getBotInfo(Player)}: ";
                debugString = findSourceDebug(debugString);
                Logger.LogDebug(debugString);
                return null;
            }
            if (player.IsAI && player.AIData?.BotOwner == null)
            {
                Logger.LogDebug("Cannot add ai as enemy with null Botowner");
                return null;
            }
            if (BotOwner.EnemiesController.EnemyInfos.TryGetValue(player, out EnemyInfo enemyInfo))
            {
                PlayerComponent playerComponent = GameWorldHandler.SAINGameWorld.PlayerTracker.GetPlayerComponent(player.ProfileId);
                if (playerComponent != null)
                {
                    SAINEnemy newEnemy = new SAINEnemy(Bot, playerComponent, enemyInfo);
                    player.OnIPlayerDeadOrUnspawn += newEnemy.DeleteInfo;
                    Enemies.Add(player.ProfileId, newEnemy);
                    //Logger.LogDebug($"Added [{player.ProfileId}] to [{BotOwner?.name}'s] Enemy List");
                    return newEnemy;
                }
            }
            else
            {
                //string debugString = $"Player {getBotInfo(player)} " +
                //    $"is not in Bot {getBotInfo(player)} EnemyInfos list. Cannot Add them as a SAIN Enemy.";
                //debugString = findSourceDebug(debugString);
                //Logger.LogDebug(debugString);
            }
            return null;
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

        public bool IsHumanPlayerLookAtMe(out Player lookingPlayer)
        {
            var gameworld = GameWorldHandler.SAINGameWorld?.GameWorld;
            if (gameworld != null)
            {
                var players = gameworld.AllAlivePlayersList;
                if (players != null)
                {
                    foreach (var player in Bot.Memory.VisiblePlayers)
                    {
                        if (player != null
                            && !player.IsAI
                            && Bot.EnemyController.IsPlayerAnEnemy(player.ProfileId))
                        {
                            Vector3 lookDir = player.LookDirection;
                            Vector3 playerHeadPos = player.MainParts[BodyPartType.head].Position;

                            Vector3 botChestPos = Bot.Person.Transform.CenterPosition;
                            Vector3 botDir = botChestPos - playerHeadPos;

                            if (Vector3.Dot(lookDir, botDir.normalized) > 0.75f)
                            {
                                lookingPlayer = player;
                                return true;
                            }
                        }
                    }
                }
            }
            lookingPlayer = null;
            return false;
        }

        public bool IsPlayerAnEnemy(string profileID)
        {
            return Enemies.ContainsKey(profileID) && Enemies[profileID] != null;
        }

        public bool IsPlayerFriendly(IPlayer iPlayer)
        {
            if (iPlayer != null)
            {
                Player player = GameWorldInfo.GetAlivePlayer(iPlayer);
                if (player != null)
                {
                    // Check that the source isn't from a member of the bot's group.
                    if (player.AIData.IsAI
                        && BotOwner.BotsGroup.Contains(player.AIData.BotOwner))
                    {
                        return true;
                    }
                    if (player.ProfileId == Bot.Person.Player.ProfileId)
                    {
                        return true;
                    }
                    if (GetEnemy(player.ProfileId) != null)
                    {
                        return false;
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
                }
            }
            return false;
        }
    }
}