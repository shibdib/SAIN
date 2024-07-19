using SAIN.SAINComponent.Classes.Sense;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINVisionClass : BotBase, IBotClass
    {
        public float TimeLastCheckedLOS { get; set; }
        public float TimeSinceCheckedLOS => Time.time - TimeLastCheckedLOS;

        public SAINVisionClass(BotComponent component) : base(component)
        {
            FlashLightDazzle = new FlashLightDazzleClass(component);
            BotLook = new SAINBotLookClass(component);
        }

        public void Init()
        {
            BotLook.Init();
        }

        public void Update()
        {
            FlashLightDazzle.CheckIfDazzleApplied(Bot.Enemy);
        }

        public void Dispose()
        {
            BotLook.Dispose();
        }

        public FlashLightDazzleClass FlashLightDazzle { get; private set; }

        public SAINBotLookClass BotLook { get; private set; }
    }
}