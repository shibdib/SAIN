using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.BotSettings.SAINSettings.Categories;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SAIN.Patches.Shoot.Aim
{
    internal class AimOffsetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            _endTargetPointProp = AccessTools.Property(HelpersGClass.AimDataType, "EndTargetPoint");
            return AccessTools.Method(HelpersGClass.AimDataType, "method_13");
        }

        private static PropertyInfo _endTargetPointProp;

        private static float DebugTimer;

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0, ref Vector3 ___vector3_5, ref Vector3 ___vector3_4, ref float ___float_13)
        {
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return true;
            }

            float aimUpgradeByTime = ___float_13;
            Vector3 badShootOffset = ___vector3_5;
            Vector3 aimOffset = ___vector3_4;
            Vector3 recoilOffset = ___botOwner_0.RecoilData.RecoilOffset;
            Vector3 realTargetPoint = ___botOwner_0.AimingData.RealTargetPoint;

            IPlayer person = ___botOwner_0?.Memory?.GoalEnemy?.Person;
            if (SAINEnableClass.GetSAIN(___botOwner_0, out var bot, nameof(AimOffsetPatch)))
            {
                float distance = (realTargetPoint - ___botOwner_0.WeaponRoot.position).magnitude;
                float scaled = distance / 20f;
                recoilOffset = bot.Info.WeaponInfo.Recoil.CurrentRecoilOffset * scaled;
            }

            // Applies aiming offset, recoil offset, and scatter offsets
            // Default Setup :: Vector3 finalTarget = __instance.RealTargetPoint + badShootOffset + (AimUpgradeByTime * (AimOffset + ___botOwner_0.RecoilData.RecoilOffset));
            Vector3 finalOffset = badShootOffset + (aimUpgradeByTime * aimOffset) + recoilOffset;

            if (person != null &&
                !person.IsAI &&
                SAINPlugin.LoadedPreset.GlobalSettings.Look.NotLookingToggle)
            {
                finalOffset += NotLookingOffset(person, ___botOwner_0);
            }

            Vector3 result = realTargetPoint + finalOffset;
            if (SAINPlugin.LoadedPreset.GlobalSettings.Debug.DebugDrawAimGizmos &&
                person?.IsYourPlayer == true)
            {
                Vector3 weaponRoot = ___botOwner_0.WeaponRoot.position;
                DebugGizmos.Line(weaponRoot, result, Color.red, 0.025f, true, 0.25f, true);
                DebugGizmos.Line(weaponRoot, realTargetPoint, Color.white, 0.025f, true, 0.25f, true);
            }

            _endTargetPointProp.SetValue(___botOwner_0.AimingData, result);
            return false;
        }

        private static Vector3 NotLookingOffset(IPlayer person, BotOwner botOwner)
        {
            float ExtraSpread = SAINNotLooking.GetSpreadIncrease(person, botOwner);
            if (ExtraSpread > 0)
            {
                Vector3 vectorSpread = UnityEngine.Random.insideUnitSphere * ExtraSpread;
                vectorSpread.y = 0;
                if (SAINPlugin.DebugMode && DebugTimer < Time.time)
                {
                    DebugTimer = Time.time + 10f;
                    Logger.LogDebug($"Increasing Spread because Player isn't looking. Magnitude: [{vectorSpread.magnitude}]");
                }
                return vectorSpread;
            }
            return Vector3.zero;
        }
    }

    internal class ScatterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(HelpersGClass.AimDataType, "method_9");
        }

        [PatchPrefix]
        public static void PatchPrefix(BotOwner ___botOwner_0, ref float additionCoef)
        {
            additionCoef = 1f;
            additionCoef *= SAINPlugin.LoadedPreset.GlobalSettings.Shoot.GlobalScatterMultiplier;
            if (!SAINEnableClass.GetSAIN(___botOwner_0, out var bot, nameof(ScatterPatch)))
            {
                return;
            }
            additionCoef *= bot.Info.FileSettings.Scattering.ScatterMultiplier;
            SAINEnemy enemy = bot.EnemyController.CheckAddEnemy(___botOwner_0?.Memory?.GoalEnemy?.Person);
            if (enemy == null)
            {
                return;
            }
            additionCoef /= enemy.EnemyAim.AimAndScatterMultiplier;
        }
    }

    internal class WeaponPresetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotWeaponManager), "UpdateHandsController");
        }

        [PatchPostfix]
        public static void Patch(BotOwner ___botOwner_0, IHandsController handsController)
        {
            IFirearmHandsController firearmHandsController;
            if ((firearmHandsController = (handsController as IFirearmHandsController)) != null)
            {
                SAINBotController.Instance?.BotChangedWeapon(___botOwner_0, firearmHandsController);
            }
        }
    }

    public class AimTimePatch : ModulePatch
    {
        private static PropertyInfo _PanicingProp;

        protected override MethodBase GetTargetMethod()
        {
            _PanicingProp = AccessTools.Property(HelpersGClass.AimDataType, "Boolean_0");
            return AccessTools.Method(HelpersGClass.AimDataType, "method_7");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0, float dist, float ang, ref bool ___bool_1, ref float ___float_10, ref float __result)
        {
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return true;
            }

            float aimDelay = ___float_10;
            bool moving = ___bool_1;
            bool panicing = (bool)_PanicingProp.GetValue(___botOwner_0.AimingData);

            __result = calculateAim(___botOwner_0, dist, ang, moving, panicing, aimDelay);

            return false;
        }

        private static float calculateAim(BotOwner botOwner, float distance, float angle, bool moving, bool panicing, float aimDelay)
        {
            StringBuilder stringBuilder = SAINPlugin.LoadedPreset.GlobalSettings.Debug.DebugAimCalculations ? new StringBuilder() : null;
            stringBuilder?.AppendLine($"Aim Time Calculation for [{botOwner.name} : {botOwner.Profile.Info.Settings.Role} : {botOwner.Profile.Info.Settings.BotDifficulty}]");

            SAINPlugin.BotController.GetSAIN(botOwner, out var botComponent);
            SAINAimingSettings sainAimSettings = botComponent?.Info.FileSettings.Aiming;
            BotSettingsComponents fileSettings = botOwner.Settings.FileSettings;

            float baseAimTime = fileSettings.Aiming.BOTTOM_COEF;
            stringBuilder.AppendLine($"baseAimTime [{baseAimTime}]");

            baseAimTime = calcCoverMod(baseAimTime, botOwner, botComponent, fileSettings, stringBuilder);

            BotCurvSettings curve = botOwner.Settings.Curv;
            float modifier = sainAimSettings != null ? sainAimSettings.AngleAimTimeMultiplier : 1f;
            float angleTime = calcCurveOutput(curve.AimAngCoef, angle, modifier, stringBuilder, "Angle");

            modifier = sainAimSettings != null ? sainAimSettings.DistanceAimTimeMultiplier : 1f;
            float distanceTime = calcCurveOutput(curve.AimTime2Dist, distance, modifier, stringBuilder, "Distance");

            float calculatedAimTime = calcAimTime(angleTime, distanceTime, botOwner, stringBuilder);
            calculatedAimTime = calcPanic(panicing, calculatedAimTime, fileSettings, stringBuilder);

            float timeToAimResult = (baseAimTime + calculatedAimTime + aimDelay);
            stringBuilder?.AppendLine($"timeToAimResult [{timeToAimResult}] (baseAimTime + calculatedAimTime + aimDelay)");

            timeToAimResult = calcMoveModifier(moving, timeToAimResult, fileSettings, stringBuilder);
            timeToAimResult = calcADSModifier(botOwner.WeaponManager?.ShootController?.IsAiming == true, timeToAimResult, stringBuilder);
            timeToAimResult = clampAimTime(timeToAimResult, fileSettings, stringBuilder);
            timeToAimResult = calcFasterCQB(distance, timeToAimResult, sainAimSettings, stringBuilder);
            timeToAimResult = calcAttachmentMod(botComponent, timeToAimResult, stringBuilder);

            if (stringBuilder != null)
            {
                Logger.LogDebug(stringBuilder.ToString());
            }

            if (botComponent != null)
            {
                botComponent.LastAimTime = timeToAimResult;
            }

            return timeToAimResult;
        }

        private static float calcAimTime(float angleTime, float distanceTime, BotOwner botOwner, StringBuilder stringBuilder)
        {
            float accuracySpeed = botOwner.Settings.Current.CurrentAccuratySpeed;
            stringBuilder?.AppendLine($"accuracySpeed [{accuracySpeed}]");

            float calculatedAimTime = angleTime * distanceTime * accuracySpeed;
            stringBuilder?.AppendLine($"calculatedAimTime [{calculatedAimTime}] (angleTime * distanceTime * accuracySpeed)");
            return calculatedAimTime;
        }

        private static float calcCoverMod(float baseAimTime, BotOwner botOwner, BotComponent botComponent, BotSettingsComponents fileSettings, StringBuilder stringBuilder)
        {
            CoverPoint coverInUse = botComponent?.Cover.CoverInUse;
            bool inCover = botOwner.Memory.IsInCover || (coverInUse != null && coverInUse.Status == CoverStatus.InCover);
            if (inCover)
            {
                baseAimTime *= fileSettings.Aiming.COEF_FROM_COVER;
                stringBuilder?.AppendLine($"In Cover: [{baseAimTime}] : COEF_FROM_COVER [{fileSettings.Aiming.COEF_FROM_COVER}]");
            }
            return baseAimTime;
        }

        private static float calcCurveOutput(AnimationCurve curve, float input, float modifier, StringBuilder stringBuilder, string curveType)
        {
            float result = curve.Evaluate(input);
            result *= modifier;
            stringBuilder?.AppendLine($"{curveType} Curve Output [{result}] : input [{input}] : Multiplier: [{modifier}]");
            return result;
        }

        private static float calcMoveModifier(bool moving, float timeToAimResult, BotSettingsComponents fileSettings, StringBuilder stringBuilder)
        {
            if (moving)
            {
                timeToAimResult *= fileSettings.Aiming.COEF_IF_MOVE;
                stringBuilder?.AppendLine($"Moving [{timeToAimResult}] : Moving Coef [{fileSettings.Aiming.COEF_IF_MOVE}]");
            }
            return timeToAimResult;
        }

        private static float calcADSModifier(bool aiming, float timeToAimResult, StringBuilder stringBuilder)
        {
            if (aiming)
            {
                float adsMulti = SAINPlugin.LoadedPreset.GlobalSettings.Aiming.AimDownSightsAimTimeMultiplier;
                timeToAimResult *= adsMulti;
                stringBuilder?.AppendLine($"Aiming Down Sights [{timeToAimResult}] : ADS Multiplier [{adsMulti}]");
            }
            return timeToAimResult;
        }

        private static float clampAimTime(float timeToAimResult, BotSettingsComponents fileSettings, StringBuilder stringBuilder)
        {
            float clampedResult = Mathf.Clamp(timeToAimResult, 0f, fileSettings.Aiming.MAX_AIM_TIME);
            if (clampedResult != timeToAimResult)
            {
                stringBuilder?.AppendLine($"Clamped Aim Time [{clampedResult}] : MAX_AIM_TIME [{fileSettings.Aiming.MAX_AIM_TIME}]");
            }
            return clampedResult;
        }

        private static float calcPanic(bool panicing, float calculatedAimTime, BotSettingsComponents fileSettings, StringBuilder stringBuilder)
        {
            if (panicing)
            {
                calculatedAimTime *= fileSettings.Aiming.PANIC_COEF;
                stringBuilder?.AppendLine($"Panicing [{calculatedAimTime}] : Panic Coef [{fileSettings.Aiming.PANIC_COEF}]");
            }
            return calculatedAimTime;
        }

        private static float calcFasterCQB(float distance, float aimTimeResult, SAINAimingSettings aimSettings, StringBuilder stringBuilder)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Aiming.FasterCQBReactionsGlobal)
            {
                return aimTimeResult;
            }
            if (aimSettings?.FasterCQBReactions == true &&
                distance <= aimSettings.FasterCQBReactionsDistance)
            {
                float ratio = distance / aimSettings.FasterCQBReactionsDistance;
                float fasterTime = aimTimeResult * ratio;
                fasterTime = Mathf.Clamp(fasterTime, aimSettings.FasterCQBReactionsMinimum, aimTimeResult);
                stringBuilder?.AppendLine($"Faster CQB Aim Time: Result [{fasterTime}] : Original [{aimTimeResult}] : At Distance [{distance}] with maxDist [{aimSettings.FasterCQBReactionsDistance}]");
                return fasterTime;
            }
            return aimTimeResult;
        }

        private static float calcAttachmentMod(BotComponent bot, float aimTimeResult, StringBuilder stringBuilder)
        {
            SAINEnemy enemy = bot?.Enemy;
            if (enemy != null)
            {
                float modifier = enemy.EnemyAim.AimAndScatterMultiplier;
                stringBuilder?.AppendLine($"Bot Attachment Mod: Result [{aimTimeResult / modifier}] : Original [{aimTimeResult}] : Modifier [{modifier}]");
                aimTimeResult /= modifier;
            }
            return aimTimeResult;
        }
    }
}
