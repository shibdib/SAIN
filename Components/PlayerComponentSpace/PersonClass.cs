using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Info;
using System;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PersonClass
    {
        public bool IsActive
        {
            get
            {
                if (!IsPlayerActive)
                {
                    return false;
                }
                if (IsAI &&
                    !IsBotActive)
                {
                    return false;
                }
                return true;
            }
        }

        public bool IsPlayerActive
        {
            get
            {
                return PlayerExists && IsAlive;
            }
        }

        public bool IsBotActive
        {
            get
            {
                if (IsAI)
                {
                    if (!BotExists)
                    {
                        return false;
                    }
                    if (!IsAlive)
                    {
                        return false;
                    }
                    if (!BotOwner.gameObject.activeInHierarchy)
                    {
                        return false;
                    }
                    if (BotOwner.BotState != EBotState.Active)
                    {
                        return false;
                    }
                    if (BotOwner.StandBy?.StandByType != BotStandByType.active)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public Vector3 Position => Transform.Position;
        public bool PlayerExists => Player != null && Player.gameObject != null && Player.Transform?.Original != null;
        public bool BotExists => BotOwner != null && BotOwner.gameObject != null && BotOwner.Transform?.Original != null;
        public bool IsAlive => IPlayer?.HealthController?.IsAlive == true;
        public IPlayer IPlayer { get; private set; }
        public Player Player { get; private set; }

        public PersonClass(IPlayer iPlayer, Player player)
        {
            Player = player;
            IPlayer = iPlayer;
            Name = player?.name;
            ProfileId = player.ProfileId;
            Nickname = player.Profile?.Nickname;
            Transform = new PersonTransformClass(this);
        }

        public void initBot(BotOwner botOwner)
        {
            if (botOwner != null)
            {
                Name = botOwner.name;
                BotOwner = botOwner;
                IsAI = true;
                BotComponent = botOwner.gameObject?.GetComponent<BotComponent>();
                IsSAINBot = BotComponent != null;
            }
        }


        public bool IsAI { get; private set; } = false;
        public bool IsSAINBot { get; private set; } = false;
        public BotOwner BotOwner { get; private set; }
        public BotComponent BotComponent { get; private set; }
        public PersonTransformClass Transform { get; private set; }
        public string ProfileId { get; private set; }
        public string Nickname { get; private set; }
        public string Name { get; private set; }
    }
}