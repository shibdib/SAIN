using EFT;
using SAIN.SAINComponent;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonClass : PersonBase
    {
        public string Name { get; private set; }
        public ProfileData Profile { get; }
        public PlayerData PlayerObjects { get; }
        public PersonAIInfo AIInfo { get; } = new PersonAIInfo();
        public PersonTransformClass Transform { get; }
        public PersonActiveClass ActivationClass { get; }

        public void Update()
        {
            ActivationClass.CheckActive();
            if (ActivationClass.PlayerActive)
            {
                Transform.Update();
            }
        }

        public void LateUpdate()
        {
            ActivationClass.CheckActive();
        }

        public bool Active => ActivationClass.Active;

        public PersonClass(PlayerData objects) : base(objects)
        {
            Transform = new PersonTransformClass(this, objects);
            ActivationClass = new PersonActiveClass(this, objects);
            Name = objects.Player.name;
        }

        public void InitBot(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                Logger.LogWarning($"{Name} : Null BotOwner, cannot Initialize!");
                return;
            }
            AIInfo.InitBot(botOwner);
            Name = botOwner.name;
            ActivationClass.InitBot(botOwner);
        }

        public void InitBot(BotComponent bot)
        {
            AIInfo.InitBot(bot);
        }
    }
}