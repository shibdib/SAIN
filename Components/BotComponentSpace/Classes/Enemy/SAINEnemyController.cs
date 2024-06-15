using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using System;
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
        public System.Action<Player> OnEnemyKilled { get; set; }

        public readonly Dictionary<string, SAINEnemy> Enemies = new Dictionary<string, SAINEnemy>();

        public SAINEnemyController(BotComponent sain) : base(sain)
        {
            if (_players == null)
                _players = GameWorldComponent.Instance.PlayerTracker.AlivePlayers;
        }

        private static Dictionary<string, PlayerComponent> _players;

        public void Init()
        {
            BotOwner.Memory.OnAddEnemy += AddEnemy;
        }

        public void Update()
        {
            CheckAddEnemy();
            updateEnemies();
            //checkDiscrepency();
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
                if (enemy == null)
                {
                    continue;
                }
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

        private void updateEnemies()
        {
            removeInvalidEnemies();
            updateAllEnemies();
            checkHumanLOS();
            if (ActiveEnemy != null && 
                !ActiveEnemy.EnemyPerson.IsActive)
            {
                setActiveEnemy(null);
            }
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

        private float _nextCheckHumanTime;
        private const float _checkHumanFreq = 0.25f;

        public List<string> HumansInLineOfSight { get; } = new List<string>();

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
                RemoveEnemy(id);
            }
            Logger.LogAndNotifyInfo($"Removed {_idsToRemove.Count} Invalid Enemies");
            _idsToRemove.Clear();
        }

        private readonly List<string> _idsToRemove = new List<string>();

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

            if (badInfo?.Person != null)
            {
                BotOwner.EnemiesController.Remove(badInfo.Person);
            }
        }

        public void ClearEnemy()
        {
            setActiveEnemy(null);
        }

        private void removeEnemy(IPlayer player)
        {
            if (player != null)
            {
                player.OnIPlayerDeadOrUnspawn -= removeEnemy;
                RemoveEnemy(player.ProfileId);
            }
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

        public void RemoveEnemy(string id)
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

            SAINEnemy dogFightTarget = Bot.Decision.DogFightDecision.DogFightTarget;
            if (dogFightTarget != null &&
                dogFightTarget.EnemyProfileId == id)
            {
                Bot.Decision.DogFightDecision.DogFightTarget = null;
            }


            if (Enemies.TryGetValue(id, out SAINEnemy enemy))
            {
                if (!enemy.IsAI)
                    HumansInLineOfSight.Remove(id);

                ActiveEnemies.Remove(enemy);
                enemy.Dispose();
                Enemies.Remove(id);
                removeEnemyInfo(enemy);
                OnEnemyRemoved?.Invoke(id);
                //Logger.LogDebug($"Removed [{id}] from [{BotOwner?.name}'s] Enemy List");
            }
        }

        public System.Action<string> OnEnemyRemoved { get; set; }

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

        private SAINEnemy tryAddEnemy(IPlayer enemyPlayer)
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

            if (Enemies.TryGetValue(enemyPlayer.ProfileId, out SAINEnemy sainEnemy))
            {
                return sainEnemy;
            }

            if (!BotOwner.EnemiesController.EnemyInfos.TryGetValue(enemyPlayer, out EnemyInfo enemyInfo))
            {
                string debugString = $"Player {enemyPlayer.Profile.Nickname} : Side: {enemyPlayer.Profile.Side} is not in Botowner's {Player.Profile.Nickname} : Side: {Player.Profile.Side} EnemyInfos dictionary.: ";
                debugString = findSourceDebug(debugString);
                Logger.LogDebug(debugString);
                return null;
            }

            sainEnemy = new SAINEnemy(Bot, enemyPlayerComponent, enemyInfo);
            Enemies.Add(enemyPlayer.ProfileId, sainEnemy);
            enemyPlayer.OnIPlayerDeadOrUnspawn += removeEnemy;
            enemyPlayerComponent.OnComponentDestroyed += RemoveEnemy;
            enemyPlayerComponent.Player.OnPlayerDead += enemyKilled;
            return sainEnemy;
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

        public Action<SAINEnemy, float> OnEnemyForgotten { get; set; }

        public bool IsHumanPlayerLookAtMe(out Player lookingPlayer)
        {
            foreach (var enemy in Enemies.Values)
            {
                if (enemy == null || enemy.IsAI)
                {
                    continue;
                }

                Vector3 lookDir = enemy.Player.LookDirection;
                Vector3 botDir = Bot.Person.Transform.BodyPosition - enemy.Player.MainParts[BodyPartType.head].Position;

                if (Vector3.Dot(lookDir, botDir.normalized) > 0.75f)
                {
                    lookingPlayer = enemy.Player;
                    return true;
                }
            }
            lookingPlayer = null;
            return false;
        }

        public bool IsPlayerAnEnemy(string profileID)
        {
            return Enemies.TryGetValue(profileID, out var enemy) && enemy != null;
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
            if (iPlayer.AIData.IsAI
            && BotOwner.BotsGroup.Contains(iPlayer.AIData.BotOwner))
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
    }
}