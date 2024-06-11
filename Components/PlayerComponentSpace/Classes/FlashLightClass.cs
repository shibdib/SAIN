using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Visual;
using HarmonyLib;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.Components
{
    public class FlashLightClass : PlayerComponentBase
    {
        public FlashLightClass(PlayerComponent component) : base(component)
        {
            LightDetection = new LightDetectionClass(component);
        }

        public bool UsingLight => Player.AIData?.UsingLight == true;
        public LightDetectionClass LightDetection { get; private set; }

        public void Update()
        {
            clearPoints();
            createPoints();
            detectPoints();
        }

        private void clearPoints()
        {
            var points = LightDetection.LightPoints;
            if (points.Count > 0)
            {
                points.RemoveAll(x => x.ShallExpire);
            }
        }

        private void createPoints()
        {
            if (!PlayerComponent.IsAI &&
                _nextPointCreateTime < Time.time && 
                UsingLight &&
                ActiveModes.Count > 0)
            {
                _nextPointCreateTime = Time.time + 0.05f;
                bool onlyLaser = !WhiteLight && !IRLight && (Laser || IRLaser);
                LightDetection.CreateDetectionPoints(WhiteLight || Laser, onlyLaser);
                //Logger.LogDebug("Creating flashlight points");
            }
        }

        private void detectPoints()
        {
            if (PlayerComponent.IsAI && 
                _nextPointCheckTime < Time.time)
            {
                _nextPointCheckTime = Time.time + 0.05f;
                LightDetection.DetectAndInvestigateFlashlight();
            }
        }

        public readonly List<DeviceMode> ActiveModes = new List<DeviceMode>();

        public bool IRLaser => ActiveModes.Contains(DeviceMode.IRLaser);
        public bool IRLight => ActiveModes.Contains(DeviceMode.IRLight);
        public bool Laser => ActiveModes.Contains(DeviceMode.VisibleLaser);
        public bool WhiteLight => ActiveModes.Contains(DeviceMode.WhiteLight);

        public void CheckDevice()
        {
            Player player = Player;

            if (player == null) return;

            if (_tacticalModesField == null)
            {
                Logger.LogError("Could find not find _tacticalModesField");
                return;
            }

            // Get the firearmsController for the player, this will be their IsCurrentEnemy weapon
            Player.FirearmController firearmController = player.HandsController as Player.FirearmController;
            if (firearmController == null)
            {
                Logger.LogError("Could find not find firearmController");
                return;
            }

            // Get the list of tacticalComboVisualControllers for the current weapon (One should exist for every flashlight, laser, or combo device)
            Transform weaponRoot = firearmController.WeaponRoot;
            List<TacticalComboVisualController> tacticalComboVisualControllers = weaponRoot.GetComponentsInChildrenActiveIgnoreFirstLevel<TacticalComboVisualController>();
            if (tacticalComboVisualControllers == null)
            {
                Logger.LogError("Could find not find tacticalComboVisualControllers");
                return;
            }

            bool WhiteLight = false;
            bool Laser = false;
            bool IRLight = false;
            bool IRLaser = false;

            ActiveModes.Clear();

            // Loop through all of the tacticalComboVisualControllers, then its modes, then that modes children, and look for a light
            foreach (TacticalComboVisualController tacticalComboVisualController in tacticalComboVisualControllers)
            {
                List<Transform> tacticalModes = _tacticalModesField.GetValue(tacticalComboVisualController) as List<Transform>;

                if (!WhiteLight && 
                    CheckWhiteLight(tacticalModes))
                {
                    if (_debugMode) Logger.LogDebug("Found Light!");
                    WhiteLight = true;
                    ActiveModes.Add(DeviceMode.WhiteLight);
                }

                if (!Laser && 
                    CheckVisibleLaser(tacticalModes))
                {
                    if (_debugMode) Logger.LogDebug("Found Visible Laser!");
                    Laser = true;
                    ActiveModes.Add(DeviceMode.VisibleLaser);
                }

                if (!IRLight && 
                    CheckIRLight(tacticalModes))
                {
                    if (_debugMode) Logger.LogDebug("Found IR Light!");
                    IRLight = true;
                    ActiveModes.Add(DeviceMode.IRLight);
                }

                if (!IRLaser && 
                    CheckIRLaser(tacticalModes))
                {
                    if (_debugMode) Logger.LogDebug("Found IR Laser!");
                    IRLaser = true;
                    ActiveModes.Add(DeviceMode.IRLaser);
                }
            }
        }

        private bool CheckVisibleLaser(List<Transform> tacticalModes)
        {
            foreach (Transform tacticalMode in tacticalModes)
            {
                // Skip disabled modes
                if (!tacticalMode.gameObject.activeInHierarchy) continue;

                // Try to find a "light" under the mode, here's hoping BSG stay consistent
                foreach (Transform child in tacticalMode.GetChildren())
                {
                    if (child.name.StartsWith("VIS_"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckIRLight(List<Transform> tacticalModes)
        {
            foreach (Transform tacticalMode in tacticalModes)
            {
                // Skip disabled modes
                if (!tacticalMode.gameObject.activeInHierarchy) continue;

                // Try to find a "VolumetricLight", hopefully only visible flashlights have these
                IkLight irLight = tacticalMode.GetComponentInChildren<IkLight>();
                if (irLight != null)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckIRLaser(List<Transform> tacticalModes)
        {
            foreach (Transform tacticalMode in tacticalModes)
            {
                // Skip disabled modes
                if (!tacticalMode.gameObject.activeInHierarchy) continue;

                // Try to find a "light" under the mode, here's hoping BSG stay consistent
                foreach (Transform child in tacticalMode.GetChildren())
                {
                    if (child.name.StartsWith("IR_"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckWhiteLight(List<Transform> tacticalModes)
        {
            foreach (Transform tacticalMode in tacticalModes)
            {
                // Skip disabled modes
                if (!tacticalMode.gameObject.activeInHierarchy) continue;

                // Try to find a "VolumetricLight", hopefully only visible flashlights have these
                VolumetricLight volumetricLight = tacticalMode.GetComponentInChildren<VolumetricLight>();
                if (volumetricLight != null)
                {
                    return true;
                }
            }
            return false;
        }

        private float _nextPointCheckTime;
        private float _nextPointCreateTime;
        static bool _debugMode => SAINPlugin.LoadedPreset.GlobalSettings.Flashlight.DebugFlash;

        static FlashLightClass()
        {
            _tacticalModesField = AccessTools.Field(typeof(TacticalComboVisualController), "list_0");
        }

        private static readonly FieldInfo _tacticalModesField;
    }
}