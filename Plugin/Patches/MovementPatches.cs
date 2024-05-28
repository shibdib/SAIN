using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.Components;
using SAIN.Helpers;
using System.Reflection;
using UnityEngine;
using DrakiaXYZ.BigBrain.Brains;
using UnityEngine.AI;
using SAIN.Layers;
using System;
using UnityEngine.UIElements;
using EFT.HealthSystem;

namespace SAIN.Patches.Generic
{
    public class SteeringPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSteering), "method_1");
        }

        public static bool Enabled => !ModDetection.ProjectFikaLoaded;

        [PatchPrefix]
        public static void PatchPrefix(BotOwner ___botOwner_0, ref Vector3 ____lookDirection)
        {
            //Vector3 targetLookDir = ____lookDirection;
            //Vector3 originPoint = ___botOwner_0.WeaponRoot.position;
            //Vector3 groundPos = ___botOwner_0.Position;
            //Vector3 groundDir = groundPos - originPoint;
            //
            //float dot = Vector3.Dot(targetLookDir.normalized, groundDir.normalized);
            //
            //if (dot > 0.5f)
            //{
            //    ____lookDirection.y = Mathf.Clamp(____lookDirection.y, -0.66f, 0.66f);
            //}
            ____lookDirection = Vector3.Normalize(____lookDirection);
            ____lookDirection.y = Mathf.Clamp(____lookDirection.y, -0.66f, 0.66f);
        }
    }

    public class DoorOpenerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.Update));
        }

        public static bool Enabled => !ModDetection.ProjectFikaLoaded;

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____owner, ref bool __result)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.General.NewDoorOpening)
            {
                return true;
            }
            if (____owner == null)
            {
                return true;
            }

            if (SAINPlugin.IsBotExluded(____owner))
            {
                return true;
            }

            if (SAINEnableClass.GetSAIN(____owner, out var sain, nameof(DoorOpenerPatch)))
            {
                __result = sain.DoorOpener.Update();
                return false;
            }
            return true;
        }
    }
}
