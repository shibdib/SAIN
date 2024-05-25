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
    internal class BotUnstuckLayer : SAINLayer
    {
        public BotUnstuckLayer(BotOwner bot, int priority) : base(bot, priority, Name)
        {
        }

        public static readonly string Name = BuildLayerName<BotUnstuckLayer>();

        public override Action GetNextAction()
        {
            return new Action(typeof(GetUnstuckAction), $"Getting Unstuck");
        }

        public override bool IsActive()
        {
            return SAINBot?.BotStuck?.BotIsStuck == true;
        }

        public override bool IsCurrentActionEnding()
        {
            if (SAINBot == null) return true;

            return false;
        }
    }
}