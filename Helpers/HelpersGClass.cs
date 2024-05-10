using Comfort.Common;
using EFT;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using EFTCore = GClass531;
using EFTCoreContainer = GClass532;
using EFTFileSettings = BotSettingsComponents;
using EFTSettingsGroup = GClass458;
using EFTStatModifiersClass = GClass529;
using EFTTime = GClass1296;
using EFTSearchPoint = PlaceForCheck;
using Aki.Reflection.Patching;

////////
// Fixed some GClass References here, but classes were renamed in the deobfuscation, so much of this isn't necessary anymore. Need to clean this up
////////

namespace SAIN.Helpers
{
    internal class HelpersGClass
    {
        static HelpersGClass()
        {
            InventoryControllerProp = AccessTools.Field(typeof(Player), "_inventoryController");
            EFTBotSettingsProp = AccessTools.Property(typeof(BotDifficultySettingsClass), "FileSettings");
            RefreshSettingsMethod = AccessTools.Method(typeof(BotDifficultySettingsClass), "method_0");
            PathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
        }

        public static void RefreshSettings(BotDifficultySettingsClass settings)
        {
            RefreshSettingsMethod.Invoke(settings, null);
        }

        private static readonly MethodInfo RefreshSettingsMethod;

        public static readonly PropertyInfo EFTBotSettingsProp;
        public static readonly FieldInfo InventoryControllerProp;
        public static readonly FieldInfo PathControllerField;

        public static BotSettingsComponents GetEFTSettings(WildSpawnType type, BotDifficulty difficulty)
        {
            return (BotSettingsComponents)SAINPlugin.LoadedPreset.BotSettings.GetEFTSettings(type, difficulty);
        }

        public static PathControllerClass GetPathControllerClass(BotMover botMover)
        {
            return (PathControllerClass)PathControllerField.GetValue(botMover);
        }

        public static EFTCoreSettings EFTCore => SAINPlugin.LoadedPreset.GlobalSettings.EFTCoreSettings;
        public static float LAY_DOWN_ANG_SHOOT => EFTCore.Core.LAY_DOWN_ANG_SHOOT;
        public static float Gravity => EFTCore.Core.G;
        public static float SMOKE_GRENADE_RADIUS_COEF => EFTCore.Core.SMOKE_GRENADE_RADIUS_COEF;
    }

    public class TemporaryStatModifiers
    {
        public TemporaryStatModifiers(float precision, float accuracySpeed, float gainSight, float scatter, float priorityScatter)
        {
            Modifiers = new EFTStatModifiersClass
            {
                PrecicingSpeedCoef = precision,
                AccuratySpeedCoef = accuracySpeed,
                GainSightCoef = gainSight,
                ScatteringCoef = scatter,
                PriorityScatteringCoef = priorityScatter,
            };
        }

        public EFTStatModifiersClass Modifiers;
    }

    public class SearchPoint
    {
        public EFTSearchPoint Point;
    }

    public class EFTCoreSettings
    {
        public static EFTCoreSettings GetCore()
        {
            UpdateCoreSettings();
            return new EFTCoreSettings
            {
                Core = EFTCoreContainer.Core,
            };
        }

        public static void UpdateCoreSettings()
        {
            var core = EFTCoreContainer.Core;
            core.SCAV_GROUPS_TOGETHER = true;
            core.DIST_NOT_TO_GROUP = 75f;
            core.DIST_NOT_TO_GROUP_SQR = 75f * 75f;
            core.MIN_DIST_TO_STOP_RUN = 0f;
            core.CAN_SHOOT_TO_HEAD = false;
            core.ARMOR_CLASS_COEF = 6f;
            core.SHOTGUN_POWER = 40f;
            core.RIFLE_POWER = 50f;
            core.PISTOL_POWER = 20f;
            core.SMG_POWER = 60f;
            core.SNIPE_POWER = 5f;
            core.SOUND_DOOR_OPEN_METERS = 30f;
            core.SOUND_DOOR_BREACH_METERS = 60f;
            core.JUMP_SPREAD_DIST = 70f;
            core.BASE_WALK_SPEREAD2 = 70f;
            core.FLARE_TIME = 2.5f;
            core.PRONE_POSE = 0.5f;
            core.MOVE_COEF = 1.3f;
        }

        public static void UpdateArmorClassCoef(float coef)
        {
            EFTCoreContainer.Core.ARMOR_CLASS_COEF = coef;
        }

        public static void UpdateCoreSettings(EFTCoreSettings newCore)
        {
            EFTCoreContainer.Core = newCore.Core;
        }

        public EFTCore Core;
    }

    public class EFTBotSettings
    {
        [JsonConstructor]
        public EFTBotSettings()
        { }

        public EFTBotSettings(string name, WildSpawnType type, BotDifficulty[] difficulties)
        {
            Name = name;
            WildSpawnType = type;
            foreach (BotDifficulty diff in difficulties)
            {
                Settings.Add(diff, EFTCoreContainer.GetSettings(diff, type));
            }
        }

        [JsonProperty]
        public string Name;
        [JsonProperty]
        public WildSpawnType WildSpawnType;
        [JsonProperty]
        public Dictionary<BotDifficulty, BotSettingsComponents> Settings = new Dictionary<BotDifficulty, BotSettingsComponents>();
    }
}