using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace SAIN.Components.BotController
{
    public class BotSpawnController : SAINControl
    {
        public BotSpawnController(SAINBotController botController) : base(botController)
        {
            Instance = this;
        }

        public static BotSpawnController Instance;

        public Dictionary<string, BotComponent> BotDictionary = new Dictionary<string, BotComponent>();

        public static readonly List<WildSpawnType> StrictExclusionList = new List<WildSpawnType>
        {
            WildSpawnType.bossZryachiy,
            WildSpawnType.followerZryachiy,
            WildSpawnType.peacefullZryachiyEvent,
            WildSpawnType.ravangeZryachiyEvent,
            WildSpawnType.shooterBTR,
            WildSpawnType.marksman
        };

        public void Update()
        {
            if (Subscribed &&
                GameEnding)
            {
                UnSubscribe();
            }
        }

        public bool GameEnding
        {
            get
            {
                var status = GameStatus;
                return status == GameStatus.Stopping || status == GameStatus.Stopped || status == GameStatus.SoftStopping;
            }
        }

        private GameStatus GameStatus
        {
            get
            {
                var botGame = BotController?.BotGame;
                if (botGame != null)
                {
                    return botGame.Status;
                }
                return GameStatus.Starting;
            }
        }

        public void Subscribe(BotSpawner botSpawner)
        {
            if (!Subscribed)
            {
                botSpawner.OnBotRemoved += RemoveBot;
                Subscribed = true;
            }
        }

        public void UnSubscribe()
        {
            if (Subscribed &&
                BotController?.BotSpawner != null)
            {
                BotController.BotSpawner.OnBotRemoved -= RemoveBot;
                Subscribed = false;
            }
        }

        private bool Subscribed = false;

        public BotComponent GetSAIN(BotOwner botOwner, StringBuilder debugString)
        {
            return GetSAIN(botOwner?.name);
        }

        public BotComponent GetSAIN(Player player)
        {
            return GetSAIN(player?.AIData?.BotOwner?.name);
        }

        public BotComponent GetSAIN(string botName)
        {
            if (!botName.IsNullOrEmpty() &&
                BotDictionary.TryGetValue(botName, out BotComponent component))
            {
                return component;
            }
            return null;
        }

        public void AddBot(BotOwner botOwner)
        {
            try
            {
                GameObject gameObject = botOwner.gameObject;
                PlayerComponent playerComponent = gameObject.GetComponent<PlayerComponent>();
                playerComponent.InitBotOwner(botOwner);

                string name = botOwner.name;
                if (BotDictionary.ContainsKey(name))
                {
                    Logger.LogDebug($"{name} was already present in Bot Dictionary. Removing...");
                    BotDictionary.Remove(name);
                }

                // If somehow this bot already has components attached, destroy it.
                if (gameObject.TryGetComponent(out BotComponent botComponent))
                {
                    Logger.LogDebug($"{name} already had a BotComponent attached. Destroying...");
                    botComponent.Dispose();
                }
                if (gameObject.TryGetComponent(out SAINNoBushESP noBushComponent))
                {
                    Logger.LogDebug($"{name} already had No Bush ESP attached. Destroying...");
                    GameObject.Destroy(noBushComponent);
                }

                if (SAINPlugin.IsBotExluded(botOwner))
                {
                    gameObject.AddComponent<SAINNoBushESP>().Init(botOwner);
                    return;
                }

                // Create a new Component
                botComponent = gameObject.AddComponent<BotComponent>();
                if (botComponent.Init(playerComponent))
                {
                    BotDictionary.Add(name, botComponent);
                    playerComponent.InitBotComponent(botComponent);
                    botOwner.LeaveData.OnLeave += RemoveBot;
                    playerComponent.IPlayer.OnIPlayerDeadOrUnspawn += RemoveBot;
                }
                else
                {
                    botComponent?.Dispose();
                }

            }
            catch (Exception ex)
            {
                Logger.LogError($"AddBot: Add Component Error: {ex}");
            }
        }

        private PlayerComponent getPlayerComp(string profileId) => GameWorldComponent.Instance.PlayerTracker.GetPlayerComponent(profileId);

        public void RemoveBot(BotOwner botOwner)
        {
            try
            {
                if (botOwner != null)
                {
                    BotDictionary.Remove(botOwner.name);
                    if (botOwner.TryGetComponent(out BotComponent component))
                    {
                        component.Dispose();
                    }
                    if (botOwner.TryGetComponent(out SAINNoBushESP noBush))
                    {
                        UnityEngine.Object.Destroy(noBush);
                    }
                }
                else
                {
                    Logger.LogError("Bot is null, cannot dispose!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose Component Error: {ex}");
            }
        }

        public void RemoveBot(IPlayer player)
        {
            RemoveBot(player?.AIData?.BotOwner);
        }
    }
}