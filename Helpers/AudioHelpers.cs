using Comfort.Common;
using EFT;
using EFT.Weather;
using SAIN.Components;
using SAIN.Components.BotController;
using SAIN.Components.PlayerComponentSpace;
using UnityEngine;
using static EFT.Player;

namespace SAIN.Helpers
{
    public class AudioHelpers
    {
        public static void TryPlayShootSound(Player player)
        {
            if (player != null && player.HealthController.IsAlive)
            {
                PlayerComponent component = SAINGameworldComponent.Instance?.PlayerTracker.GetPlayerComponent(player);
                if (component?.Equipment.PlayAIShootSound() == true)
                {
                    return;
                }

                // If for some reason we can't get the weapon info on this player, just play the default sound
                if (nextShootTime < Time.time)
                {
                    nextShootTime = Time.time + 0.05f;

                    float range = 125f;
                    var weather = SAINWeatherClass.Instance;
                    if (weather != null)
                    {
                        range *= weather.RainSoundModifier;
                    }

                    SAINPlugin.BotController?.PlayAISound(player, SAINSoundType.Gunshot, player.WeaponRoot.position, range);
                    Logger.LogWarning($"Could not find Weapon Info for [{player.Profile.Nickname}]!");
                }
            }
        }

        private static float nextShootTime;
    }
}