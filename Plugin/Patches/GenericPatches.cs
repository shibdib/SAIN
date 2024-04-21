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

namespace SAIN.Patches.Generic
{
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
            ___botOwner_0.Steering.LookToDirection(dir, 300);
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
            if (__instance.IsAI)
            {
                BotOwner botOwner = __instance?.AIData?.BotOwner;
                if (botOwner != null && SAINPlugin.BotController.GetBot(botOwner.ProfileId, out var sain))
                {
                    sain?.Info?.WeaponInfo?.Recoil?.WeaponShot();
                }
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
            BotOwner botOwner = ___botOwner_0;
            if (botOwner != null && SAINPlugin.BotController.GetBot(botOwner.ProfileId, out var sain))
            {
                sain.BotStun.GetHit(damageInfo);
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
            //mustHaveWay = false;
        }
    }

    internal class ShallKnowEnemyPatch : ModulePatch
    {
        const float TimeToForgetEnemyNoSight = 30f;

        private static PropertyInfo OwnerProp;

        protected override MethodBase GetTargetMethod()
        {
            OwnerProp = AccessTools.Property(typeof(EnemyInfo), "Owner");
            return AccessTools.Method(typeof(EnemyInfo), "ShallKnowEnemy");
        }

        [PatchPostfix]
        public static void PatchPostfix(EnemyInfo __instance, ref bool __result)
        {
            if (__result == false)
            {
                return;
            }

            BotOwner botOwner = OwnerProp.GetValue(__instance) as BotOwner;

            if (botOwner != null && __instance.Person != null && SAINPlugin.BotController.GetBot(botOwner.ProfileId, out var component))
            {
                SAINEnemy enemy = component.EnemyController.GetEnemy(__instance.Person.ProfileId);
                if (enemy != null && !enemy.Seen && enemy.Heard)
                {
                    if (enemy.TimeSinceHeard > TimeToForgetEnemyNoSight)
                    {
                        __result = false;
                    }
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
            if (___botOwner_0 != null && SAINPlugin.BotController.GetBot(___botOwner_0.ProfileId, out var component))
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
