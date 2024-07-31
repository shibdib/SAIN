namespace SAIN.Components.PlayerComponentSpace
{
    public class OtherPlayerData
    {
        public readonly string ProfileId;
        public PlayerDistanceData DistanceData { get; } = new PlayerDistanceData();

        public OtherPlayerData(string id)
        {
            ProfileId = id;
        }
    }
}