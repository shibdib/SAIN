using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Interpolation;
using SAIN.BotController.Classes;
using SAIN.Components.BotController;
using SAIN.Helpers;
using SAIN.Layers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Enemy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

namespace SAIN.Components
{
    public class SAINBotController : MonoBehaviour
    {
        public Action<SAINSoundType, Vector3, Player, float> AISoundPlayed { get; set; }
        public Action<EPhraseTrigger, ETagStatus, Player> PlayerTalk { get; set; }
        public Action<Vector3> BulletImpact { get; set; }

        public Dictionary<string, BotComponent> Bots => BotSpawnController.Bots;
        public GameWorld GameWorld => Singleton<GameWorld>.Instance;
        public IBotGame BotGame => Singleton<IBotGame>.Instance;
        public Player MainPlayer => Singleton<GameWorld>.Instance?.MainPlayer;

        public BotsController DefaultController { get; set; }
        public BotSpawner BotSpawner { get; set; }
        public CoverManager CoverManager { get; private set; } = new CoverManager();
        public LineOfSightManager LineOfSightManager { get; private set; } = new LineOfSightManager();
        public BotExtractManager BotExtractManager { get; private set; } = new BotExtractManager();
        public TimeClass TimeVision { get; private set; } = new TimeClass();
        public WeatherVisionClass WeatherVision { get; private set; } = new WeatherVisionClass();
        public BotSpawnController BotSpawnController { get; private set; } = new BotSpawnController();
        public BotSquads BotSquads { get; private set; } = new BotSquads();

        private void Awake()
        {
            GameWorld.OnDispose += Dispose;

            BotSpawnController.Awake();
            TimeVision.Awake();
            LineOfSightManager.Awake();
            CoverManager.Awake();
            PathManager.Awake();
            BotExtractManager.Awake();
            BotSquads.Awake();

            Singleton<BotEventHandler>.Instance.OnGrenadeThrow += GrenadeThrown;
            Singleton<BotEventHandler>.Instance.OnGrenadeExplosive += GrenadeExplosion;
            PlayerTalk += playerTalked;
        }

        public void PlayAISound(Player player, SAINSoundType soundType, Vector3 position, float power)
        {
            if (player != null && 
                player.HealthController?.IsAlive == true)
            {
                if (player.IsAI && player.AIData.BotOwner?.BotState != EBotState.Active)
                {
                    return;
                }
                string id = player.ProfileId;
                if (!_playerSoundPlayers.ContainsKey(id))
                {
                    _playerSoundPlayers.Add(id, new PlayerSoundPlayer(player.IsAI));
                    player.OnPlayerDeadOrUnspawn += removePlayerSoundPlayer;
                }
                if (_playerSoundPlayers.TryGetValue(id, out var soundPlayer) && 
                    soundPlayer.ShallPlayAISound(power))
                {
                    playSound(soundType, position, player, power);
                }
            }
        }

        private void removePlayerSoundPlayer(Player player)
        {
            if (player != null)
            {
                player.OnPlayerDeadOrUnspawn -= removePlayerSoundPlayer;
                _playerSoundPlayers.Remove(player.ProfileId);
            }
        }

        private class PlayerSoundPlayer
        {
            public PlayerSoundPlayer(bool isAI)
            {
                if (isAI)
                {
                    _freq = 0.5f;
                }
                else
                {
                    _freq = 0.1f;
                }
            }

            private readonly float _freq;
            private float _lastSoundPower;
            private float _nextPlaySoundTime;

            public bool ShallPlayAISound(float power)
            {
                if (_nextPlaySoundTime < Time.time || _lastSoundPower > power * 1.25f)
                {
                    _nextPlaySoundTime = Time.time + _freq;
                    _lastSoundPower = power;
                    return true;
                }
                return false;
            }
        }

        private readonly Dictionary<string, PlayerSoundPlayer> _playerSoundPlayers = new Dictionary<string, PlayerSoundPlayer>();

        private void Update()
        {
            if (GameWorld == null)
            {
                Dispose();
                return;
            }
            if (BotGame == null)
            {
                return;
            }

            BotSquads.Update();
            BotSpawnController.Update();
            BotExtractManager.Update();
            TimeVision.Update();
            WeatherVision.Update();
            LineOfSightManager.Update();

            //showBotInfoDebug();
            //CoverManager.Update();
            //PathManager.Update();
            //AddNavObstacles();
            //UpdateObstacles();
        }

        private void showBotInfoDebug()
        {
            foreach (var bot in Bots.Values)
            {
                if (bot != null && !_debugObjects.ContainsKey(bot))
                {
                    GUIObject obj = DebugGizmos.CreateLabel(bot.Position, "");
                    _debugObjects.Add(bot, obj);
                }
            }
            foreach (var obj in _debugObjects)
            {
                if (obj.Value != null)
                {
                    obj.Value.WorldPos = obj.Key.Position;
                    obj.Value.StringBuilder.Clear();
                    DebugOverlay.AddBaseInfo(obj.Key, obj.Key.BotOwner, obj.Value.StringBuilder);
                }
            }
        }

        private readonly Dictionary<BotComponent, GUIObject> _debugObjects = new Dictionary<BotComponent, GUIObject>();

        private IEnumerator playShootSoundCoroutine(Player player)
        {
            yield return null;
            if (player != null && player.HealthController?.IsAlive == true)
                AudioHelpers.TryPlayShootSound(player);
        }

        public void PlayShootSound(Player player)
        {
            StartCoroutine(playShootSoundCoroutine(player));
        }

        private Vector3 randomizePos(Vector3 position, float distance, float dispersionFactor = 20f)
        {
            float disp = distance / dispersionFactor;
            Vector3 random = UnityEngine.Random.insideUnitSphere * disp;
            random.y = 0;
            return position + random;
        }

        private void playerTalked(EPhraseTrigger phrase, ETagStatus mask, Player player)
        {
            if (player == null || Bots == null)
            {
                return;
            }

            if (phrase == EPhraseTrigger.OnDeath)
            {
                return;
            }

            bool isPain = phrase == EPhraseTrigger.OnAgony || phrase == EPhraseTrigger.OnBeingHurt;
            float painRange = 50f;
            float breathRange = player.HeavyBreath ? 50f : 25f;

            foreach (var bot in Bots)
            {
                BotComponent sain = bot.Value;
                if (IsBotActive(bot.Value) &&
                    bot.Key != player.ProfileId)
                {
                    if (!sain.EnemyController.IsPlayerFriendly(player))
                    {
                        if (isPain)
                        {
                            SAINEnemy enemy = sain.EnemyController.CheckAddEnemy(player);
                            if (enemy != null && enemy.RealDistance <= painRange)
                            {
                                Vector3 randomizedPos = randomizePos(player.Position, enemy.RealDistance, 20f);
                                enemy.SetHeardStatus(true, randomizedPos, SAINSoundType.Pain, true);
                            }
                            continue;
                        }
                        if (phrase == EPhraseTrigger.OnBreath)
                        {
                            SAINEnemy enemy = sain.EnemyController.CheckAddEnemy(player);
                            if (enemy != null && enemy.RealDistance <= breathRange)
                            {
                                Vector3 randomizedPos = randomizePos(player.Position, enemy.RealDistance, 20f);
                                enemy.SetHeardStatus(true, randomizedPos, SAINSoundType.Breathing, true);
                            }
                            continue;
                        }
                        sain.Talk.EnemyTalk.SetEnemyTalk(player);
                    }
                    else if (!isPain && phrase != EPhraseTrigger.OnBreath)
                    {
                        sain.Talk.EnemyTalk.SetFriendlyTalked(player);
                    }
                }
            }
        }

        public void BotDeath(BotOwner bot)
        {
            if (bot?.GetPlayer != null && bot.IsDead)
            {
                DeadBots.Add(bot.GetPlayer);
            }
        }

        public List<Player> DeadBots { get; private set; } = new List<Player>();
        public List<BotDeathObject> DeathObstacles { get; private set; } = new List<BotDeathObject>();

        private readonly List<int> IndexToRemove = new List<int>();

        public void AddNavObstacles()
        {
            if (DeadBots.Count > 0)
            {
                const float ObstacleRadius = 1.5f;

                for (int i = 0; i < DeadBots.Count; i++)
                {
                    var bot = DeadBots[i];
                    if (bot == null || bot.GetPlayer == null)
                    {
                        IndexToRemove.Add(i);
                        continue;
                    }
                    bool enableObstacle = true;
                    Collider[] players = Physics.OverlapSphere(bot.Position, ObstacleRadius, LayerMaskClass.PlayerMask);
                    foreach (var p in players)
                    {
                        if (p == null) continue;
                        if (p.TryGetComponent<Player>(out var player))
                        {
                            if (player.IsAI && player.HealthController.IsAlive)
                            {
                                enableObstacle = false;
                                break;
                            }
                        }
                    }
                    if (enableObstacle)
                    {
                        if (bot != null && bot.GetPlayer != null)
                        {
                            var obstacle = new BotDeathObject(bot);
                            obstacle.Activate(ObstacleRadius);
                            DeathObstacles.Add(obstacle);
                        }
                        IndexToRemove.Add(i);
                    }
                }

                foreach (var index in IndexToRemove)
                {
                    DeadBots.RemoveAt(index);
                }

                IndexToRemove.Clear();
            }
        }

        private void UpdateObstacles()
        {
            if (DeathObstacles.Count > 0)
            {
                for (int i = 0; i < DeathObstacles.Count; i++)
                {
                    var obstacle = DeathObstacles[i];
                    if (obstacle?.TimeSinceCreated > 30f)
                    {
                        obstacle?.Dispose();
                        IndexToRemove.Add(i);
                    }
                }

                foreach (var index in IndexToRemove)
                {
                    DeathObstacles.RemoveAt(index);
                }

                IndexToRemove.Clear();
            }
        }

        public void playSound(SAINSoundType soundType, Vector3 position, Player player, float range)
        {
            if (Bots.Count == 0 || player == null)
            {
                return;
            }

            AISoundType baseSoundType;
            switch (soundType)
            {
                case SAINSoundType.Gunshot:
                    baseSoundType = AISoundType.gun;
                    break;

                case SAINSoundType.SuppressedGunShot:
                    baseSoundType = AISoundType.silencedGun;
                    break;

                default:
                    baseSoundType = AISoundType.step; 
                    break;
            }

            if (BotEventHandler == null)
            {
                BotEventHandler = Singleton<BotEventHandler>.Instance;
            }

            BotEventHandler?.PlaySound(player, player.Position, range, baseSoundType);
            AISoundPlayed?.Invoke(soundType, position, player, range);
            if (baseSoundType == AISoundType.step)
            {
                StartCoroutine(delayHearAction(player, range, soundType));
            }
        }

        private BotEventHandler BotEventHandler;

        private IEnumerator delayHearAction(Player player, float range, SAINSoundType soundType, float delay = 0.25f)
        {
            yield return new WaitForSeconds(delay);

            if (player == null || !player.HealthController.IsAlive)
            {
                yield break;
            }

            foreach (var bot in Bots.Values)
            {
                if (IsBotActive(bot) && 
                    player.ProfileId != bot.Player.ProfileId)
                {
                    SAINEnemy Enemy = bot.EnemyController.GetEnemy(player.ProfileId);
                    if (Enemy?.EnemyPerson.IsActive == true
                        && Enemy.IsValid
                        && Enemy.RealDistance <= range)
                    {
                        float chance2Hear = chanceToHearAction(Enemy, range, Enemy.RealDistance);
                        if (chance2Hear == 0 || 
                            !EFTMath.RandomBool(chance2Hear))
                        {
                            continue;
                        }

                        bool shallUpdateSquad = true;
                        if (soundType == SAINSoundType.GrenadePin || soundType == SAINSoundType.GrenadeDraw)
                        {
                            Enemy.EnemyStatus.EnemyHasGrenadeOut = true;
                        }
                        else if (soundType == SAINSoundType.Reload || soundType == SAINSoundType.DryFire)
                        {
                            Enemy.EnemyStatus.EnemyIsReloading = true;
                        }
                        else if (soundType == SAINSoundType.Looting)
                        {
                            Enemy.EnemyStatus.EnemyIsLooting = true;
                        }
                        else if (soundType == SAINSoundType.Heal)
                        {
                            Enemy.EnemyStatus.EnemyIsHealing = true;
                        }
                        else if (soundType == SAINSoundType.Surgery)
                        {
                            Enemy.EnemyStatus.VulnerableAction = SAINComponent.Classes.Enemy.EEnemyAction.UsingSurgery;
                        }
                        else if (soundType == SAINSoundType.Gunshot 
                            || soundType == SAINSoundType.SuppressedGunShot 
                            || soundType == SAINSoundType.FootStep)
                        {
                            shallUpdateSquad = false;
                        }

                        if (shallUpdateSquad)
                        {
                            float dispersion = Enemy.RealDistance / 20f;
                            Vector3 random = UnityEngine.Random.insideUnitSphere * dispersion;
                            random.y = 0f;
                            Enemy.SetHeardStatus(true, Enemy.EnemyPosition + random, soundType, false);
                            bot.Squad.SquadInfo.UpdateSharedEnemyStatus(Enemy.EnemyIPlayer, Enemy.EnemyStatus.VulnerableAction, bot, soundType, Enemy.EnemyPosition + random);
                        }
                    }
                }
            }
        }

        private float chanceToHearAction(SAINEnemy enemy, float range, float distance)
        {
            var hearSettings = SAINPlugin.LoadedPreset.GlobalSettings.Hearing;
            float max = enemy.SAINBot.Equipment.HasEarPiece ? hearSettings.MaxFootstepAudioDistance : hearSettings.MaxFootstepAudioDistanceNoHeadphones;
            if (distance > max)
            {
                return 0f;
            }
            float min = 15f;
            if (distance < min)
            {
                return 100f;
            }
            float num = max - min;
            float num2 = distance - min;
            float ratio = 1f - num2 / num;
            return ratio * 100f;
        }

        private void GrenadeExplosion(Vector3 explosionPosition, string playerProfileID, bool isSmoke, float smokeRadius, float smokeLifeTime)
        {
            if (!Singleton<BotEventHandler>.Instantiated || playerProfileID == null)
            {
                return;
            }
            Player player = EFTInfo.GetPlayer(playerProfileID);
            if (player != null)
            {
                if (!isSmoke)
                {
                    registerGrenadeExplosionForSAINBots(explosionPosition, player, playerProfileID, 200f);
                }
                else
                {
                    registerGrenadeExplosionForSAINBots(explosionPosition, player, playerProfileID, 50f);

                    float radius = smokeRadius * HelpersGClass.SMOKE_GRENADE_RADIUS_COEF;
                    Vector3 position = player.Position;
                    foreach (var keyValuePair in DefaultController.Groups())
                    {
                        foreach (BotsGroup botGroupClass in keyValuePair.Value.GetGroups(true))
                        {
                            botGroupClass.AddSmokePlace(explosionPosition, smokeLifeTime, radius, position);
                        }
                    }
                }
            }
        }

        private void registerGrenadeExplosionForSAINBots(Vector3 explosionPosition, Player player, string playerProfileID, float range)
        {
            // Play a sound with the input range.
            Singleton<BotEventHandler>.Instance?.PlaySound(player, explosionPosition, range, AISoundType.gun);

            // We dont want bots to think the grenade explosion was a place they heard an enemy, so set this manually.
            foreach (var bot in Bots.Values)
            {
                if (IsBotActive(bot))
                {
                    float distance = (bot.Position - explosionPosition).magnitude;
                    if (distance < range)
                    {
                        SAINEnemy enemy = bot.EnemyController.GetEnemy(playerProfileID);
                        if (enemy != null)
                        {
                            float dispersion = distance / 10f;
                            Vector3 random = UnityEngine.Random.onUnitSphere * dispersion;
                            random.y = 0;
                            Vector3 estimatedThrowPosition = enemy.EnemyPosition + random;
                            enemy.SetHeardStatus(true, estimatedThrowPosition, SAINSoundType.GrenadeExplosion, true);
                        }
                    }
                }
            }
        }

        private void GrenadeThrown(Grenade grenade, Vector3 position, Vector3 force, float mass)
        {
            if (grenade != null)
            {
                StartCoroutine(grenadeThrown(grenade, position, force, mass));
            }
        }

        public static bool IsBotActive(BotComponent bot)
        {
            return IsBotActive(bot?.BotOwner);
        }

        public static bool IsBotActive(Player player)
        {
            if (player?.IsAI == true)
            {
                return IsBotActive(player.AIData.BotOwner);
            }
            return true;
        }

        public static bool IsBotActive(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                return false;
            }
            if (botOwner.BotState != EBotState.Active ||
                    botOwner.StandBy.StandByType != BotStandByType.active)
            {
                return false;
            }
            return true;
        }

        private IEnumerator grenadeThrown(Grenade grenade, Vector3 position, Vector3 force, float mass)
        {
            var danger = Vector.DangerPoint(position, force, mass);
            yield return null;
            Player player = EFTInfo.GetPlayer(grenade.ProfileId);
            if (player == null)
            {
                Logger.LogError($"Player Null from ID {grenade.ProfileId}");
                yield break;
            }
            if (player.HealthController.IsAlive)
            {
                foreach (var bot in Bots.Values)
                {
                    if (IsBotActive(bot) &&
                        !bot.EnemyController.IsPlayerFriendly(player) &&
                        (danger - bot.Position).sqrMagnitude < 100f * 100f)
                    {
                        bot.Grenade.EnemyGrenadeThrown(grenade, danger);
                    }
                }
            }
            yield return null;
        }

        public List<string> Groups = new List<string>();
        public PathManager PathManager { get; private set; } = new PathManager();

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                LineOfSightManager.Dispose();

                StopAllCoroutines();

                GameWorld.OnDispose -= Dispose;

                Patches.Vision.AIVisionUpdateLimitPatch.LookUpdates.Clear();

                PlayerTalk -= playerTalked;

                Singleton<BotEventHandler>.Instance.OnGrenadeThrow -= GrenadeThrown;
                Singleton<BotEventHandler>.Instance.OnGrenadeExplosive -= GrenadeExplosion;

                if (Bots.Count > 0)
                {
                    foreach (var bot in Bots)
                    {
                        bot.Value?.Dispose();
                    }
                }

                Bots.Clear();
                Destroy(this);
            }
            catch { }
        }

        public bool GetSAIN(string botName, out BotComponent bot)
        {
            StringBuilder debugString = null;
            bot = BotSpawnController.GetSAIN(botName, debugString);
            return bot != null;
        }

        public bool GetSAIN(BotOwner botOwner, out BotComponent bot)
        {
            StringBuilder debugString = null;
            bot = BotSpawnController.GetSAIN(botOwner, debugString);
            return bot != null;
        }

        public bool GetSAIN(Player player, out BotComponent bot)
        {
            StringBuilder debugString = null;
            bot = BotSpawnController.GetSAIN(player, debugString);
            return bot != null;
        }
    }

    public class BotDeathObject
    {
        public BotDeathObject(Player player)
        {
            Player = player;
            NavMeshObstacle = player.gameObject.AddComponent<NavMeshObstacle>();
            NavMeshObstacle.carving = false;
            NavMeshObstacle.enabled = false;
            Position = player.Position;
            TimeCreated = Time.time;
        }

        public void Activate(float radius = 2f)
        {
            if (NavMeshObstacle != null)
            {
                NavMeshObstacle.enabled = true;
                NavMeshObstacle.carving = true;
                NavMeshObstacle.radius = radius;
            }
        }

        public void Dispose()
        {
            if (NavMeshObstacle != null)
            {
                NavMeshObstacle.carving = false;
                NavMeshObstacle.enabled = false;
                GameObject.Destroy(NavMeshObstacle);
            }
        }

        public NavMeshObstacle NavMeshObstacle { get; private set; }
        public Player Player { get; private set; }
        public Vector3 Position { get; private set; }
        public float TimeCreated { get; private set; }
        public float TimeSinceCreated => Time.time - TimeCreated;
        public bool ObstacleActive => NavMeshObstacle.carving;
    }
}