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
        }
    }

    public class AIVisionUpdateLimitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LookSensor), "CheckAllEnemies");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____botOwner, GClass522 lookAll)
        {
            if (____botOwner != null)
            {
                if (!LookUpdates.ContainsKey(____botOwner.ProfileId))
                {
                    LookUpdates.Add(____botOwner.ProfileId, new AIVisionInfo(____botOwner));
                }
                LookUpdates[____botOwner.ProfileId].CheckEnemies(lookAll);
            }
            return false;
        }

        public static Dictionary<string, AIVisionInfo> LookUpdates = new Dictionary<string, AIVisionInfo>();

        public class AIVisionInfo
        {
            public AIVisionInfo(BotOwner bot)
            {
                BotOwner = bot;
            }

            public void CheckEnemies(GClass522 lookAll)
            {
                float time = Time.time;
                var enemyInfos = this.BotOwner.EnemiesController.EnemyInfos.Values;
                this._cacheToLookEnemyInfos.AddRange(enemyInfos);
                foreach (EnemyInfo enemyInfo in this._cacheToLookEnemyInfos)
                {
                    try
                    {
                        IPlayer person = enemyInfo.Person;
                        if (!NextUpdateTimes.ContainsKey(person.ProfileId))
                        {
                            NextUpdateTimes.Add(person.ProfileId, 0f);
                        }

                        float timeAdd;
                        if (!person.IsAI)
                        {
                            timeAdd = 0.1f;
                        }
                        else
                        {
                            if (BotOwner.Memory.GoalEnemy == enemyInfo)
                            {
                                timeAdd = 0.2f;
                            }
                            else
                            {
                                timeAdd = 0.33f;
                            }
                        }

                        if (NextUpdateTimes[person.ProfileId] + timeAdd < time)
                        {
                            NextUpdateTimes[person.ProfileId] = time;
                            enemyInfo.CheckLookEnemy(lookAll);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                        BotOwner.EnemiesController.EnemyInfos.Remove(enemyInfo.Person);
                    }
                }
                this._cacheToLookEnemyInfos.Clear();
            }

            private readonly BotOwner BotOwner;
            public Dictionary<string, float> NextUpdateTimes = new Dictionary<string, float>();
            private readonly List<EnemyInfo> _cacheToLookEnemyInfos = new List<EnemyInfo>();
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

                    if (!__instance.Person.IsAI)
                    {
                        float partRatio = SAINVisionClass.GetRatioPartsVisible(__instance, out int visibleCount);
                        float ratio = calcSpeedFromRatio(partRatio);
                        __result /= ratio;
                        if (visibleCount > 0)
                        {
                            //Logger.LogInfo($"vision speed ratio from parts visible {ratio} : part ratio: {partRatio} Visible Part Count: {visibleCount} AllParts Count: {__instance.AllActiveParts.Count + 1}");
                        }
                    }

                    __result = Mathf.Clamp(__result, 0.1f, 8888f);
                    return;
                }
            }

            __result *= SAINEnemyVision.GetGainSightModifier(__instance, null);
            __result = Mathf.Clamp(__result, 0.1f, 8888f);
        }

        private static float calcSpeedFromRatio(float partRatio)
        {
            float min = 0.6f;
            float max = 1.1f;
            return Mathf.Lerp(min, max, partRatio);
        }
    }

    public class VisionDistancePosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "CheckVisibility");
        }

        private static float calcSpeedFromRatio(float partRatio)
        {
            float min = 0.5f;
            float max = 1f;
            return Mathf.Lerp(min, max, partRatio);
        }

        private static float calcSprintMod(Player player)
        {
            if (player.MovementContext.IsSprintEnabled)
            {
                LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
                float velocityFactor = Mathf.InverseLerp(0, 5f, player.Velocity.magnitude);
                return Mathf.Lerp(1, globalLookSettings.SprintingVisionModifier, velocityFactor);
            }
            return 1f;
        }

        private static float calcAngleMod(SAINEnemy enemy)
        {
            float minAngle = 45f;
            float angleToEnemy = enemy.Vision.AngleToEnemy;
            float maxAngle = enemy.Vision.MaxVisionAngle;
            if (angleToEnemy > minAngle &&
                angleToEnemy < maxAngle)
            {
                float num = maxAngle - minAngle;
                float num2 = angleToEnemy - minAngle;
                float ratio = 1f - num2 / num;
                float min = 0.25f;
                float max = 1f;
                return Mathf.InverseLerp(min, max, ratio);
            }
            return 1f;
        }

        private static float calcGearStealthMod(Player player, float distance)
        {
            var gearInfo = SAINGearInfoHandler.GetGearInfo(player);
            if (gearInfo != null)
            {
                return gearInfo.GetStealthModifier(distance);
            }
            return 1f;
        }

        [PatchPrefix]
        public static void PatchPrefix(ref float addVisibility, EnemyInfo __instance)
        {
            //var stringBuilder = new StringBuilder();
            Player player = EFTInfo.GetPlayer(__instance?.Person?.ProfileId);
            if (player != null)
            {
                float visibility = 1f;
                // Increase or decrease vis distance based on pose and if sprinting.
                float sprintMod = calcSprintMod(player);

                if (sprintMod != 1f)
                {
                    visibility /= sprintMod;
                    //stringBuilder.AppendLine($"sprintMod {sprintMod}");
                }

                float stealthMod = calcGearStealthMod(player, __instance.Distance);
                if (stealthMod != 1f)
                {
                    visibility /= stealthMod;
                    //stringBuilder.AppendLine($"stealthMod div {stealthMod}");
                }

                if (SAINEnableClass.GetSAIN(__instance.Owner, out var sain, nameof(VisionDistancePosePatch)))
                {
                    SAINEnemy sainEnemy = sain.EnemyController.GetEnemy(player.ProfileId);
                    if (sainEnemy != null)
                    {
                        if (sainEnemy.EnemyStatus.PositionalFlareEnabled)
                        {
                            visibility *= 1.25f;
                            //stringBuilder.AppendLine($"PosFlare {1.25f}");
                        }

                        float anglemod = calcAngleMod(sainEnemy);
                        if (anglemod != 1f)
                        {
                            visibility *= anglemod;
                            //stringBuilder.AppendLine($"anglemod {anglemod}");
                        }

                        if (sainEnemy.EnemyStatus.ShotAtMeRecently)
                        {
                            visibility *= 1.25f;
                        }

                        //if (sainEnemy.HeardRecently)
                        //{
                        //    if (player.AIData.GetFlare)
                        //    {
                        //        // if player shot a weapon recently
                        //        // if player is using suppressed weapon, and has shot recently, don't increase vis distance as much.
                        //        bool suppressedFlare = false;
                        //        if (player.HandsController.Item is Weapon weapon)
                        //        {
                        //            var weaponInfo = SAINGearInfoHandler.GetGearInfo(player);
                        //            suppressedFlare = weaponInfo?.GetWeaponInfo(weapon)?.HasSuppressor == true;
                        //        }
                        //
                        //        // increase visiblity
                        //        visibility *= suppressedFlare ? 1.05f : 1.15f;
                        //        if (suppressedFlare)
                        //        {
                        //            stringBuilder.AppendLine($"suppressedFlare {1.05f}");
                        //        }
                        //        else
                        //        {
                        //
                        //            stringBuilder.AppendLine($"Flare {1.15f}");
                        //        }
                        //    }
                        //}

                    }
                }

                // Reduce vision distance for ai vs ai vision checks
                if (player.IsAI
                    && SAINPlugin.LoadedPreset.GlobalSettings.General.LimitAIvsAI)
                {
                    visibility *= 0.8f;
                }

                float defaultVisDist = __instance.Owner.LookSensor.VisibleDist;
                float visionDist = (defaultVisDist * visibility) - defaultVisDist;

                if (!__instance.Person.IsAI && _nextLogTime < Time.time)
                {
                    _nextLogTime = Time.time + 0.5f;
                    //stringBuilder.AppendLine($"Adding [{visionDist}] (mod: {visibility}) meters to base vision distance [{__instance.Owner.LookSensor.VisibleDist}] for [{__instance.Owner.name}] for reasons: ");
                    //Logger.LogDebug(stringBuilder.ToString());
                }

                addVisibility += visionDist;
            }
        }
        private static float _nextLogTime;
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
            if (____player.gameObject.TryGetComponent<SAINFlashLightComponent>(out var component))
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