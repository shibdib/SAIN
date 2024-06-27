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
        public BotRunLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.Run)
        {
        }

        public static readonly string Name = BuildLayerName("Run Debug");

        public override Action GetNextAction()
        {
            return new Action(typeof(RunningAction), $"RUNNING");
        }

        public override bool IsActive()
        {
            bool active = SAINPlugin.DebugSettings.ForceBotsToRunAround;
            setLayer(active);
            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            if (Bot == null) return true;

            return false;
        }

        public SoloDecision CurrentDecision => Bot.Decision.CurrentSoloDecision;
    }
}