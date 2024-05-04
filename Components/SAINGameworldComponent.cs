using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using SAIN.Components.BotController;
using SAIN.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.Components
{
    public class SAINGameworldComponent : MonoBehaviour
    {
        private void Awake()
        {
            SAINBotController = this.GetOrAddComponent<SAINBotControllerComponent>();
            ExtractFinder = this.GetOrAddComponent<Extract.ExtractFinderComponent>();
        }

        private void Update()
        {
            if (SAINMainPlayer == null)
            {
                SAINMainPlayer = ComponentHelpers.AddOrDestroyComponent(SAINMainPlayer, GameWorld?.MainPlayer);
            }

            FindLocation();
            findSpawnPointMarkers();
        }

        private bool _locationInitialized;
        private void FindLocation()
        {
            if (GameWorld == null || (!_locationInitialized && GameWorld.LocationId.IsNullOrEmpty()))
            {
                return;
            }
            else
            {
                _locationInitialized = true;
            }

            if (Location == ELocation.None)
            {
                string location = GameWorld.LocationId;
                switch (location.ToLower())
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

                if (Location != ELocation.None)
                {
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                    Logger.LogDebug(Location);
                }
            }
        }

        public ELocation Location { get; private set; }

        private void OnDestroy()
        {
            try
            {
                ComponentHelpers.DestroyComponent(SAINBotController);
                ComponentHelpers.DestroyComponent(SAINMainPlayer);
            }
            catch
            {
                Logger.LogError("Dispose Component Error");
            }
        }

        public Player FindClosestPlayer(out float closestPlayerSqrMag, Vector3 targetPosition)
        {
            var players = GameWorld?.AllAlivePlayersList;

            Player closestPlayer = null;
            closestPlayerSqrMag = float.MaxValue;

            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player != null && player.AIData?.IsAI == false)
                    {
                        float sqrMag = (player.Position - targetPosition).sqrMagnitude;
                        if (sqrMag < closestPlayerSqrMag)
                        {
                            closestPlayer = player;
                            closestPlayerSqrMag = sqrMag;
                        }
                    }
                }
            }
            return closestPlayer;
        }

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

        public GameWorld GameWorld => Singleton<GameWorld>.Instance;
        public SAINMainPlayerComponent SAINMainPlayer { get; private set; }
        public SAINBotControllerComponent SAINBotController { get; private set; }
        public Extract.ExtractFinderComponent ExtractFinder { get; private set; }
        public SpawnPointMarker[] SpawnPointMarkers { get; private set; }
    }

}
