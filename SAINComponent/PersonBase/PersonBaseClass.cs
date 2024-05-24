using EFT;
using SAIN.Helpers;
using System;

namespace SAIN.SAINComponent.BaseClasses
{
    public abstract class PersonBaseClass
    {
        public PersonBaseClass(IPlayer iPlayer)
        {
            IPlayer = iPlayer;
            Player = EFTInfo.GetPlayer(iPlayer);
        }

        public bool PlayerNull => IPlayer == null || IPlayer.Transform == null;
        public IPlayer IPlayer { get; private set; }
        public Player Player { get; private set; }
    }
}