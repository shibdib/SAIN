using Aki.Reflection.Patching;
using Audio.Data;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.Helpers;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Hearing
{
    public class DryShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), "DryShot");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player ____player)
        {
            SAINBotController.Instance?.PlayAISound(____player, SAINSoundType.DryFire, ____player.WeaponRoot.position, 10f);
        }
    }

    public class HearingSensorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotHearingSensor), "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ____botOwner)
        {
            return SAINPlugin.IsBotExluded(____botOwner);
        }
    }

    public class TryPlayShootSoundPatch : ModulePatch
    {
        private static PropertyInfo AIFlareEnabled;

        protected override MethodBase GetTargetMethod()
        {
            AIFlareEnabled = AccessTools.Property(typeof(AIData), "Boolean_0");
            return AccessTools.Method(typeof(AIData), "TryPlayShootSound");
        }

        [PatchPrefix]
        public static bool PatchPrefix(AIData __instance)
        {
            AIFlareEnabled.SetValue(__instance, true);
            return false;
        }
    }

    public class OnMakingShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "OnMakingShot");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance)
        {
            SAINBotController.Instance?.PlayShootSound(__instance);
        }
    }

    public class SoundClipNameCheckerPatch : ModulePatch
    {
        private static MethodInfo _Player;
        private static FieldInfo _PlayerBridge;

        protected override MethodBase GetTargetMethod()
        {
            _PlayerBridge = AccessTools.Field(typeof(BaseSoundPlayer), "playersBridge");
            _Player = AccessTools.PropertyGetter(_PlayerBridge.FieldType, "iPlayer");
            return AccessTools.Method(typeof(BaseSoundPlayer), "SoundEventHandler");
        }

        [PatchPrefix]
        public static void PatchPrefix(string soundName, BaseSoundPlayer __instance)
        {
            if (SAINPlugin.BotController != null)
            {
                object playerBridge = _PlayerBridge.GetValue(__instance);
                Player player = _Player.Invoke(playerBridge, null) as Player;
                SAINSoundTypeHandler.AISoundFileChecker(soundName, player);
            }
        }
    }

    public class LootingSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "method_39");
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance, int count)
        {
            if (count > 0)
            {
                SAINBotController.Instance?.PlayAISound(__instance, SAINSoundType.Looting, __instance.Position, 60f);
            }
        }
    }

    public class FallSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "PlayGroundedSound");
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance, float fallHeight, float jumpHeight, float ____nextJumpAfter)
        {
            if (Time.realtimeSinceStartup < ____nextJumpAfter)
            {
                return;
            }
            if (!__instance.method_41())
            {
                return;
            }
            float num = Mathf.Max(fallHeight, jumpHeight);
            if (num > __instance.LandingThreshold && __instance.CheckSurface())
            {
                float baseRange = 60;
                float modifier = Mathf.InverseLerp(0.1f, __instance.LandingThreshold * 2.5f, num);
                float range = baseRange * modifier;

                if (__instance.IsYourPlayer)
                    Logger.LogDebug($"FallSound Range {range} Mod: {modifier}");

                SAINBotController.Instance?.PlayAISound(__instance, SAINSoundType.FootStep, __instance.Position, range);
            }
        }
    }

    public class TurnSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "method_47");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance, float ____lastTimeTurnSound, float ___maxLengthTurnSound)
        {
            if (Time.time - ____lastTimeTurnSound >= ___maxLengthTurnSound)
            {
                float num = Mathf.InverseLerp(1f, 360f + (1f - __instance.MovementContext.PoseLevel) * 360f, Mathf.Abs(__instance.MovementContext.AverageRotationSpeed.Avarage));
                float volume = num * __instance.MovementContext.CovertMovementVolume;

                if (__instance.IsYourPlayer)
                    Logger.LogDebug($"Turn Sound: {volume * 40f} Mod: {volume}");

                SAINBotController.Instance?.PlayAISound(__instance, SAINSoundType.FootStep, __instance.Position, volume * 40f);
            }
        }
    }

    public class ProneSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "PlaySoundBank");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance, ref string soundBank)
        {
            if (soundBank == "Prone"
                && __instance.SinceLastStep >= 0.5f
                && SAINPlugin.BotController?.AISoundPlayed != null
                && __instance.CheckSurface())
            {
                SAINBotController.Instance?.PlayAISound(__instance, SAINSoundType.FootStep, __instance.Position, 50f);
            }
        }
    }

    public class AimSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "method_46");
        }

        [PatchPrefix]
        public static void PatchPrefix(float volume, Player __instance)
        {
            float baseRange = 50f;
            float range = __instance.MovementContext.CovertEquipmentNoise * volume * baseRange;
            if (__instance.IsYourPlayer)
                Logger.LogInfo($"Gear Sound Range {range}");
            SAINBotController.Instance?.PlayAISound(__instance, SAINSoundType.GearSound, __instance.Position, __instance.MovementContext.CovertEquipmentNoise * volume);
        }
    }

    public class SetInHandsGrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "SetInHands",
                new[] { typeof(GrenadeClass), typeof(Callback<IHandsThrowController>) });
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance)
        {
            if (SAINPlugin.BotController != null)
            {
                SAINPlugin.BotController.AISoundPlayed?.Invoke(SAINSoundType.GrenadeDraw, __instance.Position, __instance, 35f);
            }
        }
    }

    public class SetInHandsFoodPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "SetInHands",
                new[] { typeof(FoodClass), typeof(float), typeof(int), typeof(Callback<GInterface130>) });
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance)
        {
            SAINBotController.Instance?.PlayAISound(__instance, SAINSoundType.Food, __instance.Position, 40f);
        }
    }

    public class SetInHandsMedsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "SetInHands",
                new[] { typeof(MedsClass), typeof(EBodyPart), typeof(int), typeof(Callback<GInterface130>) });
        }

        [PatchPrefix]
        public static void PatchPrefix(MedsClass meds, Player __instance)
        {
            SAINSoundType soundType = SAINSoundType.Heal;
            if (meds != null && meds.HealthEffectsComponent.AffectsAny(new EDamageEffectType[] { EDamageEffectType.DestroyedPart }))
            {
                soundType = SAINSoundType.Surgery;
            }
            SAINBotController.Instance?.PlayAISound(__instance, soundType, __instance.Position, 45f);
        }
    }
}