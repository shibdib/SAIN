using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT;
using EFT.EnvironmentEffect;
using HarmonyLib;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Enemy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Systems.Effects;
using UnityEngine;

namespace SAIN.Patches.Generic
{
    public static class GenericHelpers
    {
        public static bool CheckNotNull(BotOwner botOwner)
        {
            return botOwner != null &&
                botOwner.gameObject != null &&
                botOwner.gameObject.transform != null &&
                botOwner.Transform != null &&
                !botOwner.IsDead;
        }
    }

    public class SetEnvironmentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AIData), "SetEnvironment");
        }

        [PatchPostfix]
        public static void Patch(AIData __instance, IndoorTrigger trigger)
        {
            SAINBotController.Instance?.PlayerEnviromentChanged(__instance?.Player?.ProfileId, trigger);
        }
    }

    public class FixItemTakerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotItemTaker), "method_11");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return GenericHelpers.CheckNotNull(___botOwner_0);
        }
    }

    public class FixItemTakerPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotItemTaker), "RefreshClosestItems");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return GenericHelpers.CheckNotNull(___botOwner_0);
        }
    }

    public class FixPatrolDataPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass436), "method_4");
        }

        [PatchPrefix]
        public static bool PatchPrefix(List<BotOwner> followers, ref bool __result)
        {
            using (List<BotOwner>.Enumerator enumerator = followers.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (!GenericHelpers.CheckNotNull(enumerator.Current) || enumerator.Current.BotFollower?.PatrolDataFollower?.HaveProblems == true)
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            __result = true;
            return false;
        }
    }

    public class DisableLookSensorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LookSensor), "CheckAllEnemies");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ____botOwner)
        {
            return SAINEnableClass.isBotExcluded(____botOwner);
        }
    }

    public class AddPointToSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsGroup), "AddPointToSearch");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotsGroup __instance, BotOwner owner)
        {
            return SAINPlugin.IsBotExluded(owner);
        }
    }

    public class SetPanicPointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), "SetPanicPoint");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return SAINPlugin.IsBotExluded(___botOwner_0);
        }
    }

    public class HaveSeenEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(EnemyInfo), "HaveSeen");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref bool __result, EnemyInfo __instance)
        {
            if (__result == true)
            {
                return;
            }
            if (SAINEnableClass.GetSAIN(__instance.Owner, out var sain, nameof(HaveSeenEnemyPatch))
                //&& sain.Info.Profile.IsPMC
                && sain.EnemyController.CheckAddEnemy(__instance.Person)?.Heard == true)
            {
                __result = true;
            }
        }
    }

    public class BulletImpactPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BetterAudio), "LimitedPlay");
        }

        [PatchPostfix]
        public static void PatchPostfix(BetterAudio __instance, Vector3 position, SoundBank bank, float distance, Vector3 gagRadius, float chokeTime, float volume, float bankBlendValue, EnvironmentType env, EOcclusionTest occlusionTest, string key)
        {
            if (SAINPlugin.BotController != null)
            {
            }
        }
    }
    public class BulletImpactPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EffectsCommutator), "PlayHitEffect");
        }

        [PatchPostfix]
        public static void PatchPostfix(EftBulletClass info)
        {
            if (SAINPlugin.BotController != null)
            {
                //Vector3 position = __instance.transform.position + ___vector3_0;
                SAINPlugin.BotController.BulletImpacted(info);
            }
        }
    }

    public class StopSetToNavMeshPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0)
        {
            return SAINPlugin.IsBotExluded(___botOwner_0);
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
                SAINPlugin.BotController?.StartCoroutine(TakePrevWeapon(___botOwner_0));
            }
            return false;
        }

        private static IEnumerator TakePrevWeapon(BotOwner bot)
        {
            yield return new WaitForSeconds(0.25f);
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
            if (SAINEnableClass.GetSAIN(__instance, out var sain, nameof(OnMakingShotRecoilPatch)))
            {
                sain.Info.WeaponInfo.Recoil.WeaponShot();
            }
        }
    }

    internal class ForceNoHeadAimPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "method_7");
        }

        [PatchPrefix]
        public static void PatchPrefix(ref bool withLegs, ref bool canBehead, EnemyInfo __instance)
        {
            if (!__instance.Person.IsAI)
            {
                canBehead = 
                    SAINPlugin.LoadedPreset.GlobalSettings.Aiming.PMCSAimForHead &&
                    EnumValues.WildSpawn.IsPMC(__instance.Owner.Profile.Info.Settings.Role);

                withLegs = true;
            }
            else
            {
                canBehead = true;
                withLegs = true;
            }
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

        private const float TimeToForgetEnemyNotSeen = 30f;
        private const float TimeToForgetEnemySeen = 60f;

        [PatchPostfix]
        public static void PatchPostfix(EnemyInfo __instance, ref bool __result)
        {
            if (SAINPlugin.IsBotExluded(__instance.Owner))
            {
                return;
            }
            if (BotsGroupSenseRecently(__instance))
            {
                __result = true;
            }
        }

        public static bool BotsGroupSenseRecently(EnemyInfo enemyInfo)
        {
            BotsGroup group = enemyInfo.GroupOwner;
            for (int i = 0; i < group.MembersCount; i++)
            {
                if (SAINEnableClass.GetSAIN(group.Member(i), out BotComponent sain, nameof(ShallKnowEnemyPatch))
                    && EnemySenseRecently(sain, enemyInfo))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool EnemySenseRecently(BotComponent sain, EnemyInfo enemyInfo)
        {
            SAINEnemy myEnemy = sain.EnemyController.CheckAddEnemy(enemyInfo.Person);
            if (myEnemy?.IsValid == true)
            {
                var lastKnown = myEnemy?.KnownPlaces?.LastKnownPlace;
                return lastKnown != null
                    && lastKnown.TimeSincePositionUpdated <= sain.BotOwner.Settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC;
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

        [PatchPostfix]
        public static void PatchPostfix(EnemyInfo __instance, ref bool __result)
        {
            if (SAINPlugin.IsBotExluded(__instance.Owner))
            {
                return;
            }
            if (ShallKnowEnemyPatch.BotsGroupSenseRecently(__instance))
            {
                __result = true;
            }
        }
    }

    internal class SkipLookForCoverPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotCoversData).GetMethod("GetClosestPoint");

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return SAINPlugin.IsBotExluded(___botOwner_0);
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
                if (SAINPlugin.IsBotExluded(bot))
                {
                    bot.BewareGrenade.AddGrenadeDanger(danger, grenade);
                }
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
            return AccessTools.Method(typeof(BotsController), "method_0");
        }

        [PatchPrefix]
        public static void PatchPrefix(BotsController __instance)
        {
            var controller = SAINPlugin.BotController;
            if (controller != null && controller.DefaultController == null)
            {
                controller.DefaultController = __instance;
            }
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