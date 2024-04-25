using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using Interpolation;
using SAIN.Components.Helpers;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Hearing
{
    public class HearingSensorPatch : ModulePatch
    {
        private static PropertyInfo HearingSensor;

        protected override MethodBase GetTargetMethod()
        {
            HearingSensor = AccessTools.Property(typeof(BotOwner), "HearingSensor");
            return AccessTools.Method(HearingSensor.PropertyType, "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____botOwner)
        {
            if (SAINPlugin.BotController?.GetSAIN(____botOwner, out _) == true)
            {
                return false;
            }
            return true;
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
            AudioHelpers.TryPlayShootSound(__instance);
        }
    }

    public class BetterAudioPatch : ModulePatch
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
                SAINSoundTypeHandler.AISoundPlayer(soundName, player);
            }
        }
    }

    public class BetterAudioPatch2 : ModulePatch
    {
        private static MethodInfo _Player;
        private static FieldInfo _PlayerBridge;
        protected override MethodBase GetTargetMethod()
        {
            _PlayerBridge = AccessTools.Field(typeof(BaseSoundPlayer), "playersBridge");
            _Player = AccessTools.PropertyGetter(_PlayerBridge.FieldType, "iPlayer");
            return AccessTools.Method(typeof(BaseSoundPlayer), "SoundAtPointEventHandler");
        }

        [PatchPrefix]
        public static void PatchPrefix(string soundName, BaseSoundPlayer __instance)
        {
            if (SAINPlugin.BotController != null && soundName.Contains("SndFuse"))
            {
                object playerBridge = _PlayerBridge.GetValue(__instance);
                Player player = _Player.Invoke(playerBridge, null) as Player;
                SAINSoundTypeHandler.AISoundPlayer(soundName, player);
            }
        }
    }

    public class BetterAudioPatch3 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SoundBank), "PlayWithConstantRolloffDistance");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref float __result, BetterSource source, EnvironmentType pos, float distance, float volume, float blendParameter, bool forceStereo, SoundBank __instance)
        {
            try
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
    public class BetterAudioPatch4 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BetterSource), "Play");
        }

        [PatchPrefix]
        public static void PatchPrefix(AudioClip clip1, AudioClip clip2, float balance, float volume, bool forceStereo, bool oneShot, BetterSource __instance)
        {
            try
            {
                if (clip1 !=  null)
                {
                    Logger.LogWarning($"{clip1.name} : {balance} : {volume} : {forceStereo} : {oneShot} ::: {__instance?.OcclusionVolumeFactor}");
                }
                if (clip2 != null)
                {
                    Logger.LogWarning($"{clip2.name} : {balance} : {volume} : {forceStereo} : {oneShot} ::: {__instance?.OcclusionVolumeFactor}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}