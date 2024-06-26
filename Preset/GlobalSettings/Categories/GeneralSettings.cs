using HarmonyLib;
using Newtonsoft.Json;
using SAIN.Attributes;
using System;
using System.Reflection;

namespace SAIN.Preset.GlobalSettings
{
    public static class CloneSettingsClass
    {
        public static object Clone(object original)
        {
            Type type = original.GetType();
            object clone = Activator.CreateInstance(type);
            FieldInfo[] fields = type.GetFields(System.Reflection.BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                object originalValue = field.GetValue(original);
                field.SetValue(clone, originalValue);
            }
            return clone;
        }
    }

    public abstract class SAINSettingsBase<T>
    {
        [Hidden]
        [JsonIgnore]
        public static readonly T Defaults = (T)Activator.CreateInstance(typeof(T));
    }

    public class GeneralSettings
    {
        [JsonIgnore]
        [Hidden]
        public static readonly GeneralSettings Defaults = new GeneralSettings();

        [Name("Bots Use Grenades")]
        [Default(true)]
        public bool BotsUseGrenades = true;

        [Name("Bots Open Doors Fast")]
        [Description("WIP. Can cause bots to get stuck on doors sometimes.")]
        [Default(true)]
        public bool NewDoorOpening = true;

        [Name("Limit SAIN Function in AI vs AI")]
        [Description("Disables certains functions when ai are fighting other ai, and they aren't close to a human player. Turn off if you are spectating ai in free-cam.")]
        [Default(true)]
        public bool LimitAIvsAI = true;

        [Name("Max AI vs AI audio range for Distant Bots")]
        [Description("Bots will not hear gunshots from other bots past this distance (meters) if they are far away (around 250 meters) from the player")]
        [Default(125f)]
        [Advanced]
        public float LimitAIvsAIMaxAudioRange = 125f;

        [Name("Max AI vs AI audio range for Very Distant Bots")]
        [Description("Bots will not hear gunshots from other bots past this distance (meters) if they are VERY far away (around 400 meters) from the player")]
        [Default(80f)]
        [Advanced]
        public float LimitAIvsAIMaxAudioRangeVeryFar = 80f;

        [Name("Random Speed Hacker AI")]
        [Description("Emulate the real Live-Like experience! 1% of bots will be a speed-hacker.")]
        [Default(false)]
        public bool RandomSpeedHacker = false;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [Default(24)]
        [MinMax(0, 100)]
        [Hidden]
        [JsonIgnore]
        public int SAINCombatSquadLayerPriority = 22;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [Default(22)]
        [MinMax(0, 100)]
        [Hidden]
        [JsonIgnore]
        public int SAINExtractLayerPriority = 24;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [Default(20)]
        [MinMax(0, 100)]
        [Hidden]
        [JsonIgnore]
        public int SAINCombatSoloLayerPriority = 20;

        [JsonIgnore]
        [Hidden]
        public float SprintReachDistance = 1f;

        [JsonIgnore]
        [Hidden]
        public float BaseReachDistance = 0.5f;
    }
}