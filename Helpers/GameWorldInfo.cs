using Comfort.Common;
using EFT;

using SAIN.Components;
using System;
using System.Collections.Generic;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent.Classes.Enemy;

namespace SAIN.Helpers
{
    internal class GameWorldInfo
    {
        public static bool IsEnemyMainPlayer(SAINEnemy enemy)
        {
            Player player = enemy?.EnemyPlayer;
            Player mainPlayer = GameWorld?.MainPlayer;
            return
                player != null &&
                mainPlayer != null &&
                player.ProfileId == mainPlayer.ProfileId;
        }

        public static Player GetAlivePlayer(IPlayer person) => GetAlivePlayer(person?.ProfileId);

        public static Player GetAlivePlayer(string profileID) => GameWorld?.GetAlivePlayerByProfileID(profileID);

        public static GameWorld GameWorld => Singleton<GameWorld>.Instance;
        public static List<Player> AlivePlayers => GameWorld?.AllAlivePlayersList;
        public static Dictionary<string, Player> AlivePlayersDictionary => GameWorld?.allAlivePlayersByID;
    }
}
