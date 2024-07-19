using EFT;
using SAIN.Components;
using SAIN.Components.BotController;
using SAIN.Layers.Peace;
using SAIN.SAINComponent;
using System.Collections.Generic;

namespace SAIN
{
    public abstract class SAINControllerBase
    {
        public SAINControllerBase(SAINBotController botController)
        {
            BotController = botController;
        }

        public SAINBotController BotController { get; private set; }
        public BotDictionary Bots => BotController?.BotSpawnController?.BotDictionary;
        public GameWorld GameWorld => BotController.GameWorld;
        public GameWorldComponent SAINGameWorld => BotController.SAINGameWorld;
    }
}