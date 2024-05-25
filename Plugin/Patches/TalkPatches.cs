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
        public static bool PatchPrefix(Player __instance, EPhraseTrigger @event, ETagStatus mask, bool aggressive)
        {
            if (__instance?.HealthController?.IsAlive == false)
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

            BotOwner botOwner = __instance?.AIData?.BotOwner;
            if (botOwner == null)
            {
                SAINPlugin.BotController?.PlayerTalk?.Invoke(@event, mask, __instance);
                return true;
            }

            // If handling of bots talking is disabled, let the original method run
            if (SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching)
            {
                return true;
            }
            if (SAINPlugin.IsBotExluded(botOwner))
            {
                return true;
            }
            return false;
        }
    }

    public class BotTalkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotTalk), "Say");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0, EPhraseTrigger type, ETagStatus? additionalMask = null)
        {
            // If handling of bots talking is disabled, let the original method run
            if (SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching)
            {
                return true;
            }
            if (___botOwner_0.HealthController?.IsAlive == false)
            {
                return false;
            }
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return true;
            }
            return false;
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
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            // If handling of bots talking is disabled, let the original method run
            if (SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching)
            {
                return true;
            }
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return true;
            }
            return false;
        }
    }
}