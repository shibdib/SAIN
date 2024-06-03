using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System.Text;
using SAIN.SAINComponent;
using SAIN.Layers.Combat.Solo.Cover;
using System.Collections.Generic;
using SAIN.Layers.Combat.Solo;

namespace SAIN.Layers
{
    internal class SAINAvoidThreatLayer : SAINLayer
    {
        public SAINAvoidThreatLayer(BotOwner bot, int priority) : base(bot, priority, Name)
        {
        }

        public static readonly string Name = BuildLayerName<SAINAvoidThreatLayer>();

        public override Action GetNextAction()
        {
            _lastActionDecision = CurrentDecision;
            switch (_lastActionDecision)
            {
                case SoloDecision.DogFight:
                    return new Action(typeof(DogFightAction), $"Dog Fight - Enemy Close!");

                case SoloDecision.AvoidGrenade:
                    return new Action(typeof(RunToCoverAction), $"Avoid Grenade");

                default:
                    return new Action(typeof(DogFightAction), $"NO DECISION - ERROR IN LOGIC");
            }
        }

        public override bool IsActive()
        {
            bool active = 
                SAINBot?.BotActive == true &&
                (CurrentDecision == SoloDecision.DogFight ||
                CurrentDecision == SoloDecision.AvoidGrenade);

            if (SAINBot != null)
            {
                SAINBot.ActiveLayer = ESAINLayer.AvoidThreat;
            }
            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            return SAINBot?.BotActive == true && _lastActionDecision != CurrentDecision;
        }

        private SoloDecision _lastActionDecision;
        public SoloDecision CurrentDecision => SAINBot.Decision.CurrentSoloDecision;
    }
}