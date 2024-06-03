using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using SAIN.Preset;
using SAIN.Components;
using SAIN.Plugin;
using SAIN.Preset.BotSettings.SAINSettings;
using System;
using System.Reflection;
using UnityEngine;
using Comfort.Common;
using SAIN.SAINComponent.Classes;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine.UIElements;
using EFT.InventoryLogic;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent;
using SAIN.Preset.GlobalSettings;
using System.Text;

namespace SAIN.Patches.Vision
{
    public class Math
    {
        public static float CalcVisSpeed(float dist, SAINSettingsClass preset)
        {
            float result = 1f;
            if (dist >= preset.Look.CloseFarThresh)
            {
                result *= preset.Look.FarVisionSpeed;
            }
            else
            {
                result *= preset.Look.CloseVisionSpeed;
            }
            result *= preset.Look.VisionSpeedModifier;

            return Mathf.Round(result * 100f) / 100f;
        }
    }

    public class SetPartPriorityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "UpdatePartsByPriority");
        }

        [PatchPrefix]
        public static bool PatchPrefix(EnemyInfo __instance)
        {
            bool isAI = __instance?.Person?.IsAI == true;
            bool visible = __instance.IsVisible;

            if (isAI)
            {
                if (!__instance.HaveSeenPersonal || Time.time - __instance.TimeLastSeenReal > 5f)
                {
                    __instance.SetFarParts();
                }
                else
                {
                    __instance.SetMiddleParts();
                }
                return false;
            }

            if (!isAI && 
                SAINEnableClass.GetSAIN(__instance.Owner, out BotComponent botComponent, nameof(SetPartPriorityPatch)))
            {
                SAINEnemy enemy = botComponent.EnemyController.CheckAddEnemy(__instance.Person);
                if (enemy != null)
                {
                    if (enemy.IsCurrentEnemy)
                    {
                        __instance.SetCloseParts();
                        return false;
                    }
                    if ((enemy.EnemyStatus.ShotAtMeRecently ||
                        enemy.EnemyStatus.PositionalFlareEnabled))
                    {
                        __instance.SetCloseParts();
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public class GlobalLookSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGlobalLookData), "Update");
        }

        [PatchPostfix]
        public static void Patch(BotGlobalLookData __instance)
        {
            __instance.CHECK_HEAD_ANY_DIST = false;
            __instance.MIDDLE_DIST_CAN_SHOOT_HEAD = true;
            __instance.SHOOT_FROM_EYES = false;
        }
    }

    public class WeatherTimeVisibleDistancePatch : ModulePatch
    {
        private static PropertyInfo _clearVisibleDistProperty;
        private static PropertyInfo _visibleDistProperty;
        private static PropertyInfo _HourServerProperty;

        protected override MethodBase GetTargetMethod()
        {
            _clearVisibleDistProperty = typeof(LookSensor).GetProperty("ClearVisibleDist");
            _visibleDistProperty = typeof(LookSensor).GetProperty("VisibleDist");
            _HourServerProperty = typeof(LookSensor).GetProperty("HourServer");

            return AccessTools.Method(typeof(LookSensor), "method_2");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____botOwner, ref float ____nextUpdateVisibleDist)
        {
            if (____nextUpdateVisibleDist < Time.time)
            {
                float timeMod = 1f;
                float weatherMod = 1f;

                // Checks to make sure a date and time is present
                if (____botOwner.GameDateTime != null)
                {
                    DateTime dateTime = SAINPlugin.BotController.TimeVision.GameDateTime;
                    timeMod = SAINPlugin.BotController.TimeVision.TimeOfDayVisibility;
                    // Modify the Rounding of the "HourServer" property to the hour from the DateTime object
                    _HourServerProperty.SetValue(____botOwner.LookSensor, (int)((short)dateTime.Hour));
                }
                if (SAINPlugin.BotController != null)
                {
                    weatherMod = SAINPlugin.BotController.WeatherVision.VisibilityNum;
                    weatherMod = Mathf.Clamp(weatherMod, 0.5f, 1f);
                }

                float currentVisionDistance = ____botOwner.Settings.Current.CurrentVisibleDistance;

                // Sets a minimum cap based on weather conditions to avoid bots having too low of a vision Distance while at peace in bad weather
                float currentVisionDistanceCapped = Mathf.Clamp(currentVisionDistance * weatherMod, 80f, currentVisionDistance);

                // Applies SeenTime Modifier to the final vision Distance results
                float finalVisionDistance = currentVisionDistanceCapped * timeMod;

                _clearVisibleDistProperty.SetValue(____botOwner.LookSensor, finalVisionDistance);

                finalVisionDistance = ____botOwner.NightVision.UpdateVision(finalVisionDistance);
                finalVisionDistance = ____botOwner.BotLight.UpdateLightEnable(finalVisionDistance);
                _visibleDistProperty.SetValue(____botOwner.LookSensor, finalVisionDistance);

                ____nextUpdateVisibleDist = Time.time + (____botOwner.FlashGrenade.IsFlashed ? 3 : 20);
            }
            // Not sure what this does, but its new, so adding it here since this patch replaces the old.
            ____botOwner.BotLight.UpdateStrope();
            return false;
        }
    }

    public class NoAIESPPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner)?.GetMethod("IsEnemyLookingAtMe", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(IPlayer) }, null);
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class BotLightTurnOnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotLight), "TurnOn");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0, ref bool ____isInDarkPlace)
        {
            if (____isInDarkPlace
                && !SAINPlugin.LoadedPreset.GlobalSettings.Flashlight.AllowLightOnForDarkBuildings)
            {
                ____isInDarkPlace = false;
            }
            if (____isInDarkPlace || ___botOwner_0.Memory.GoalEnemy != null)
            {
                return true;
            }
            if (!shallTurnLightOff(___botOwner_0.Profile.Info.Settings.Role))
            {
                return true;
            }
            ___botOwner_0.BotLight.TurnOff(false, true);
            return false;
        }

        private static bool shallTurnLightOff(WildSpawnType wildSpawnType)
        {
            FlashlightSettings settings = SAINPlugin.LoadedPreset.GlobalSettings.Flashlight;
            if (EnumValues.WildSpawn.IsScav(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemySCAV;
            }
            if (EnumValues.WildSpawn.IsPMC(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyPMC;
            }
            if (EnumValues.WildSpawn.IsGoons(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyGOONS;
            }
            if (EnumValues.WildSpawn.IsBoss(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyBOSS;
            }
            if (EnumValues.WildSpawn.IsFollower(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyFOLLOWER;
            }
            return settings.TurnLightOffNoEnemyRAIDERROGUE;
        }
    }

    public class VisionSpeedPostPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "method_5");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref float __result, EnemyInfo __instance)
        {
            if (SAINEnableClass.GetSAIN(__instance?.Owner, out var sain, nameof(VisionSpeedPostPatch)))
            {
                SAINEnemy enemy = sain.EnemyController.GetEnemy(__instance.Person.ProfileId);
                if (enemy != null)
                {
                    __result *= enemy.Vision.GainSightCoef;
                }
            }
            //__result = Mathf.Clamp(__result, 0.1f, 8888f);
        }
    }

    public class VisionDistancePosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "CheckVisibility");
        }

        [PatchPrefix]
        public static void PatchPrefix(ref float addVisibility, EnemyInfo __instance)
        {
            if (SAINEnableClass.GetSAIN(__instance?.Owner, out var sain, nameof(VisionSpeedPostPatch)))
            {
                SAINEnemy enemy = sain.EnemyController.GetEnemy(__instance.Person.ProfileId);
                if (enemy != null)
                {
                    addVisibility += enemy.Vision.VisionDistance;
                }
            }
        }

    }

    public class CheckFlashlightPatch : ModulePatch
    {
        private static FieldInfo _tacticalModesField;
        private static MethodInfo _UsingLight;

        protected override MethodBase GetTargetMethod()
        {
            _UsingLight = AccessTools.PropertySetter(typeof(AIData), "UsingLight");
            _tacticalModesField = AccessTools.Field(typeof(TacticalComboVisualController), "list_0");
            return AccessTools.Method(typeof(Player.FirearmController), "SetLightsState");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player ____player)
        {
            if (____player.gameObject.TryGetComponent<FlashLightComponent>(out var component))
            {
                component.CheckDevice(____player, _tacticalModesField);
                if (!component.WhiteLight && !component.Laser)
                {
                    _UsingLight.Invoke(____player.AIData, new object[] { false });
                }
            }
        }
    }
}