using EFT;
using SAIN.SAINComponent.Classes.Info;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.BaseClasses
{
    public class SAINPersonClass : PersonBaseClass
    {
        public SAINPersonClass(IPlayer person) : base(person)
        {
            Name = Player?.name;
            Transform = new SAINPersonTransformClass(this);
            Profile = person.Profile;
            ProfileId = person.Profile?.ProfileId;
            Nickname = person.Profile?.Nickname;
            BotOwner = person.AIData?.BotOwner;
            IsAI = person.IsAI;
            if (IsAI)
            {
                SAIN = person.AIData?.BotOwner?.GetComponent<BotComponent>();
            }
            IsSAINBot = SAIN != null;
        }

        public void Update()
        {
        }

        public bool IsActive => PlayerNull == false && (IsAI == false || BotOwner?.BotState == EBotState.Active);
        public Vector3 Position => Transform.Position;
        public readonly SAINPersonTransformClass Transform;
        public readonly Profile Profile;
        public readonly string ProfileId;
        public readonly string Nickname;
        public readonly string Name;
        public readonly bool IsAI;
        public readonly bool IsSAINBot;
        public readonly BotOwner BotOwner;
        public readonly BotComponent SAIN;
    }
}