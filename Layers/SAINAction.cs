using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Components;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Decision;
using System.Text;

namespace SAIN.Layers
{
    public abstract class SAINAction : CustomLogic
    {
        public SAINAction(BotOwner botOwner, string name) : base(botOwner)
        {
            SAINBot = botOwner.GetComponent<Bot>();
            Shoot = new ShootClass(botOwner);
        }

        public SAINBotController BotController => SAINPlugin.BotController;
        public DecisionWrapper Decisions => SAINBot.Memory.Decisions;

        public readonly Bot SAINBot;

        public readonly ShootClass Shoot;

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            DebugOverlay.AddBaseInfo(SAINBot, BotOwner, stringBuilder);
        }
    }
}
