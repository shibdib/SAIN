﻿using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using EFT.Weather;
using HarmonyLib;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components
{
    public class GameWorldComponent : MonoBehaviour
    {
        public event Action<Door, EDoorState, bool> OnDoorStateChanged;
        public event Action<bool> OnDoorsDisabled;

        public void ChangeDoorState(Door door, EDoorState state, bool shallInvert)
        {
            if (shallInvert)
                door.OpenAngle = -door.OpenAngle;

            door.method_3(state);
            OnDoorStateChanged?.Invoke(door, state, shallInvert);

            if (shallInvert)
                door.OpenAngle = -door.OpenAngle;
        }

        public void HostDisabledDoors(bool value)
        {
            _doorsDisabledByHost = value;
        }

        private bool _doorsDisabledByHost;

        public bool WinterActive => Season == ESeason.Winter;

        public static GameWorldComponent Instance { get; set; }
        public ELocation Location { get; private set; }
        public GameWorld GameWorld { get; private set; }
        public PlayerSpawnTracker PlayerTracker { get; private set; }
        public SAINBotController SAINBotController { get; private set; }
        public Extract.ExtractFinderComponent ExtractFinder { get; private set; }
        public SpawnPointMarker[] SpawnPointMarkers { get; private set; }

        private void Update()
        {
            checkDoors();
            findWeather();
            findLocation();
            findSpawnPointMarkers();
        }

        private void findWeather()
        {
            if (_weatherFound)
            {
                return;
            }
            if (_nextCheckWeatherTime > Time.time)
            {
                return;
            }
            _nextCheckWeatherTime = Time.time + 0.5f;

            var weather = (AccessTools.Field(typeof(GameWorld), "ginterface26_0").GetValue(GameWorld) as GInterface26);
            if (weather == null)
            {
                Season = ESeason.Summer;
            }
            else
            {
                Logger.LogWarning($"Got Weather! {weather.Status}");
                Season = weather.Season;
                _weatherFound = true;
            }
        }


        private void checkDoors()
        {
            if (Singleton<IBotGame>.Instance == null) { return; }

            bool shallDisable = _doorsDisabledByHost || GlobalSettingsClass.Instance.General.Doors.DisableAllDoors;

            if (!_doorsDisabled &&
                shallDisable)
            {
                OnDoorsDisabled?.Invoke(true);
                _doorsDisabled = true; 
                disableDoors();
                return;
            }
            if (_doorsDisabled &&
                !shallDisable)
            {
                OnDoorsDisabled?.Invoke(false);
                _doorsDisabled = false;
                enableDoors();
                return;
            }
        }

        public bool DisableDoor(Door door)
        {
            // We don't support doors that don't start open/closed
            if (door.DoorState != EDoorState.Open && door.DoorState != EDoorState.Shut)
                return false;

            // We don't support non-operatable doors
            if (!door.Operatable || !door.enabled)
                return false;

            // We don't support doors that aren't on the "Interactive" layer
            if (door.gameObject.layer != LayerMaskClass.InteractiveLayer)
                return false;

            door.gameObject.SmartDisable();
            door.enabled = false;
            _disabledDoors.Add(door);
            return true;
        }

        private void disableDoors()
        {
            int doorCount = 0;
            // Code taken from Drakia's Door Randomizer Mod
            FindObjectsOfType<Door>().ExecuteForEach(door =>
            {
                if (DisableDoor(door))
                    doorCount++;
            });

            _doorsDisabled = true;
            Logger.LogDebug($"Disabled Doors: {doorCount}");
        }


        private void enableDoors()
        {
            int doorCount = 0;
            foreach (var door in _disabledDoors)
            {
                door.gameObject.SmartEnable();
                door.enabled = true;
            }
            _disabledDoors.Clear();
            _doorsDisabled = false;
            Logger.LogDebug($"Enabled Doors: {doorCount}");
        }

        private bool _doorsDisabled;
        private readonly List<Door> _disabledDoors = new List<Door>();

        private bool _weatherFound;
        private float _nextCheckWeatherTime;

        private void findLocation()
        {
            if (!_foundLocation)
            {
                Location = parseLocation();
            }
        }

        private ELocation parseLocation()
        {
            ELocation Location = ELocation.None;
            string locationString = GameWorld?.LocationId;
            if (locationString.IsNullOrEmpty())
            {
                return Location;
            }

            switch (locationString.ToLower())
            {
                case "bigmap":
                    Location = ELocation.Customs;
                    break;

                case "factory4_day":
                    Location = ELocation.Factory;
                    break;

                case "factory4_night":
                    Location = ELocation.FactoryNight;
                    break;

                case "interchange":
                    Location = ELocation.Interchange;
                    break;

                case "laboratory":
                    Location = ELocation.Labs;
                    break;

                case "lighthouse":
                    Location = ELocation.Lighthouse;
                    break;

                case "rezervbase":
                    Location = ELocation.Reserve;
                    break;

                case "sandbox":
                    Location = ELocation.GroundZero;
                    break;

                case "shoreline":
                    Location = ELocation.Shoreline;
                    break;

                case "tarkovstreets":
                    Location = ELocation.Streets;
                    break;

                case "terminal":
                    Location = ELocation.Terminal;
                    break;

                case "town":
                    Location = ELocation.Town;
                    break;

                default:
                    Location = ELocation.None;
                    break;
            }

            _foundLocation = true;
            return Location;
        }

        public ESeason Season { get; private set; }

        private void findSpawnPointMarkers()
        {
            if ((SpawnPointMarkers != null) || (Camera.main == null))
            {
                return;
            }

            SpawnPointMarkers = UnityEngine.Object.FindObjectsOfType<SpawnPointMarker>();

            if (SAINPlugin.DebugMode)
                Logger.LogInfo($"Found {SpawnPointMarkers.Length} spawn point markers");
        }

        public IEnumerable<Vector3> GetAllSpawnPointPositionsOnNavMesh()
        {
            List<Vector3> spawnPointPositions = new List<Vector3>();
            foreach (SpawnPointMarker spawnPointMarker in SpawnPointMarkers)
            {
                // Try to find a point on the NavMesh nearby the spawn point
                Vector3? spawnPointPosition = NavMeshHelpers.GetNearbyNavMeshPoint(spawnPointMarker.Position, 2);
                if (spawnPointPosition.HasValue && !spawnPointPositions.Contains(spawnPointPosition.Value))
                {
                    spawnPointPositions.Add(spawnPointPosition.Value);
                }
            }

            return spawnPointPositions;
        }

        private void Awake()
        {
            Instance = this;
            GameWorld = this.GetComponent<GameWorld>();
            if (GameWorld == null)
            {
                Logger.LogWarning($"GameWorld is null from GetComponent");
            }
            StartCoroutine(Init());
        }

        private IEnumerator Init()
        {
            yield return getGameWorld();

            if (GameWorld == null)
            {
                Logger.LogWarning("GameWorld Null, cannot Init SAIN Gameworld! Check 2. Disposing Component...");
                Dispose();
                yield break;
            }

            PlayerTracker = new PlayerSpawnTracker(this);
            SAINBotController = this.GetOrAddComponent<SAINBotController>();
            ExtractFinder = this.GetOrAddComponent<Extract.ExtractFinderComponent>();
            GameWorld.OnDispose += Dispose;

            Logger.LogWarning("SAIN GameWorld Created.");
        }

        private IEnumerator getGameWorld()
        {
            if (GameWorld != null)
            {
                yield break;
            }
            if (GameWorld == null)
            {
                yield return new WaitForEndOfFrame();
                GameWorld = findGameWorld();
                if (GameWorld != null)
                {
                    Logger.LogWarning("Found GameWorld at EndOfFrame");
                    yield break;
                }
            }
            for (int i = 0; i < 30; i++)
            {
                if (GameWorld == null)
                {
                    yield return null;
                    GameWorld = findGameWorld();
                }
                if (GameWorld != null)
                {
                    break;
                }
            }
        }

        private GameWorld findGameWorld()
        {
            GameWorld gameWorld = this.GetComponent<GameWorld>();
            if (gameWorld == null)
            {
                gameWorld = Singleton<GameWorld>.Instance;
            }
            return gameWorld;
        }

        public void Dispose()
        {
            Instance = null;
            try
            {
                PlayerTracker?.Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose GameWorld Component Class Error: {e}");
            }

            try
            {
                ComponentHelpers.DestroyComponent(SAINBotController);
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose GameWorld SubComponent Error: {e}");
            }

            Instance = null;
            GameWorld.OnDispose -= Dispose;
            Destroy(this);
            Logger.LogWarning("SAIN GameWorld Destroyed.");
        }

        private bool _foundLocation = false;
    }
}