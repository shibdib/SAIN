using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAIN.Components;
using EFT;
using BepInEx.Logging;
using UnityEngine;

namespace SAIN.Components.Helpers
{
    public class SAINSoundTypeHandler
    {
        public static void AISoundFileChecker(string sound, Player player)
        {
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
                        soundDist = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_GrenadePinDraw;
                    }
                    if (sound == "Draw")
                    {
                        soundType = SAINSoundType.GrenadeDraw;
                        soundDist = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_GrenadePinDraw;
                    }
                }
                else if (Item is MedsClass)
                {
                    soundType = SAINSoundType.Heal;
                    soundDist = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Healing;
                    if (sound == "CapRemove" || sound == "Inject")
                    {
                        soundDist *= 0.5f;
                    }
                }
                else
                {
                    soundType = SAINSoundType.Reload;
                    soundDist = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Reload;
                }
            }

            SAINBotController.Instance?.PlayAISound(player, soundType, player.Position + Vector3.up, soundDist);
        }
    }
}
