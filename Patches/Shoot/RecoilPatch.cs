using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using SAIN.SAINComponent;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Shoot
{
    public class RecoilPatch : ModulePatch
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
            return SAINPlugin.IsBotExluded(____owner);
        }
    }

    public class LoseRecoilPatch : ModulePatch
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
            if (SAINPlugin.IsBotExluded(____owner))
            {
                return true;
            }
            if (SAINPlugin.BotController == null)
            {
                Logger.LogError($"Bot Controller Null in [{nameof(LoseRecoilPatch)}]");
                return true;
            }
            if (SAINPlugin.BotController.GetSAIN(____owner, out BotComponent sain))
            {
                var recoil = sain?.Info?.WeaponInfo?.Recoil;
                if (recoil != null)
                {
                    ____recoilOffset = recoil.CurrentRecoilOffset;
                    return false;
                }
            }
            return true;
        }
    }

    public class EndRecoilPatch : ModulePatch
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
            if (SAINPlugin.IsBotExluded(____owner))
            {
                return true;
            }
            if (SAINPlugin.BotController == null)
            {
                Logger.LogError($"Bot Controller Null in [{nameof(EndRecoilPatch)}]");
                return true;
            }
            if (SAINPlugin.BotController.GetSAIN(____owner, out BotComponent sain))
            {
                return false;
            }
            return true;
        }
    }
}