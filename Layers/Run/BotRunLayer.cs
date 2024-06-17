using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System.Text;
using SAIN.SAINComponent;
using SAIN.Layers.Combat.Solo.Cover;
using System.Collections.Generic;
using SAIN.Layers.Combat.Solo;

namespace SAIN.Layers.Combat.Run
{
    internal class BotRunLayer : SAINLayer
    {
        public BotRunLayer(BotOwner bot, int priority) : base(bot, priority, Name)
        {
        }

        public static readonly string Name = BuildLayerName<BotRunLayer>();

        public override Action GetNextAction()
        {
            return new Action(typeof(RunningAction), $"RUNNING");
        }

        public override bool IsActive()
        {
            bool active = SAINPlugin.DebugSettings.ForceBotsToRunAround;
            if (active)
            {
                SAINBot.ActiveLayer = ESAINLayer.Run;
            }
            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            if (SAINBot == null) return true;

            return false;
        }

        public SoloDecision CurrentDecision => SAINBot.Decision.CurrentSoloDecision;
    }
}