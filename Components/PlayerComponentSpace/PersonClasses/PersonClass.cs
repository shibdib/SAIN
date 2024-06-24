using EFT;
using SAIN.SAINComponent;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonClass
    {
        public void Update()
        {
            ActiveClass.CheckActive();
            if (ActiveClass.PlayerActive)
            {
                Transform.UpdatePositions();
            }
        }

        public void LateUpdate()
        {
            ActiveClass.CheckActive();
        }

        public bool Active => ActiveClass.Active;

        public IPlayer IPlayer { get; private set; }

        public Player Player { get; private set; }

        public GameObject GameObject { get; private set; }

        public bool IsAI { get; private set; }

        public bool IsSAINBot { get; private set; }

        public BotOwner BotOwner { get; private set; }

        public BotComponent BotComponent { get; private set; }

        public PlayerComponent PlayerComponent { get; private set; }

        public PersonTransformClass Transform { get; private set; }

        public PersonActiveClass ActiveClass { get; private set; }

        public string Name { get; private set; }

        public readonly string ProfileId;

        public readonly string Nickname;

        public PersonClass(IPlayer iPlayer, Player player, PlayerComponent playerComponent)
        {
            PlayerComponent = playerComponent;
            GameObject = playerComponent.gameObject;
            Player = player;
            IPlayer = iPlayer;
            Name = player?.name;
            ProfileId = player.ProfileId;
            Nickname = player.Profile?.Nickname;
            Transform = new PersonTransformClass(this);
            ActiveClass = new PersonActiveClass(this);
        }

        public void InitBotOwner(BotOwner botOwner)
        {
            if (botOwner != null)
            {
                Name = botOwner.name;
                BotOwner = botOwner;
                IsAI = true;
                ActiveClass.InitBotOwner(botOwner);
            }
        }

        public void InitBotComponent(BotComponent bot)
        {
            BotComponent = bot;
            IsSAINBot = bot != null;
        }

    }
}