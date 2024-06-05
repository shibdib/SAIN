using EFT.EnvironmentEffect;

namespace SAIN.Components.PlayerComponentSpace
{
    public class SAINAIData
    {
        public void updateEnvironment(IndoorTrigger trigger)
        {
            InBunker = trigger?.IsBunker == true;
        }

        public bool InBunker { get; set; }
    }
}