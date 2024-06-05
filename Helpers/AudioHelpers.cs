using Comfort.Common;
using EFT;
using EFT.Weather;
using SAIN.Components;
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
                float range = 125f;
                AISoundType soundType = AISoundType.gun;

                FirearmController controller = player.HandsController as FirearmController;
                if (controller?.Item != null)
                {
                    GearInfoContainer info = SAINGearInfoHandler.GetGearInfo(player);
                    if (info != null)
                    {
                        var weaponInfo = info.GetWeaponInfo(controller.Item);
                        if (weaponInfo != null)
                        {
                            weaponInfo.TryCalculate();
                            range = weaponInfo.CalculatedAudibleRange;
                            soundType = weaponInfo.AISoundType;
                        }

                        info.PlayShootSound(range * RainSoundModifier(), soundType);
                        return;
                    }
                }

                // If for some reason we can't get the weapon info on this player, just play the default sound
                if (nextShootTime < Time.time)
                {
                    nextShootTime = Time.time + 0.1f;
                    SAINSoundType sainType = soundType == AISoundType.gun ? SAINSoundType.Gunshot : SAINSoundType.SuppressedGunShot;
                    SAINPlugin.BotController?.PlayAISound(player, sainType, player.WeaponRoot.position, range * RainSoundModifier());
                    Logger.LogWarning($"Could not find Weapon Info for [{player.Profile.Nickname}]!");
                }
            }
        }

        private static float nextShootTime;

        public static float RainSoundModifier()
        {
            if (WeatherController.Instance?.WeatherCurve == null)
                return 1f;

            if (RainCheckTimer < Time.time)
            {
                RainCheckTimer = Time.time + 5f;
                // Grabs the current rain Rounding
                float Rain = WeatherController.Instance.WeatherCurve.Rain;
                RainModifier = 1f;
                float max = 1f;
                float rainMin = 0.65f;

                Rain = InverseScaling(Rain, rainMin, max);

                // Combines ModifiersClass and returns
                RainModifier *= Rain;
            }
            return RainModifier;
        }

        public static float InverseScaling(float value, float min, float max)
        {
            // Inverse
            float InverseValue = 1f - value;

            // Scaling
            float ScaledValue = (InverseValue * (max - min)) + min;

            value = ScaledValue;

            return value;
        }

        private static float RainCheckTimer = 0f;
        private static float RainModifier = 1f;
    }
}