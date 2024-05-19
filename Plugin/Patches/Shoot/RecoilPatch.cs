using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using System;
using Aki.Reflection.Utils;
using System.Linq;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;

namespace SAIN.Patches.Shoot
{
    public class AimOffsetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            _endTargetPointProp = AccessTools.Property(HelpersGClass.AimDataType, "EndTargetPoint");
            return AccessTools.Method(HelpersGClass.AimDataType, "method_13");
        }

        private static PropertyInfo _endTargetPointProp;

        private static float DebugTimer;

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0, ref Vector3 ___vector3_5, ref Vector3 ___vector3_4, ref float ___float_13)
        {
            Vector3 badShootOffset = ___vector3_5;
            float aimUpgradeByTime = ___float_13;
            Vector3 aimOffset = ___vector3_4;
            Vector3 recoilOffset = ___botOwner_0.RecoilData.RecoilOffset;
            Vector3 realTargetPoint = ___botOwner_0.AimingData.RealTargetPoint;

            // Applies aiming offset, recoil offset, and scatter offsets
            // Default Setup :: Vector3 finalTarget = __instance.RealTargetPoint + badShootOffset + (AimUpgradeByTime * (AimOffset + ___botOwner_0.RecoilData.RecoilOffset));
            Vector3 finalOffset = badShootOffset + (aimUpgradeByTime * (aimOffset + recoilOffset));

            IPlayer person = ___botOwner_0?.Memory?.GoalEnemy?.Person;
            if (person != null && SAINPlugin.LoadedPreset.GlobalSettings.General.NotLookingToggle)
            {
                finalOffset += NotLookingOffset(person, ___botOwner_0);
            }

            _endTargetPointProp.SetValue(___botOwner_0.AimingData, realTargetPoint + finalOffset);
            return false;
        }

        private static Vector3 NotLookingOffset(IPlayer person, BotOwner botOwner)
        {
            if (person.IsAI == false)
            {
                float ExtraSpread = SAINNotLooking.GetSpreadIncrease(botOwner);
                if (ExtraSpread > 0)
                {
                    Vector3 vectorSpread = UnityEngine.Random.insideUnitSphere * ExtraSpread;
                    if (SAINPlugin.DebugMode && DebugTimer < Time.time)
                    {
                        DebugTimer = Time.time + 1f;
                        Logger.LogDebug($"Increasing Spread because Player isn't looking. Magnitude: [{vectorSpread.magnitude}]");
                    }
                    return vectorSpread;
                }
            }
            return Vector3.zero;
        }
    }

    public class RecoilPatch : ModulePatch
    {
        private static PropertyInfo _RecoilDataPI;

        protected override MethodBase GetTargetMethod()
        {
            _RecoilDataPI = AccessTools.Property(typeof(BotOwner), "RecoilData");
            return AccessTools.Method(_RecoilDataPI.PropertyType, "Recoil");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref Vector3 ____recoilOffset, ref BotOwner ____owner)
        {
            if (SAINPlugin.BotController == null)
            {
                Logger.LogError($"Bot Controller Null in [{nameof(RecoilPatch)}]");
                return true;
            }
            if (SAINPlugin.BotController.GetSAIN(____owner, out SAINComponentClass sain))
            {
                return false;
            }
            return true;
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
        public static bool PatchPrefix(ref Vector3 ____recoilOffset, ref BotOwner ____owner)
        {
            if (SAINPlugin.BotController == null)
            {
                Logger.LogError($"Bot Controller Null in [{nameof(LoseRecoilPatch)}]");
                return true;
            }
            if (SAINPlugin.BotController.GetSAIN(____owner, out SAINComponentClass sain))
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
        public static bool PatchPrefix(ref BotOwner ____owner)
        {
            if (SAINPlugin.BotController == null)
            {
                Logger.LogError($"Bot Controller Null in [{nameof(EndRecoilPatch)}]");
                return true;
            }
            if (SAINPlugin.BotController.GetSAIN(____owner, out SAINComponentClass sain))
            {
                return false;
            }
            return true;
        }
    }
}