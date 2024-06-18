using EFT;
using SAIN.Components.PlayerComponentSpace;
using System.Collections;
using UnityEngine;

namespace SAIN.Components.BotControllerSpace.Classes
{
    public class BotHearingClass : SAINControl
    {
        public BotHearingClass(SAINBotController botController) : base(botController)
        {
        }

        public void PlayerTalked(EPhraseTrigger phrase, ETagStatus mask, Player player)
        {
            if (phrase == EPhraseTrigger.OnDeath)
            {
                return;
            }
            if (player == null ||
                Bots == null ||
                player.HealthController.IsAlive == false)
            {
                return;
            }

            BotController.PlayerTalk?.Invoke(phrase, mask, player);
        }

        public void PlayShootSound(string profileId)
        {
            BotController.StartCoroutine(playShootSoundCoroutine(profileId));
        }

        private IEnumerator playShootSoundCoroutine(string profileId)
        {
            yield return null;

            PlayerComponent component = GameWorldComponent.Instance?.PlayerTracker.GetPlayerComponent(profileId);
            if (component != null && component.IsActive)
            {
                component.Equipment.PlayAIShootSound();
            }
        }

        public void PlayAISound(PlayerComponent playerComponent, SAINSoundType soundType, Vector3 position, float range, float volume, bool limitFreq)
        {
            if (playerComponent == null)
            {
                Logger.LogError("Player Component Null");
                return;
            }
            if (!playerComponent.IsActive)
            {
                return;
            }
            if (limitFreq && 
                !playerComponent.AIData.AISoundPlayer.ShallPlayAISound(range))
            {
                return;
            }

            BotController.StartCoroutine(
                delaySoundHeard(soundType, playerComponent, position, range, volume));
        }

        public void PlayAISound(string profileId, SAINSoundType soundType, Vector3 position, float range, float volume)
        {
            if (profileId.IsNullOrEmpty())
            {
                return;
            }

            PlayerComponent playerComponent = SAINGameWorld.PlayerTracker.GetPlayerComponent(profileId);

            if (playerComponent != null && playerComponent.IsActive)
                PlayAISound(playerComponent, soundType, position, range, volume, true);
        }

        private IEnumerator delaySoundHeard(SAINSoundType soundType, PlayerComponent playerComponent, Vector3 position, float range, float volume, float delay = 0.1f)
        {
            BotController.AISoundPlayed?.Invoke(soundType, position, playerComponent, range, volume);
            if (playerComponent.Player.IsYourPlayer)
            {
                Logger.LogDebug($"SoundType [{soundType}] FinalRange: {range * volume} Base Range {range} : Volume: {volume}");
            }

            yield return new WaitForSeconds(delay);

            if (playerComponent == null ||
                playerComponent.Player == null ||
                !playerComponent.Player.HealthController.IsAlive)
            {
                yield break;
            }

            playBotEvent(playerComponent.Player, position, range * volume, soundType);
        }

        private void playBotEvent(Player player, Vector3 position, float range, SAINSoundType soundType)
        {
            AISoundType baseSoundType = getBaseSoundType(soundType);
            BotController.BotEventHandler?.PlaySound(player, position, range, baseSoundType);
        }

        private AISoundType getBaseSoundType(SAINSoundType soundType)
        {
            AISoundType baseSoundType;
            switch (soundType)
            {
                case SAINSoundType.Gunshot:
                    baseSoundType = AISoundType.gun;
                    break;

                case SAINSoundType.SuppressedGunShot:
                    baseSoundType = AISoundType.silencedGun;
                    break;

                default:
                    baseSoundType = AISoundType.step;
                    break;
            }
            return baseSoundType;
        }
    }
}