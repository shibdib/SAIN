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
    public class KickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.Interact));
        }

        public static bool Enabled = true;

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____owner, Door door, ref EInteractionType Etype, ref float ____traversingEnd)
        {
            if (____owner == null || Enabled == false)
            {
                return true;
            }

            EnemyInfo enemy = ____owner.Memory.GoalEnemy;
            if (enemy == null || enemy.Person?.Transform == null)
            {
                if (Etype == EInteractionType.Breach)
                {
                    Etype = EInteractionType.Open;
                }
            }
            else if (Etype == EInteractionType.Open || Etype == EInteractionType.Breach)
            {
                bool enemyClose = Vector3.Distance(____owner.Position, enemy.CurrPosition) < 30f;

                if (enemyClose)
                {
                    var breakInParameters = door.GetBreakInParameters(____owner.Position);

                    if (door.BreachSuccessRoll(breakInParameters.InteractionPosition))
                    {
                        Etype = EInteractionType.Breach;
                    }
                    else
                    {
                        Etype = EInteractionType.Open;
                    }
                }
                else
                {
                    Etype = EInteractionType.Open;
                }
            }

            EDoorState state = EDoorState.None;
            switch (Etype)
            {
                case EInteractionType.Open:
                    state = EDoorState.Open;
                    break;
                case EInteractionType.Close:
                    state = EDoorState.Shut;
                    break;
                default:
                    break;
            }

            ____owner.DoorOpener.Interacting = true;
            ____traversingEnd = Time.time + 0.25f;

            if (state != EDoorState.None)
            {
                door.method_3(state, false);
            }
            else
            {
                InteractionResult interactionResult = new InteractionResult(Etype);
                ____owner.GetPlayer.CurrentManagedState.StartDoorInteraction(door, interactionResult, null);
            }

            return false;
        }
    }

    public class KickPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.method_2));
        }

        public static bool Enabled = true;

        [PatchPrefix]
        public static bool PatchPrefix(
            ref BotOwner ____owner, 
            ref float ____traversingEnd, 
            ref float ____comeToDoorLast, 
            ref Door ____currentDoor,
            ref float ____nextPosibleDoorOpenTime,
            ref bool ____shallStartInteract)
        {
            float time = Time.time;
            float num = time - ____comeToDoorLast;
            ____owner.Mover.SprintPause(1f);

            //____owner.MovementPause(this.Single_0__traversingEndFreq);
            if (num > 3f)
            {
                ____comeToDoorLast = time;
                return false;
            }
            if (!____currentDoor.Operatable || ____owner.DoorOpener.CanOpenDoorNow)
            {
                return false;
            }
            if (____currentDoor.DoorState == EDoorState.Interacting)
            {
                ____nextPosibleDoorOpenTime = Time.time + 0.5f;
                return false;
            }
            ____shallStartInteract = false;
            if (____currentDoor.DoorState != EDoorState.Shut)
            {
                if (____currentDoor.DoorState == EDoorState.Open)
                {
                    //____nextPosibleDoorOpenTime = Time.time + 0.75f;
                    //____owner.DoorOpener.Interact(____currentDoor, EInteractionType.Close);
                }
                return false;
            }
            ____nextPosibleDoorOpenTime = Time.time + 2f;
            ____owner.DoorOpener.Interact(____currentDoor, EInteractionType.Open);
            return false;
        }
    }

    public class KickPatch3 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.method_2));
        }

        public static bool Enabled = true;

        [PatchPrefix]
        public static bool PatchPrefix(
            ref BotOwner ____owner,
            ref float ____traversingEnd,
            ref float ____comeToDoorLast,
            ref Door ____currentDoor,
            ref float ____nextPosibleDoorOpenTime,
            ref bool ____shallStartInteract)
        {
            float time = Time.time;
            float num = time - ____comeToDoorLast;
            ____owner.Mover.SprintPause(1f);

            //____owner.MovementPause(this.Single_0__traversingEndFreq);
            if (num > 3f)
            {
                ____comeToDoorLast = time;
                return false;
            }
            if (!____currentDoor.Operatable || ____owner.DoorOpener.CanOpenDoorNow)
            {
                return false;
            }
            if (____currentDoor.DoorState == EDoorState.Interacting)
            {
                ____nextPosibleDoorOpenTime = Time.time + 0.5f;
                return false;
            }
            ____shallStartInteract = false;
            if (____currentDoor.DoorState != EDoorState.Shut)
            {
                if (____currentDoor.DoorState == EDoorState.Open)
                {
                    //____nextPosibleDoorOpenTime = Time.time + 0.75f;
                    //____owner.DoorOpener.Interact(____currentDoor, EInteractionType.Close);
                }
                return false;
            }
            ____nextPosibleDoorOpenTime = Time.time + 2f;
            ____owner.DoorOpener.Interact(____currentDoor, EInteractionType.Open);
            return false;
        }
    }
}
