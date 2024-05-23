using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAIN.Components;
using EFT;
using BepInEx.Logging;

namespace SAIN.Components.Helpers
{
    public class SAINSoundTypeHandler
    {
        protected static ManualLogSource Logger;
        protected static SAINBotController BotController => SAINPlugin.BotController;

        public static void AISoundFileChecker(string sound, Player player)
        {
            if (BotController == null || BotController.Bots == null || BotController.Bots.Count == 0)
            {
                return;
            }

            SAINSoundType soundType = SAINSoundType.None;
            var Item = player.HandsController.Item;
            float soundDist = 20f;

            if (Item != null)
            {
                if (Item is GrenadeClass)
                {
                    if (sound == "Pin")
                    {
                        soundType = SAINSoundType.GrenadePin;
                        soundDist = 25f;
                    }
                    if (sound == "Draw")
                    {
                        soundType = SAINSoundType.GrenadeDraw;
                        soundDist = 25f;
                    }
                }
                else if (Item is MedsClass)
                {
                    soundType = SAINSoundType.Heal;
                    if (sound == "CapRemove" || sound == "Inject")
                    {
                        soundDist = 20f;
                    }
                    else
                    {
                        soundDist = 20f;
                    }
                }
                else
                {
                    soundType = SAINSoundType.Reload;
                    if (sound == "MagOut")
                    {
                        soundDist = 20f;
                    }
                }
            }

            BotController?.AISoundPlayed?.Invoke(soundType, player.Position, player, soundDist);
        }
    }
}
