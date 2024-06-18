using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using SAIN.SAINComponent;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Shoot.Recoil
{
    internal class OnMakingShotRecoilPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(Player).GetMethod("OnMakingShot");

        [PatchPrefix]
        public static void PatchPrefix(ref Player __instance)
        {
            if (SAINEnableClass.GetSAIN(__instance, out var sain, nameof(OnMakingShotRecoilPatch)))
            {
                sain.Info.WeaponInfo.Recoil.WeaponShot();
            }
        }
    }

    internal class RecoilPatch : ModulePatch
    {
        private static PropertyInfo _RecoilDataPI;

        protected override MethodBase GetTargetMethod()
        {
            _RecoilDataPI = AccessTools.Property(typeof(BotOwner), "RecoilData");
            return AccessTools.Method(_RecoilDataPI.PropertyType, "Recoil");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ____owner)
        {
            return true;
        }
    }

    internal class LoseRecoilPatch : ModulePatch
    {
        private static PropertyInfo _RecoilDataPI;

        protected override MethodBase GetTargetMethod()
        {
            _RecoilDataPI = AccessTools.Property(typeof(BotOwner), "RecoilData");
            return AccessTools.Method(_RecoilDataPI.PropertyType, "LosingRecoil");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref Vector3 ____recoilOffset, BotOwner ____owner)
        {
            return true;
        }
    }

    internal class EndRecoilPatch : ModulePatch
    {
        private static PropertyInfo _RecoilDataPI;

        protected override MethodBase GetTargetMethod()
        {
            _RecoilDataPI = AccessTools.Property(typeof(BotOwner), "RecoilData");
            return AccessTools.Method(_RecoilDataPI.PropertyType, "CheckEndRecoil");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ____owner)
        {
            return true;
        }
    }
}