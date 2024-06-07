using EFT;
using SAIN.Components;
using SAIN.SAINComponent;
using System.Collections.Generic;

namespace SAIN
{
    public abstract class SAINControl
    {
        public SAINControl(SAINBotController botController)
        {
            BotController = botController;
        }

        public SAINBotController BotController { get; private set; }
        public Dictionary<string, BotComponent> Bots => BotController?.BotSpawnController?.BotDictionary;
        public GameWorld GameWorld => BotController.GameWorld;
        public GameWorldComponent SAINGameWorld => BotController.SAINGameWorld;
    }
}