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
            return SAINPlugin.EditorDefaults.ForceBotsToRunAround;
        }

        public override bool IsCurrentActionEnding()
        {
            if (SAINBot == null) return true;

            return false;
        }

        private SoloDecision LastActionDecision = SoloDecision.None;
        public SoloDecision CurrentDecision => SAINBot.Decision.CurrentSoloDecision;
    }
}