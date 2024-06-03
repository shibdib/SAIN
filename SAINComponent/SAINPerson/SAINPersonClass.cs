using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.Classes.Info;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.BaseClasses
{
    public class SAINPersonClass
    {
        public bool PlayerNull => IPlayer == null || IPlayer.Transform == null;
        public IPlayer IPlayer { get; private set; }
        public Player Player { get; private set; }

        public SAINPersonClass(IPlayer iPlayer)
        {
            IPlayer = iPlayer;
            Player = GameWorldInfo.GetAlivePlayer(iPlayer);
            Name = Player?.name;
            Transform = new SAINPersonTransformClass(this);
            Profile = iPlayer.Profile;
            ProfileId = iPlayer.Profile?.ProfileId;
            Nickname = iPlayer.Profile?.Nickname;
        }

        public SAINPersonClass(IPlayer iPlayer, Player player)
        {
            Player = player;
            IPlayer = iPlayer;
            Name = player?.name;
            Transform = new SAINPersonTransformClass(this);
            Profile = player.Profile;
            ProfileId = player.Profile?.ProfileId;
            Nickname = player.Profile?.Nickname;
        }

        public void InitBot(BotOwner botOwner)
        {
            BotOwner = botOwner;
            IsAI = botOwner != null;
            BotComponent = botOwner?.gameObject?.GetComponent<BotComponent>();
            IsSAINBot = BotComponent != null;
        }

        public bool IsActive => PlayerNull == false && 
            (IsAI == false || BotOwner?.BotState == EBotState.Active) &&
            Player.gameObject.activeInHierarchy;

        public bool IsAI { get; private set; }
        public bool IsSAINBot { get; private set; }
        public BotOwner BotOwner { get; private set; }
        public BotComponent BotComponent { get; private set; }

        public Vector3 Position => Transform.Position;
        public readonly SAINPersonTransformClass Transform;
        public readonly Profile Profile;
        public readonly string ProfileId;
        public readonly string Nickname;
        public readonly string Name;
    }
}