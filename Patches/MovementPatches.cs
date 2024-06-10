using Aki.Reflection.Patching;
using EFT;
using EFT.EnvironmentEffect;
using HarmonyLib;
using System.Reflection;

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