using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.EnvironmentEffect;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Generic
{
    public class InBunkerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnvironmentManager), "SetTriggerForPlayer");
        }

        [PatchPrefix]
        public static void PatchPrefix(IPlayer player, IndoorTrigger trigger)
        {
        }
    }

    public class EncumberedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass681), "UpdateWeightLimits");
        }

        [PatchPrefix]
        public static bool PatchPrefix(bool ___bool_7, GClass681.IObserverToPlayerBridge ___iobserverToPlayerBridge_0, GClass681 __instance)
        {
            if (!___bool_7)
            {
                bool isAI = ___iobserverToPlayerBridge_0.iPlayer.IsAI;

                BackendConfigSettingsClass.GClass1368 stamina = Singleton<BackendConfigSettingsClass>.Instance.Stamina;

                float carryWeightModifier = ___iobserverToPlayerBridge_0.Skills.CarryingWeightRelativeModifier;
                float d = carryWeightModifier * carryWeightModifier;

                float absoluteWeightModifier = ___iobserverToPlayerBridge_0.iPlayer.HealthController.CarryingWeightAbsoluteModifier;
                Vector2 b = new Vector2(absoluteWeightModifier, absoluteWeightModifier);

                BackendConfigSettingsClass.InertiaSettings inertia = Singleton<BackendConfigSettingsClass>.Instance.Inertia;
                float strength = (float)___iobserverToPlayerBridge_0.Skills.Strength.SummaryLevel;
                Vector3 b2 = new Vector3(inertia.InertiaLimitsStep * strength, inertia.InertiaLimitsStep * strength, 0f);

                Logger.LogDebug($"Strength {strength}");
                Logger.LogDebug($"carryWeightModifier {carryWeightModifier}");
                Logger.LogDebug($"absoluteWeightModifier {absoluteWeightModifier}");
                Logger.LogDebug($"d {d} : b {b.magnitude} : b2 {b2.magnitude}");

                __instance.BaseInertiaLimits = inertia.InertiaLimits + b2;
                __instance.WalkOverweightLimits = stamina.WalkOverweightLimits * d + b;
                __instance.BaseOverweightLimits = stamina.BaseOverweightLimits * d + b;
                __instance.SprintOverweightLimits = stamina.SprintOverweightLimits * d + b;
                __instance.WalkSpeedOverweightLimits = stamina.WalkSpeedOverweightLimits * d + b;

                Logger.LogDebug($"BaseInertiaLimits {__instance.BaseInertiaLimits.magnitude}");
                Logger.LogDebug($"WalkOverweightLimits {__instance.WalkOverweightLimits.magnitude}");
                Logger.LogDebug($"BaseOverweightLimits {__instance.BaseOverweightLimits.magnitude}");
                Logger.LogDebug($"SprintOverweightLimits {__instance.SprintOverweightLimits.magnitude}");
                Logger.LogDebug($"WalkSpeedOverweightLimits {__instance.WalkSpeedOverweightLimits.magnitude}");

                return false;
            }
            return true;
        }
    }

    public class DoorOpenerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.Update));
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____owner, ref bool __result)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.General.NewDoorOpening)
            {
                return true;
            }
            if (SAINPlugin.IsBotExluded(____owner))
            {
                return true;
            }

            if (SAINEnableClass.GetSAIN(____owner, out var botComponent, nameof(DoorOpenerPatch)) &&
                botComponent.HasEnemy)
            {
                __result = botComponent.DoorOpener.Update();
                return false;
            }
            return true;
        }
    }
}