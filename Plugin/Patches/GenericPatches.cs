using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.Components;
using SAIN.Helpers;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using DrakiaXYZ.BigBrain.Brains;
using UnityEngine.AI;
using SAIN.Layers;
using Comfort.Common;
using EFT.HealthSystem;
using Aki.Reflection.Utils;
using System;
using System.Linq;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent;
using Audio.Data;
using EFT.UI;
using System.Collections;

namespace SAIN.Patches.Generic
{
    public class StopSetToNavMeshPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0)
        {
            if (SAINPlugin.GetSAIN(___botOwner_0, out var sain, nameof(StopSetToNavMeshPatch)) && sain.Mover.SprintController.Running)
            {
                return false;
            }
            return true;
        }
    }
    public class TurnDamnLightOffPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotWeaponSelector), "method_1");
        }

        [PatchPrefix]
        public static void PatchPrefix(ref BotOwner ___botOwner_0)
        {
            // Try to turn a gun's light off before swapping weapon.
            ___botOwner_0?.BotLight?.TurnOff(false, true);
        }
    }
    public class IsSameWayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PathControllerClass), "IsSameWay", new[] { typeof(Vector3), typeof(Vector3) });
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
    public class ShallRunAwayGrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotBewareGrenade), "ShallRunAway");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class RotateClampPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "Rotate");
        }

        [PatchPrefix]
        public static void PatchPrefix(ref Player __instance, ref bool ignoreClamp)
        {
            if (__instance?.IsAI == true 
                && __instance.IsSprintEnabled)
            {
                ignoreClamp = true;
            }
        }
    }

    public class HealCancelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass410), "CancelCurrent");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0, ref bool ___bool_1, ref bool ___bool_0, ref float ___float_0)
        {
            if (___bool_1)
            {
                return false;
            }
            if (___bool_0)
            {
                if (___float_0 < Time.time + 3f)
                {
                    ___float_0 += 5f;
                }
                ___bool_1 = true;
                ___botOwner_0.WeaponManager.Selector.TakePrevWeapon();
                ___botOwner_0.StartCoroutine(TakePrevWeapon(___botOwner_0));
            }
            return false;
        }

        private static IEnumerator TakePrevWeapon(BotOwner bot)
        {
            yield return new WaitForSeconds(0.5f);
            if (bot != null && bot.GetPlayer != null && bot.GetPlayer.HealthController.IsAlive)
            {
                bot.WeaponManager?.Selector?.TakePrevWeapon();
            }
        }
    }
    public class AimRotateSpeedPatch : ModulePatch
    {
        private static Type _aimingDataType;
        private static MethodInfo _aimingDataMethod11;

        protected override MethodBase GetTargetMethod()
        {
            //return AccessTools.Method(typeof(GClass544), "method_7");
            _aimingDataType = PatchConstants.EftTypes.Single(x => x.GetProperty("LastSpreadCount") != null && x.GetProperty("LastAimTime") != null);
            _aimingDataMethod11 = AccessTools.Method(_aimingDataType, "method_11");
            return _aimingDataMethod11;
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0, ref Vector3 ___vector3_2, ref Vector3 ___vector3_0, Vector3 dir)
        {
            ___vector3_2 = dir;
            ___botOwner_0.Steering.LookToDirection(dir, 250);
            ___botOwner_0.Steering.SetYByDir(___vector3_0);
            return false;
        }
    }

    internal class OnMakingShotRecoilPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(Player).GetMethod("OnMakingShot");
        [PatchPrefix]
        public static void PatchPrefix(ref Player __instance)
        {
            if (SAINPlugin.GetSAIN(__instance, out var sain, nameof(OnMakingShotRecoilPatch)))
            {
                sain.Info.WeaponInfo.Recoil.WeaponShot();
            }
        }
    }

    internal class GetHitPatch : ModulePatch
    {
        private static Type _aimingDataType;
        private static MethodInfo _aimingDataGetHit;

        protected override MethodBase GetTargetMethod()
        {
            _aimingDataType = PatchConstants.EftTypes.Single(x => x.GetProperty("LastSpreadCount") != null && x.GetProperty("LastAimTime") != null);
            _aimingDataGetHit = AccessTools.Method(_aimingDataType, "GetHit");
            return _aimingDataGetHit;
        }

        [PatchPrefix]
        public static void PatchPrefix(ref BotOwner ___botOwner_0, DamageInfo damageInfo)
        {
            if (SAINPlugin.GetSAIN(___botOwner_0, out var sain, nameof(GetHitPatch)))
            {
            }
        }
    }

    internal class ForceNoHeadAimPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(EnemyInfo).GetMethod("method_7");
        [PatchPrefix]
        public static void PatchPrefix(ref bool withLegs, ref bool canBehead)
        {
            canBehead = false;
        }
    }

    internal class NoTeleportPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), "GoToPoint", new[] { typeof(Vector3), typeof(bool), typeof(float), typeof(bool), typeof(bool), typeof(bool) });
        }

        [PatchPrefix]
        public static void PatchPrefix(ref Vector3 pos, ref bool slowAtTheEnd, ref float reachDist, ref bool getUpWithCheck, ref bool mustHaveWay, ref bool onlyShortTrie)
        {
            mustHaveWay = false;
        }
    }

    internal class ShallKnowEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "ShallKnowEnemy");
        }

        const float TimeToForgetEnemyNotSeen = 30f;
        const float TimeToForgetEnemySeen = 60f;

        [PatchPostfix]
        public static void PatchPostfix(EnemyInfo __instance, ref bool __result)
        {
            BotOwner botOwner = __instance?.Owner;

            if (BotsGroupSenseRecently(__instance))
            {
                __result = true;
                return;
            }

            if (__instance.Person != null
                && SAINPlugin.GetSAIN(botOwner, out SAINComponentClass sain, nameof(ShallKnowEnemyPatch)))
            {
                if (SquadSensedRecently(sain, __instance))
                {
                    __result = true;
                }
                else if (EnemySenseRecently(sain, __instance))
                {
                    __result = true;
                }
                else
                {
                    __result = false;
                }
            }
        }
        public static bool BotsGroupSenseRecently(EnemyInfo enemyInfo)
        {
            BotsGroup group = enemyInfo.GroupOwner;
            for (int i = 0; i < group.MembersCount; i++)
            {
                if (SAINPlugin.GetSAIN(group.Member(i), out SAINComponentClass sain, nameof(ShallKnowEnemyPatch)) 
                    && EnemySenseRecently(sain, enemyInfo))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool SquadSensedRecently(SAINComponentClass sain, EnemyInfo enemyInfo)
        {
            // Check each of a sain bots members to see if they heard them recently, if so, we should know this enemy
            bool senseRecently = false;
            var members = sain.Squad?.Members;
            if (members != null && members.Count > 0)
            {
                foreach (var member in members)
                {
                    if (member.Value != null && member.Value.Player?.HealthController.IsAlive == true && EnemySenseRecently(member.Value, enemyInfo))
                    {
                        senseRecently = true;
                        break;
                    }
                }
            }
            return senseRecently;
        }

        public static bool EnemySenseRecently(SAINComponentClass sain, EnemyInfo enemyInfo)
        {
            SAINEnemy myEnemy = sain.EnemyController.CheckAddEnemy(enemyInfo.Person);
            if (myEnemy != null)
            {
                if (!myEnemy.Seen 
                    && myEnemy.Heard 
                    && myEnemy.TimeSinceHeard <= TimeToForgetEnemyNotSeen)
                {
                    return true;
                }
                if (myEnemy.Seen 
                    && myEnemy.Heard 
                    && myEnemy.TimeSinceHeard <= TimeToForgetEnemySeen 
                    && myEnemy.TimeSinceSeen <= sain.Info.ForgetEnemyTime)
                {
                    return true;
                }
                if (myEnemy.KnownPlaces.SearchedAllKnownLocations)
                {
                    //return false;
                }
                if (myEnemy.Seen
                    && myEnemy.TimeSinceSeen <= sain.Info.ForgetEnemyTime)
                {
                    return true;
                }
            }
            else
            {
                Logger.LogWarning($"{enemyInfo?.Person?.Profile.Nickname} is not in Enemy List.");
            }
            return false;
        }
    }

    public enum KnowEnemyReason
    {
        None = 0,
        HeardRecent = 1,
        SeenRecent = 2,
        SquadHeardRecent = 3,
        SquadSeenRecent = 4,
    }

    internal class ShallKnowEnemyLatePatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "ShallKnowEnemyLate");
        }

        const float TimeToForgetEnemyNotSeen = 30f;
        const float TimeToForgetEnemySeen = 60f;

        [PatchPostfix]
        public static void PatchPostfix(EnemyInfo __instance, ref bool __result)
        {
            BotOwner botOwner = __instance?.Owner;

            if (ShallKnowEnemyPatch.BotsGroupSenseRecently(__instance))
            {
                __result = true;
                return;
            }

            if (__instance.Person != null
                && SAINPlugin.GetSAIN(botOwner, out SAINComponentClass sain, nameof(ShallKnowEnemyPatch)))
            {
                if (ShallKnowEnemyPatch.SquadSensedRecently(sain, __instance))
                {
                    __result = true;
                }
                else if (ShallKnowEnemyPatch.EnemySenseRecently(sain, __instance))
                {
                    __result = true;
                }
                else
                {
                    __result = false;
                }
            }
        }
    }

    internal class SkipLookForCoverPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotCoversData).GetMethod("GetClosestPoint");
        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0)
        {
            if (___botOwner_0 != null && SAINPlugin.BotController.GetSAIN(___botOwner_0, out _))
            {
                return false;
            }
            return true;
        }
    }

    internal class BotGroupAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotsGroup).GetMethod("AddEnemy");
        [PatchPrefix]
        public static bool PatchPrefix(IPlayer person)
        {
            if (person == null || (person.IsAI && person.AIData?.BotOwner?.GetPlayer == null))
            {
                return false;
            }

            return true;
        }
    }

    internal class BotMemoryAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotMemoryClass).GetMethod("AddEnemy");
        [PatchPrefix]
        public static bool PatchPrefix(IPlayer enemy)
        {
            if (enemy == null || (enemy.IsAI && enemy.AIData?.BotOwner?.GetPlayer == null))
            {
                return false;
            }

            return true;
        }
    }

    public class GrenadeThrownActionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "method_4");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotsController __instance, Grenade grenade, Vector3 position, Vector3 force, float mass)
        {
            Vector3 danger = Vector.DangerPoint(position, force, mass);
            foreach (BotOwner bot in __instance.Bots.BotOwners)
            {
                if (SAINPlugin.BotController.Bots.ContainsKey(bot.ProfileId))
                {
                    continue;
                }
                bot.BewareGrenade.AddGrenadeDanger(danger, grenade);
            }
            return false;
        }
    }

    public class GrenadeExplosionActionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "method_3");
        }

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            return false;
        }
    }

    public class GetBotController : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        public static void PatchPrefix(BotsController __instance)
        {
            SAINPlugin.BotController.DefaultController = __instance;
        }
    }

    public class GetBotSpawner : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), "AddPlayer");
        }

        [PatchPostfix]
        public static void PatchPostfix(BotSpawner __instance)
        {
            var controller = SAINPlugin.BotController;
            if (controller != null && controller.BotSpawner == null)
            {
                controller.BotSpawner = __instance;
            }
        }
    }
}
