using SPT.Reflection.Patching;
using EFT;
using HarmonyLib;
using System.Reflection;
using System;
using SAIN.SAINComponent;
using SAIN.Components.BotController;
using UnityEngine;
using SAIN.Components.PlayerComponentSpace;
using UnityEngine.UIElements;
using SAIN.Components;
using EFT.Interactive;

namespace SAIN.Patches.Components
{
    internal class AddBotComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "method_10");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref BotOwner __instance)
        {
            try {
                if (__instance.BotState != EBotState.ActiveFail) {
                    BotSpawnController.Instance.AddBot(__instance);
                }
                else {
                    Logger.LogDebug($"{__instance.name} failed EFT Init, skipping adding SAIN components");
                }
            }
            catch (Exception ex) {
                Logger.LogError($" SAIN Add Bot Error: {ex}");
            }
        }
    }

    internal class AddLightComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(VolumetricLight), "Awake");
        }

        [PatchPostfix]
        public static void PatchPostfix(VolumetricLight __instance)
        {
            SAIN.Components.BotLightTracker.AddLight(__instance.Light);
        }
    }

    internal class AddLightComponentPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LampController), "Awake");
        }

        [PatchPostfix]
        public static void PatchPostfix(LampController __instance)
        {
            int count = 0;
            foreach (var light in __instance.Lights) {
                SAIN.Components.BotLightTracker.AddLight(light);
                Logger.LogDebug($"Added Light [{count}]");
                count++;
            }
        }
    }

    internal class AddGameWorldPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorldUnityTickListener), "Create");
        }

        [PatchPostfix]
        public static void PatchPostfix(GameObject gameObject)
        {
            try {
                GameWorldHandler.Create(gameObject);
            }
            catch (Exception ex) {
                Logger.LogError($" SAIN Init Gameworld Error: {ex}");
            }
        }
    }

    internal class GetBotController : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "method_0");
        }

        [PatchPrefix]
        public static void PatchPrefix(BotsController __instance)
        {
            var controller = SAINBotController.Instance;
            if (controller != null && controller.DefaultController == null) {
                controller.DefaultController = __instance;
            }
        }
    }

    internal class GetBotSpawner : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), "AddPlayer");
        }

        [PatchPostfix]
        public static void PatchPostfix(BotSpawner __instance)
        {
            var controller = SAINBotController.Instance;
            if (controller != null && controller.BotSpawner == null) {
                controller.BotSpawner = __instance;
            }
        }
    }
}