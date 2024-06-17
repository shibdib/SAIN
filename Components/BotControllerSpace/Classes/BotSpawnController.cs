using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using System;
using System.Collections;
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

        public void AddBot(BotOwner botOwner)
        {
            BotController.StartCoroutine(addBot(botOwner));
        }

        public void Subscribe(BotSpawner botSpawner)
        {
            if (!Subscribed)
            {
                botSpawner.OnBotRemoved += removeBot;
                Subscribed = true;
            }
        }

        public void UnSubscribe()
        {
            if (Subscribed &&
                BotController?.BotSpawner != null)
            {
                BotController.BotSpawner.OnBotRemoved -= removeBot;
                Subscribed = false;
            }
        }

        private bool Subscribed = false;

        public BotComponent GetSAIN(BotOwner botOwner, StringBuilder debugString)
        {
            return GetSAIN(botOwner?.name);
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

        private IEnumerator addBot(BotOwner botOwner)
        {
            yield return null;

            try
            {
                PlayerComponent playerComponent = getPlayerComp(botOwner);
                checkExisting(botOwner);

                if (SAINPlugin.IsBotExluded(botOwner))
                {
                    botOwner.gameObject.AddComponent<SAINNoBushESP>().Init(botOwner);
                    yield break;
                }

                initBotComp(botOwner, playerComponent);
            }
            catch (Exception ex)
            {
                Logger.LogError($"AddBot: Add Component Error: {ex}");
            }

            yield return null;
        }

        private PlayerComponent getPlayerComp(BotOwner botOwner)
        {
            PlayerComponent playerComponent = botOwner.gameObject.GetComponent<PlayerComponent>();
            playerComponent.InitBotOwner(botOwner);
            return playerComponent;
        }

        private void checkExisting(BotOwner botOwner)
        {
            string name = botOwner.name;
            if (BotDictionary.ContainsKey(name))
            {
                Logger.LogDebug($"{name} was already present in Bot Dictionary. Removing...");
                BotDictionary.Remove(name);
            }

            GameObject gameObject = botOwner.gameObject;
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
        }

        private void initBotComp(BotOwner botOwner, PlayerComponent playerComponent)
        {
            BotComponent botComponent = botOwner.gameObject.AddComponent<BotComponent>();
            if (botComponent.Init(playerComponent.Person))
            {
                BotDictionary.Add(botOwner.name, botComponent);
                playerComponent.InitBotComponent(botComponent);
                botOwner.LeaveData.OnLeave += removeBot;
                playerComponent.OnComponentDestroyed += removeBot;
            }
            else
            {
                botComponent?.Dispose();
            }
        }

        private IEnumerator destroyBotComponent(BotOwner botOwner)
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
            yield return null;
        }

        public void removeBot(IPlayer player)
        {
            removeBot(player?.AIData?.BotOwner);
        }

        private void removeBot(string profileID)
        {
            var playerComp = GameWorldComponent.Instance?.PlayerTracker.GetPlayerComponent(profileID);
            if (playerComp != null)
            {
                playerComp.OnComponentDestroyed -= removeBot;
                removeBot(playerComp.BotOwner);
            }
        }

        public void removeBot(BotOwner botOwner)
        {
            destroyBotComponent(botOwner);
        }
    }
}