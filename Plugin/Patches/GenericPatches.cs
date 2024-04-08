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

namespace SAIN.Patches.Generic
{
    internal class HeadShotProtectionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(Player).GetMethod("ApplyShot");
        [PatchPrefix]
        public static void PatchPrefix(ref EBodyPart bodyPartType, ref DamageInfo damageInfo, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, GStruct390 shotId, ref Player __instance)
        {
            var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
            if (settings.HeadShotDamageRedirection == false)
            {
                return;
            }

            if (bodyPartType == EBodyPart.Head && __instance.IsYourPlayer)
            {
                float oldDmg = damageInfo.Damage;
                float ratio = 1f - settings.HeadShotDamageRedirectionPercent / 100f;
                float newDamage = oldDmg * ratio;
                float amountReduced = oldDmg - newDamage;

                EBodyPart newPart = BodyParts.PickRandom();
                Logger.LogInfo($"Headshot Damage To Player! Original Damage: {oldDmg} New Damage: {newDamage} :: {amountReduced} damage redirected to {newPart}");

                damageInfo.Damage = amountReduced;
                __instance.ApplyShot(damageInfo, newPart, colliderType, armorPlateCollider, shotId);

                damageInfo.Damage = newDamage;
            }
        }

        private static List<EBodyPart> BodyParts = new List<EBodyPart> 
        { 
            EBodyPart.LeftLeg,
            EBodyPart.RightLeg,
            EBodyPart.LeftArm,
            EBodyPart.RightArm,
            EBodyPart.Chest, 
            EBodyPart.Stomach
        };
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
