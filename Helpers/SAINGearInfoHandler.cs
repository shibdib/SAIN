using EFT;
using System.Collections.Generic;

namespace SAIN
{
    public class SAINGearInfoHandler
    {
        public static void Update()
        {
            if (GameWorldHandler.SAINGameWorld == null)
            {
                if (WeaponInfos.Count > 0)
                {
                    ClearCache();
                }
                return;
            }
        }

        public static GearInfoContainer GetGearInfo(Player player)
        {
            if (!WeaponInfos.ContainsKey(player))
            {
                WeaponInfos.Add(player, new GearInfoContainer(player));
                player.OnPlayerDeadOrUnspawn += RemovePlayer;
            }
            WeaponInfos[player].CheckForNewGear();
            return WeaponInfos[player];
        }

        private static void RemovePlayer(Player player)
        {
            if (player == null)
            {
                return;
            }
            if (WeaponInfos.ContainsKey(player))
            {
                WeaponInfos[player].ClearCache();
                WeaponInfos.Remove(player);
            }
            player.OnPlayerDeadOrUnspawn -= RemovePlayer;
        }

        public static void ClearCache()
        {
            foreach (var info in WeaponInfos)
            {
                info.Value?.ClearCache();
                if (info.Value?.Player != null)
                {
                    info.Value.Player.OnPlayerDeadOrUnspawn -= RemovePlayer;
                }
            }
            WeaponInfos.Clear();
        }

        public static Dictionary<Player, GearInfoContainer> WeaponInfos = new Dictionary<Player, GearInfoContainer>();
    }
}
