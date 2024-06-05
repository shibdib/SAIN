using BepInEx.Logging;
using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.GlobalSettings;

namespace SAIN.SAINComponent
{
    public abstract class PlayerComponentBase
    {
        public PlayerComponentBase(PlayerComponent player)
        {
            PlayerComponent = player;
        }

        public PlayerComponent PlayerComponent { get; private set; }
        public Player Player => PlayerComponent.Player;
        public IPlayer IPlayer => PlayerComponent.IPlayer;
    }
}