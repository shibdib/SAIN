using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using SAIN.Components.BotController;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.Components
{
    public class PlayerTracker
    {
        private SAINGameworldComponent SAINGameWorld;
        private GameWorld GameWorld => Singleton<GameWorld>.Instance;

        public PlayerComponent FindClosestHumanPlayer(out float closestPlayerSqrMag, Vector3 targetPosition, out Player player)
        {
            PlayerComponent closestPlayer = null;
            closestPlayerSqrMag = float.MaxValue;
            player = null;

            foreach (var component in AlivePlayers.Values)
            {
                if (component != null &&
                    component.Player != null &&
                    !component.IsAI)
                {
                    float sqrMag = (component.Position - targetPosition).sqrMagnitude;
                    if (sqrMag < closestPlayerSqrMag)
                    {
                        player = component.Player;
                        closestPlayer = component;
                        closestPlayerSqrMag = sqrMag;
                    }
                }
            }
            return closestPlayer;
        }

        public Player FindClosestHumanPlayer(out float closestPlayerSqrMag, Vector3 targetPosition)
        {
            FindClosestHumanPlayer(out closestPlayerSqrMag, targetPosition, out Player player);
            return player;
        }

        public PlayerTracker(SAINGameworldComponent component)
        {
            SAINGameWorld = component;
            GameWorld.OnPersonAdd += addPlayer;
        }

        public void Dispose()
        {
            var gameWorld = GameWorld;
            if (gameWorld != null)
            {
                gameWorld.OnPersonAdd -= addPlayer;
            }
        }

        public bool CheckAddPlayer(IPlayer player)
        {
            if (player != null)
            {
                if (AlivePlayers.ContainsKey(player.ProfileId))
                {
                    return false;
                }
            }
            return true;
        }

        public readonly Dictionary<string, PlayerComponent> AlivePlayers = new Dictionary<string, PlayerComponent>();

        public readonly Dictionary<string, Player> DeadPlayers = new Dictionary<string, Player>();

        private void addPlayer(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                Logger.LogError($"Could not add PlayerComponent for Null IPlayer.");
                return;
            }

            string id = iPlayer.ProfileId;
            Player player = GetPlayer(iPlayer);
            if (player == null)
            {
                Logger.LogError($"Could not add PlayerComponent for Null Player. IPlayer: {iPlayer.Profile?.Nickname} : {id}");
                return;
            }

            player.OnPlayerDeadOrUnspawn += clearPlayer;

            if (clearPlayer(id, out bool compDestroyed))
            {
                string playerInfo = $"{player.name} : {player.Profile?.Nickname} : {id}";
                Logger.LogWarning($"PlayerComponent already exists for Player: {playerInfo}");
                if (compDestroyed)
                {
                    Logger.LogWarning($"Destroyed old Component for: {playerInfo}");
                }
            }

            PlayerComponent component = player.GetOrAddComponent<PlayerComponent>();
            if (component?.Init(iPlayer, player) == true)
            {
                AlivePlayers.Add(id, component);
            }
            else
            {
                Logger.LogError($"Init PlayerComponent Failed for {player.name} : {player.ProfileId}");
                GameObject.Destroy(component);
            }
        }

        public PlayerComponent GetPlayerComponent(string profileId)
        {
            if (profileId.IsNullOrEmpty())
            {
                return null;
            }
            if (AlivePlayers.TryGetValue(profileId, out PlayerComponent component))
            {
                return component;
            }
            return null;
        }

        public PlayerComponent GetPlayerComponent(IPlayer iPlayer)
        {
            return GetPlayerComponent(iPlayer?.ProfileId);
        }

        public PlayerComponent GetPlayerComponent(Player player)
        {
            return GetPlayerComponent(player?.ProfileId);
        }

        public Player GetPlayer(IPlayer iPlayer)
        {
            Player player = null;
            if (iPlayer is Player)
            {
                player = (Player)iPlayer;
                if (player != null)
                {
                    Logger.LogInfo("Got player from cast");
                }
            }
            else
            {
                Logger.LogInfo("failed to get player from cast");
            }

            return player ?? GetPlayer(iPlayer?.ProfileId);
        }

        public Player GetPlayer(string profileId)
        {
            if (profileId.IsNullOrEmpty())
            {
                return null;
            }
            return GameWorldInfo.GetAlivePlayer(profileId);
        }

        private void clearPlayer(Player player)
        {
            clearPlayer(player?.ProfileId, out _);
            SAINGameWorld.StartCoroutine(addDeadPlayer(player));
        }

        private IEnumerator addDeadPlayer(Player player)
        {
            yield return null;

            if (player != null)
            {
                if (DeadPlayers.Count > _maxDeadTracked)
                {
                    DeadPlayers.Remove(DeadPlayers.First().Key);
                }
                DeadPlayers.Add(player.ProfileId, player);
            }
            clearNullPlayers();
        }

        private bool clearPlayer(string id, out bool destroyedComponent)
        {
            destroyedComponent = false;
            if (id.IsNullOrEmpty())
            {
                return false;
            }
            return removeId(id, out destroyedComponent);
        }

        private bool removeId(string id, out bool destroyedComponent)
        {
            destroyedComponent = false;
            if (AlivePlayers.TryGetValue(id, out PlayerComponent playerComponent))
            {
                if (playerComponent != null)
                {
                    destroyedComponent = true;
                    GameObject.Destroy(playerComponent);
                }
                AlivePlayers.Remove(id);
                return true;
            }
            return false;
        }

        private void clearNullPlayers()
        {
            foreach (var player in AlivePlayers)
            {
                PlayerComponent component = player.Value;
                if (component == null || 
                    component.IPlayer == null || 
                    component.Player == null)
                {
                    _ids.Add(player.Key);
                }
            }
            foreach (var id in _ids)
            {
                removeId(id, out _);
            }
            _ids.Clear();
        }

        private readonly List<string> _ids = new List<string>();
        private const int _maxDeadTracked = 30;
    }

    public class SAINGameworldComponent : MonoBehaviour
    {
        public static SAINGameworldComponent Instance { get; private set; }
        public PlayerTracker PlayerTracker { get; private set; }


        private void Awake()
        {
            Instance = this;
            GameWorld.OnDispose += dispose;
            GameWorld = Singleton<GameWorld>.Instance;
            PlayerTracker = new PlayerTracker(this);
            SAINBotController = this.GetOrAddComponent<SAINBotController>();
            ExtractFinder = this.GetOrAddComponent<Extract.ExtractFinderComponent>();
            SAINMainPlayer = ComponentHelpers.AddOrDestroyComponent(SAINMainPlayer, GameWorld?.MainPlayer);
        }

        private void Update()
        {
            findLocation();
            findSpawnPointMarkers();
        }

        private void findLocation()
        {
            if (Location == ELocation.None)
            {
                Location = GameWorldHandler.FindLocation();
            }
        }

        public ELocation Location { get; private set; }

        private void dispose()
        {
            GameWorld.OnDispose -= dispose;

            try
            {
                PlayerTracker.Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose GameWorld Component Class Error: {e}");
            }

            try
            {
                ComponentHelpers.DestroyComponent(SAINBotController);
                ComponentHelpers.DestroyComponent(SAINMainPlayer);
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose GameWorld SubComponent Error: {e}");
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

        public GameWorld GameWorld { get; private set; }
        public SAINMainPlayerComponent SAINMainPlayer { get; private set; }
        public SAINBotController SAINBotController { get; private set; }
        public Extract.ExtractFinderComponent ExtractFinder { get; private set; }
        public SpawnPointMarker[] SpawnPointMarkers { get; private set; }
    }
}