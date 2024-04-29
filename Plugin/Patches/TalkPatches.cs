using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using SAIN.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using Comfort.Common;
using SAIN.Helpers;

namespace SAIN.Patches.Talk
{
    public class PlayerTalkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "Say");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref Player __instance, ref EPhraseTrigger @event, ref ETagStatus mask, ref bool aggressive)
        {
            // If handling of bots talking is disabled, let the original method run
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.DisableBotTalkPatching || __instance.HealthController?.IsAlive == false)
            {
                return true;
            }

            switch (@event)
            {
                case EPhraseTrigger.OnDeath:
                case EPhraseTrigger.OnBeingHurt:
                case EPhraseTrigger.OnAgony:
                    return true;
                default:
                    break;
            }

            if (!__instance.IsAI)
            {
                SAINPlugin.BotController?.PlayerTalk?.Invoke(@event, mask, __instance);
                return true;
            }
            else if (SAINPlugin.GetSAIN(__instance.AIData.BotOwner, out _, nameof(PlayerTalkPatch)))
            {
                return false;
            }
            return true;
        }
    }

    public class BotTalkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotTalk), "Say");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0, EPhraseTrigger type, ETagStatus? additionalMask = null)
        {
            // If handling of bots talking is disabled, let the original method run
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.DisableBotTalkPatching)
            {
                return true;
            }
            if (___botOwner_0.HealthController?.IsAlive == false)
            {
                return false;
            }
            if (SAINPlugin.GetSAIN(___botOwner_0, out _, nameof(BotTalkPatch)))
            {
                return false;
            }
            return true;
        }
    }

    public class BotTalkManualUpdatePatch : ModulePatch
    {
        private static PropertyInfo BotTalk;

        protected override MethodBase GetTargetMethod()
        {
            BotTalk = AccessTools.Property(typeof(BotOwner), "BotTalk");
            return AccessTools.Method(BotTalk.PropertyType, "ManualUpdate");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0)
        {
            // If handling of bots talking is disabled, let the original method run
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.DisableBotTalkPatching)
            {
                return true;
            }
            if (SAINPlugin.GetSAIN(___botOwner_0, out _, nameof(BotTalkPatch)))
            {
                return false;
            }
            return true;
        }
    }
}