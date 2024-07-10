using EFT;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public abstract class PersonBase
    {
        public PlayerComponent PlayerComponent => _personInfo.PlayerComponent;
        public IPlayer IPlayer => _personInfo.IPlayer;
        public Player Player => _personInfo.Player;
        public string ProfileID => _personInfo.Profile.ProfileId;
        public string Nickname => _personInfo.Profile.Nickname;

        public PersonBase(PlayerData player)
        {
            _personInfo = player;
        }

        private readonly PlayerData _personInfo;
    }
}