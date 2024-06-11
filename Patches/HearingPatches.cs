using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.Helpers;
using System.Reflection;
using Systems.Effects;
using UnityEngine;

namespace SAIN.Patches.Hearing
{
    public class TreeSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TreeInteractive), "method_0");
        }

        [PatchPostfix]
        public static void Patch(Vector3 soundPosition, BetterSource source, GInterface94 player, SoundBank ____soundBank)
        {
            if (player.iPlayer != null)
            {
                float baseRange = 50f;
                if (____soundBank != null)
                {
                    baseRange = ____soundBank.Rolloff * player.SoundRadius;
                }
                //Logger.LogDebug($"Playing Bush Sound Range: {baseRange}");
                SAINPlugin.BotController?.BotHearing.PlayAISound(player.iPlayer.ProfileId, SAINSoundType.Bush, soundPosition, baseRange, 1f);
            }
        }
    }

    public class DoorOpenSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), "StartInteraction");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player ____player)
        {
            float baseRange = 40f;
            SAINPlugin.BotController?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.Door, ____player.Position, baseRange, 1f);
        }
    }

    public class DoorBreachSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), "PlayBreachSound");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player ____player)
        {
            float baseRange = 70f;
            SAINPlugin.BotController?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.Door, ____player.Position, baseRange, 1f);
        }
    }

    public class JumpSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), "method_2");
        }

        [PatchPrefix]
        public static bool PatchPrefix(Player ____player, ref float ____nextJumpNoise)
        {
            if (____nextJumpNoise < Time.time)
            {
                ____nextJumpNoise = Time.time + 0.5f;
                float baseRange = 55f;
                SAINPlugin.BotController?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.Jump, ____player.Position, baseRange, 1f);
            }
            return false;
        }
    }

    public class FootstepSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), "method_1");
        }

        [PatchPrefix]
        public static bool PatchPrefix(Player ____player, Vector3 motion, MovementContext __instance, ref float ____nextStepNoise)
        {
            if (____nextStepNoise < Time.time)
            {
                ____nextStepNoise = Time.time + 0.33f;
                float baseRange = 35f;

                if (motion.y < 0.2f && motion.y > -0.2f)
                {
                    motion.y = 0f;
                }
                if (motion.sqrMagnitude < 1E-06f)
                {
                    return false;
                }

                float num = ____player.Speed;
                if (____player.IsSprintEnabled)
                {
                    num = 2f;
                }
                float num2 = Mathf.Clamp(0.5f * ____player.PoseLevel + 0.5f, 0f, 1f);
                num *= num2;
                float num3 = ____player.IsSprintEnabled ? 1f : __instance.CovertMovementVolumeBySpeed;
                float volume = (num3 + num) / 2f;

                SAINPlugin.BotController?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.FootStep, ____player.Position, baseRange, volume);
            }
            return false;
        }
    }

    public class DryShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), "DryShot");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player ____player)
        {
            float baseRange = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_DryFire;
            SAINPlugin.BotController?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.DryFire, ____player.WeaponRoot.position, baseRange, 1f);
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
            SAINBotController.Instance?.BotHearing.PlayShootSound(__instance.ProfileId);
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
                float baseRange = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Looting;
                SAINPlugin.BotController?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.Looting, __instance.Position, baseRange, 1f);
            }
        }
    }

    public class FallSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "PlayGroundedSound");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance, float fallHeight, float jumpHeight, float ____nextJumpAfter)
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
                float baseRange = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.MaxSoundRange_FallLanding;
                float volume = Mathf.InverseLerp(0.1f, __instance.LandingThreshold * 2.5f, num);
                SAINPlugin.BotController?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.FootStep, __instance.Position, baseRange, volume);
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
                float baseRange = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_MovementTurnSkid;
                SAINPlugin.BotController?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.TurnSound, __instance.Position, baseRange, volume);
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
                && __instance.CheckSurface())
            {
                float range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Prone;
                SAINPlugin.BotController?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.Prone, __instance.Position, range, 1f);
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
            float baseRange = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_AimingandGearRattle;
            SAINPlugin.BotController?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.GearSound, __instance.Position, baseRange, volume);
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
            float range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_GrenadePinDraw;
            SAINPlugin.BotController?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.GrenadeDraw, __instance.Position, range, 1f);
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
            float range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_EatDrink;
            SAINPlugin.BotController?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.Food, __instance.Position, range, 1f);
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
            SAINSoundType soundType;
            float range;
            if (meds != null && meds.HealthEffectsComponent.AffectsAny(new EDamageEffectType[] { EDamageEffectType.DestroyedPart }))
            {
                soundType = SAINSoundType.Surgery;
                range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Surgery;
            }
            else
            {
                soundType = SAINSoundType.Heal;
                range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Healing;
            }
            SAINPlugin.BotController?.BotHearing.PlayAISound(__instance.ProfileId, soundType, __instance.Position, range, 1f);
        }
    }

    public class BulletImpactPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EffectsCommutator), "PlayHitEffect");
        }

        [PatchPostfix]
        public static void PatchPostfix(EftBulletClass info)
        {
            if (SAINPlugin.BotController != null)
            {
                //Vector3 position = __instance.transform.position + ___vector3_0;
                SAINPlugin.BotController.BulletImpacted(info);
            }
        }
    }
}