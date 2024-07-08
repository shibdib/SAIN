using EFT;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.GlobalSettings;
using System.Text;
using UnityEngine;

namespace SAIN.Helpers
{
    internal class UpdateSettingClass
    {
        public static void ManualSettingsUpdate(WildSpawnType WildSpawnType, BotDifficulty botDifficulty, BotSettingsComponents eftSettings, BotSettingsComponents defaultSettings = null, SAINSettingsClass sainSettings = null)
        {
            if (sainSettings == null)
                sainSettings = SAINPlugin.LoadedPreset.BotSettings.GetSAINSettings(WildSpawnType, botDifficulty);

            if (defaultSettings == null)
                defaultSettings = HelpersGClass.GetEFTSettings(WildSpawnType, botDifficulty);

            StringBuilder debugString = new StringBuilder();
            if (SAINPlugin.DebugMode)
            {
                debugString.AppendLine($"Applied Multipliers for [{WildSpawnType}, {botDifficulty}]");
            }

            eftSettings.Core.VisibleDistance = MultiplySetting(
                sainSettings.Core.VisibleDistance,
                VisionDistanceMulti,
                "VisibleDistance",
                debugString);

            eftSettings.Core.GainSightCoef = MultiplySetting(
                sainSettings.Core.GainSightCoef,
                VisionSpeedMulti(sainSettings),
                "GainSightCoef",
                debugString);

            if (SAINPlugin.DebugMode)
            {
                Logger.LogDebug(debugString);
            }
        }

        public static void ManualSettingsUpdate(WildSpawnType WildSpawnType, BotDifficulty botDifficulty, BotOwner BotOwner, SAINSettingsClass sainSettings)
        {
            var eftSettings = BotOwner.Settings.FileSettings;

            ManualSettingsUpdate(WildSpawnType, botDifficulty, eftSettings, null, sainSettings);

            //if (BotOwner.WeaponManager?.WeaponAIPreset != null)
            //{
            //    BotOwner.WeaponManager.WeaponAIPreset.XZ_COEF = eftSettings.Aiming.XZ_COEF;
            //    BotOwner.WeaponManager.WeaponAIPreset.BaseShift = eftSettings.Aiming.BASE_SHIEF;
            //}
        }

        private static float MultiplySetting(float defaultValue, float multiplier, string name, StringBuilder debugString)
        {
            float result = Mathf.Round(defaultValue * multiplier * 100f) / 100f;
            if (SAINPlugin.DebugMode)
            {
                debugString.AppendLabeledValue($"Multiplied [{name}]", $"Default Value: [{defaultValue}] Multiplier: [{multiplier}] Result: [{result}]", Color.white, Color.white);
            }
            return result;
        }

        public static float VisionSpeedMulti(SAINSettingsClass SAINSettings) 
            => Round(SAINSettings.Look.VisionSpeedModifier * GlobalSettingsClass.Instance.Look.VisionSpeed.GlobalVisionSpeedModifier);

        public static float VisionDistanceMulti => GlobalSettingsClass.Instance.Look.VisionDistance.GlobalVisionDistanceMultiplier;

        private static float Round(float value)
        {
            return Mathf.Round(value * 100f) / 100f;
        }
    }
}