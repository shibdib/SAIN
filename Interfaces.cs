using BepInEx.Logging;
using EFT;
using SAIN.Attributes;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using System.Collections.Generic;

namespace SAIN
{
    public interface ISAINSubComponent
    {
        void Init(BotComponent sain);
        BotOwner BotOwner { get; }
        Player Player { get; }
    }

    public interface IBotComponent
    {
        bool Init(PlayerComponent playerComponent);
        PlayerComponent PlayerComponent { get; }
        SAINPersonClass Person { get; }
        BotOwner BotOwner { get; }
        Player Player { get; }
    }

    public interface ISAINClass
    {
        void Init();
        void Update();
        void Dispose();
    }
}
