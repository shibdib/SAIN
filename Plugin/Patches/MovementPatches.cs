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

namespace SAIN.Patches.Generic
{
    public class DoorOpenerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.Update));
        }

        public static bool Enabled => !ModDetection.ProjectFikaLoaded;

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____owner)
        {
            if (____owner == null || ModDetection.ProjectFikaLoaded)
            {
                return true;
            }

            if (SAINPlugin.GetSAIN(____owner, out var sain, nameof(DoorOpenerPatch)))
            {
                sain.DoorOpener.Update();
                return false;
            }
            return true;
        }
    }
}
