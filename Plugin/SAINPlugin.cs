using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using DrakiaXYZ.VersionChecker;
using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.BotController;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Layers;
using SAIN.Patches.Generic;
using SAIN.Patches.Hearing;
using SAIN.Patches.Shoot;
using SAIN.Patches.Vision;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Mover;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using static EFT.SpeedTree.TreeWind;
using static SAIN.AssemblyInfoClass;

namespace SAIN
{
    [BepInPlugin(SAINGUID, SAINName, SAINVersion)]
    [BepInDependency(BigBrainGUID, BigBrainVersion)]
    [BepInDependency(WaypointsGUID, WaypointsVersion)]
    [BepInDependency(SPTGUID, SPTVersion)]
    [BepInProcess(EscapeFromTarkov)]
    [BepInIncompatibility("com.dvize.BushNoESP")]
    [BepInIncompatibility("com.dvize.NoGrenadeESP")]
    public class SAINPlugin : BaseUnityPlugin
    {
        public static bool IsBotExluded(BotOwner botOwner)
        {
            if (_excludedIDs.Contains(botOwner.ProfileId))
            {
                return true;
            }
            if (_sainIDs.Contains(botOwner.ProfileId))
            {
                return false;
            }
            bool isExcluded = IsBotExluded(botOwner.Profile.Info.Settings.Role, botOwner.Profile.Nickname);
            if (isExcluded)
            {
                _excludedIDs.Add(botOwner.ProfileId);
            }
            else
            {
                _sainIDs.Add(botOwner.ProfileId);
            }
            return isExcluded;
        }

        private static List<string> _excludedIDs = new List<string>();
        private static List<string> _sainIDs = new List<string>();

        public static void ClearExcludedIDs()
        {
            if (_excludedIDs.Count > 0)
                _excludedIDs.Clear();

            if (_sainIDs.Count > 0) 
                _sainIDs.Clear();
        }

        public static bool IsBotExluded(WildSpawnType wildSpawnType, string nickname)
        {
            if (BotSpawnController.ExclusionList.Contains(wildSpawnType))
            {
                return true;
            }
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.VanillaBosses &&
                !EnumValues.WildSpawn.IsGoons(wildSpawnType) && 
                (EnumValues.WildSpawn.IsBoss(wildSpawnType) || 
                EnumValues.WildSpawn.IsFollower(wildSpawnType)))
            {
                return true;
            }
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.VanillaScavs &&
                EnumValues.WildSpawn.IsScav(wildSpawnType))
            {
                if (!SAINPlugin.LoadedPreset.GlobalSettings.General.VanillaPlayerScavs && isPlayerScav(nickname))
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        private static bool isPlayerScav(string nickname)
        {
            // Pattern: xxx (xxx)
            string pattern = "\\w+.[(]\\w+[)]";
            Regex regex = new Regex(pattern);
            if (regex.Matches(nickname).Count > 0)
            {
                return true;
            }
            return false;
        }

        public static bool GetSAIN(BotOwner botOwner, out BotComponent sain, string patchName)
        {
            sain = null;
            if (SAINPlugin.BotController == null)
            {
                SAIN.Logger.LogError($"Bot Controller Null in [{patchName}]");
                return false;
            }
            return SAINPlugin.BotController.GetSAIN(botOwner, out sain);
        }

        public static bool GetSAIN(Player player, out BotComponent sain, string patchName)
        {
            sain = null;
            if (player != null && !player.IsAI)
            {
                return false;
            }
            if (SAINPlugin.BotController == null)
            {
                SAIN.Logger.LogError($"Bot Controller Null in [{patchName}]");
                return false;
            }
            return SAINPlugin.BotController.GetSAIN(player, out sain);
        }

        public static bool DebugMode => EditorDefaults.GlobalDebugMode;
        public static bool DrawDebugGizmos => EditorDefaults.DrawDebugGizmos;
        public static PresetEditorDefaults EditorDefaults => PresetHandler.EditorDefaults;

        public static SoloDecision ForceSoloDecision = SoloDecision.None;
        public static SquadDecision ForceSquadDecision = SquadDecision.None;
        public static SelfDecision ForceSelfDecision = SelfDecision.None;

        private void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            //new DefaultBrainsClass();

            PresetHandler.Init();
            BindConfigs();
            Patches();
            BigBrainHandler.Init();
            Vector.Init();
        }

        private void BindConfigs()
        {
            string category = "SAIN Editor";

            NextDebugOverlay = Config.Bind(category, "Next Debug Overlay", new KeyboardShortcut(KeyCode.LeftBracket), "Change The Debug Overlay with DrakiaXYZs Debug Overlay");
            PreviousDebugOverlay = Config.Bind(category, "Previous Debug Overlay", new KeyboardShortcut(KeyCode.RightBracket), "Change The Debug Overlay with DrakiaXYZs Debug Overlay");

            OpenEditorButton = Config.Bind(category, "Open Editor", false, "Opens the Editor on press");
            OpenEditorConfigEntry = Config.Bind(category, "Open Editor Shortcut", new KeyboardShortcut(KeyCode.F6), "The keyboard shortcut that toggles editor");
        }

        public static ConfigEntry<KeyboardShortcut> NextDebugOverlay { get; private set; }
        public static ConfigEntry<KeyboardShortcut> PreviousDebugOverlay { get; private set; }
        public static ConfigEntry<bool> OpenEditorButton { get; private set; }
        public static ConfigEntry<KeyboardShortcut> OpenEditorConfigEntry { get; private set; }

        private void Patches()
        {
            var patches = new List<Type>() {
                //typeof(Patches.Generic.ShallRunAwayGrenadePatch),
                
                typeof(Patches.Generic.SetPanicPointPatch),
                typeof(Patches.Generic.AddPointToSearchPatch),
                typeof(Patches.Generic.CalcPowerPatch),
                typeof(Patches.Generic.BulletImpactSuppressionPatch),
                typeof(Patches.Generic.HaveSeenEnemyPatch),
                typeof(Patches.Generic.StopSetToNavMeshPatch),
                typeof(Patches.Generic.TurnDamnLightOffPatch),
                typeof(Patches.Generic.RotateClampPatch),
                typeof(Patches.Generic.HealCancelPatch),
                typeof(Patches.Generic.DoorOpenerPatch),
                typeof(Patches.Generic.GetBotController),
                typeof(Patches.Generic.GetBotSpawner),
                typeof(Patches.Generic.GrenadeThrownActionPatch),
                typeof(Patches.Generic.GrenadeExplosionActionPatch),
                typeof(Patches.Generic.AimRotateSpeedPatch),
                typeof(Patches.Generic.OnMakingShotRecoilPatch),
                //typeof(Patches.Generic.GetHitPatch),
                typeof(Patches.Generic.BotGroupAddEnemyPatch),
                typeof(Patches.Generic.ForceNoHeadAimPatch),
                typeof(Patches.Generic.NoTeleportPatch),
                typeof(Patches.Generic.ShallKnowEnemyPatch),
                typeof(Patches.Generic.ShallKnowEnemyLatePatch),
                //typeof(Patches.Generic.SkipLookForCoverPatch),
                typeof(Patches.Generic.BotMemoryAddEnemyPatch),

                typeof(Patches.Hearing.LootingSoundPatch),
                typeof(Patches.Hearing.TurnSoundPatch),
                typeof(Patches.Hearing.ProneSoundPatch),
                typeof(Patches.Hearing.TryPlayShootSoundPatch),
                typeof(Patches.Hearing.OnMakingShotPatch),
                typeof(Patches.Hearing.HearingSensorPatch),
                typeof(Patches.Hearing.SoundClipNameCheckerPatch),
                typeof(Patches.Hearing.AimSoundPatch),
                typeof(Patches.Hearing.LootingSoundPatch),
                typeof(Patches.Hearing.SetInHandsGrenadePatch),
                typeof(Patches.Hearing.SetInHandsFoodPatch),
                typeof(Patches.Hearing.SetInHandsMedsPatch),

                typeof(Patches.Talk.PlayerTalkPatch),
                typeof(Patches.Talk.BotTalkPatch),
                typeof(Patches.Talk.BotTalkManualUpdatePatch),
                //typeof(Patches.Talk.TalkDisablePatch3),
                //typeof(Patches.Talk.TalkDisablePatch4),

                //typeof(Patches.Vision.AIVisionUpdateLimitPatch),
                typeof(Patches.Vision.WeatherTimeVisibleDistancePatch),
                typeof(Patches.Vision.NoAIESPPatch),
                typeof(Patches.Vision.BotLightTurnOnPatch),
                typeof(Patches.Vision.VisionSpeedPostPatch),
                typeof(Patches.Vision.VisionDistancePosePatch),
                typeof(Patches.Vision.CheckFlashlightPatch),

                typeof(Patches.Shoot.AimTimePatch),
                typeof(Patches.Shoot.AimOffsetPatch),
                typeof(Patches.Shoot.RecoilPatch),
                typeof(Patches.Shoot.LoseRecoilPatch),
                typeof(Patches.Shoot.EndRecoilPatch),
                typeof(Patches.Shoot.FullAutoPatch),
                typeof(Patches.Shoot.SemiAutoPatch),
                typeof(Patches.Shoot.SemiAutoPatch2),
                typeof(Patches.Shoot.SemiAutoPatch3),

                typeof(Patches.Components.AddComponentPatch)
            };

            // Reflection go brrrrrrrrrrrrrr
            MethodInfo enableMethod = AccessTools.Method(typeof(ModulePatch), "Enable");
            foreach (var patch in patches)
            {
                if (!typeof(ModulePatch).IsAssignableFrom(patch))
                {
                    Logger.LogError($"Type {patch.Name} is not a ModulePatch");
                    continue;
                }

                try
                {
                    enableMethod.Invoke(Activator.CreateInstance(patch), null);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        public static SAINPresetClass LoadedPreset => PresetHandler.LoadedPreset;

        public static SAINBotController BotController => GameWorldHandler.SAINBotController;

        private void Update()
        {
            DebugGizmos.Update();
            DebugOverlay.Update();
            ModDetection.Update();
            SAINEditor.Update();
            GameWorldHandler.Update();

            //SAINBotSpaceAwareness.Update();

            //SAINVaultClass.DebugVaultPointCount();

            LoadedPreset.GlobalSettings.Personality.Update();
            //BigBrainHandler.CheckLayers();
        }

        private void Start() => SAINEditor.Init();

        private void LateUpdate() => SAINEditor.LateUpdate();

        private void OnGUI() => SAINEditor.OnGUI();
    }
}
