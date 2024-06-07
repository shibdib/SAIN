using EFT;
using SAIN.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerSpawnTracker
    {
        public readonly PlayerDictionary AlivePlayers = new PlayerDictionary();

        public readonly Dictionary<string, Player> DeadPlayers = new Dictionary<string, Player>();


        public PlayerComponent GetPlayerComponent(string profileId) => AlivePlayers.GetPlayerComponent(profileId);

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

        private void addPlayer(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                Logger.LogError($"Could not add PlayerComponent for Null IPlayer.");
                return;
            }

            string id = iPlayer.ProfileId;
            Player player = GetPlayer(id);
            if (player == null)
            {
                Logger.LogError($"Could not add PlayerComponent for Null Player. IPlayer: {iPlayer.Profile?.Nickname} : {id}");
                return;
            }

            if (AlivePlayers.TryRemove(id, out bool compDestroyed))
            {
                string playerInfo = $"{player.name} : {player.Profile?.Nickname} : {id}";
                Logger.LogWarning($"PlayerComponent already exists for Player: {playerInfo}");
                if (compDestroyed)
                {
                    Logger.LogWarning($"Destroyed old Component for: {playerInfo}");
                }
            }

            PlayerComponent component = player.gameObject.AddComponent<PlayerComponent>();
            if (component?.Init(iPlayer, player) == true)
            {
                player.OnPlayerDeadOrUnspawn += clearPlayer;
                AlivePlayers.Add(id, component);
            }
            else
            {
                Logger.LogError($"Init PlayerComponent Failed for {player.name} : {player.ProfileId}");
                Object.Destroy(component);
            }
        }

        public Player GetPlayer(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                Logger.LogWarning("IPlayer is Null");
                return null;
            }

            Player player = GameWorldInfo.GetAlivePlayer(iPlayer.ProfileId);
            if (player != null)
            {
                return player;
            }

            if (iPlayer is Player player2 && player2 != null)
            {
                Logger.LogWarning("Got player from cast");
                return player2;
            }
            else
            {
                Logger.LogError("Failed to get player from cast");
                return null;
            }
        }

        public Player GetPlayer(string profileId)
        {
            if (!profileId.IsNullOrEmpty())
            {
                return GameWorldInfo.GetAlivePlayer(profileId);
            }
            return null;
        }

        private void clearPlayer(Player player)
        {
            if (player == null)
            {
                AlivePlayers.ClearNullPlayers();
                return;
            }
            player.OnPlayerDeadOrUnspawn -= clearPlayer;
            AlivePlayers.TryRemove(player.ProfileId, out _);
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
        }

        public PlayerSpawnTracker(GameWorldComponent component)
        {
            SAINGameWorld = component;
            component.GameWorld.OnPersonAdd += addPlayer;
        }

        public void Dispose()
        {
            var gameWorld = SAINGameWorld?.GameWorld;
            if (gameWorld != null)
            {
                gameWorld.OnPersonAdd -= addPlayer;
            }
        }

        private readonly GameWorldComponent SAINGameWorld;
        private const int _maxDeadTracked = 30;
    }

    public class PlayerDictionary : Dictionary<string, PlayerComponent>
    {
        public PlayerComponent GetPlayerComponent(string profileId)
        {
            if (!profileId.IsNullOrEmpty() && 
                this.TryGetValue(profileId, out PlayerComponent component))
            {
                return component;
            }
            return null;
        }

        public bool TryRemove(string id, out bool destroyedComponent)
        {
            destroyedComponent = false;
            if (id.IsNullOrEmpty())
            {
                return false;
            }
            if (this.TryGetValue(id, out PlayerComponent playerComponent))
            {
                if (playerComponent != null)
                {
                    destroyedComponent = true;
                    Object.Destroy(playerComponent);
                }
                this.Remove(id);
                return true;
            }
            return false;
        }

        public void ClearNullPlayers()
        {
            foreach (KeyValuePair<string, PlayerComponent> kvp in this)
            {
                PlayerComponent component = kvp.Value;
                if (component == null ||
                    component.IPlayer == null ||
                    component.Player == null)
                {
                    _ids.Add(kvp.Key);
                    if (component.IPlayer != null)
                    {
                        Logger.LogWarning($"Removing {component.Player.name} from player dictionary");
                    }
                }
            }
            if (_ids.Count > 0)
            {
                Logger.LogWarning($"Removing {_ids.Count} null players");
                foreach (var id in _ids)
                {
                    TryRemove(id, out _);
                }
                _ids.Clear();
            }
        }

        private readonly List<string> _ids = new List<string>();
    }
}