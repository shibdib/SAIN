using Aki.Reflection.Patching;
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

namespace SAIN.Patches.Components
{
    public class AddComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "method_10");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref BotOwner __instance)
        {
            try
            {
                if (__instance.BotState != EBotState.ActiveFail)
                {
                    BotSpawnController.Instance.AddBot(__instance);
                }
                else
                {
                    Logger.LogDebug($"{__instance.name} failed EFT Init, skipping adding SAIN components");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($" SAIN Add Bot Error: {ex}");
            }
        }
    }

    public class AddGameWorldPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorldUnityTickListener), "Create");
        }

        [PatchPostfix]
        public static void PatchPostfix(GameObject gameObject)
        {
            try
            {
                GameWorldHandler.Create(gameObject);
            }
            catch (Exception ex)
            {
                Logger.LogError($" SAIN Init Gameworld Error: {ex}");
            }
        }
    }
}
