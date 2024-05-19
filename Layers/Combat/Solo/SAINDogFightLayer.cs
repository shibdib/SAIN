using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System.Text;
using SAIN.SAINComponent;
using SAIN.Layers.Combat.Solo.Cover;
using System.Collections.Generic;

namespace SAIN.Layers.Combat.Solo
{
    internal class SAINDogFightLayer : SAINLayer
    {
        public SAINDogFightLayer(BotOwner bot, int priority) : base(bot, priority, Name)
        {
        }

        public static readonly string Name = BuildLayerName<SAINDogFightLayer>();

        public override Action GetNextAction()
        {
            return new Action(typeof(DogFightAction), $"Dog Fight");
        }

        public override bool IsActive()
        {
            return shallDogFight;
        }

        public override bool IsCurrentActionEnding()
        {
            return !shallDogFight;
        }

        private bool shallDogFight => SAIN != null && SAIN.Memory.Decisions.Main.Current == SoloDecision.DogFight;

        public SoloDecision CurrentDecision => SAIN.Memory.Decisions.Main.Current;
    }
}