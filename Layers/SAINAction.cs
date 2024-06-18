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
            Bot = botOwner.GetComponent<BotComponent>();
            Shoot = new ShootClass(botOwner);
        }

        public SAINBotController BotController => SAINPlugin.BotController;

        public readonly BotComponent Bot;

        public readonly ShootClass Shoot;

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            DebugOverlay.AddBaseInfo(Bot, BotOwner, stringBuilder);
        }
    }
}
