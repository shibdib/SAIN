using EFT.EnvironmentEffect;
using SAIN.Components.PlayerComponentSpace.Classes.Equipment;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Info;

namespace SAIN.Components.PlayerComponentSpace.Classes
{
    public class SAINAIData : PlayerComponentBase
    {
        public AIGearModifierClass AIGearModifier { get; private set; }

        public PlayerLocationClass PlayerLocation { get; private set; }

        public SAINAIData(GearInfo gearInfo, PlayerComponent component) : base(component)
        {
            PlayerLocation = new PlayerLocationClass(this);
            AIGearModifier = new AIGearModifierClass(this);
        }

        public class PlayerLocationClass : AIDataBase
        {
            public float BunkerDepth { get; private set; }
            public bool InBunker { get; private set; }

            public PlayerLocationClass(SAINAIData aiData) : base(aiData)
            {
            }

            public void updateEnvironment(IndoorTrigger trigger)
            {
                InBunker = trigger?.IsBunker == true;
                BunkerDepth = InBunker ? trigger.BunkerDepth : 0f;
            }
        }
    }

    public abstract class AIDataBase
    {
        public AIDataBase(SAINAIData aidata)
        {
            AIData = aidata;
        }

        protected readonly SAINAIData AIData;
        protected GearInfo GearInfo => AIData.PlayerComponent.Equipment.GearInfo;
    }
}