using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using System.Reflection;
using System;
using SAIN.SAINComponent;
using SAIN.Components.BotController;
using UnityEngine;

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
                BotSpawnController.Instance.AddBot(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($" SAIN Add Component Error: {ex}");
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
            GameWorldHandler.Create(gameObject);
        }
    }
}
