using Comfort.Common;
using EFT;
using EFT.EnvironmentEffect;
using SAIN.BotController.Classes;
using SAIN.Components.BotController;
using SAIN.Components.BotControllerSpace.Classes;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Layers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components
{
    public class SAINBotController : MonoBehaviour
    {
        public static SAINBotController Instance { get; private set; }

        public Action<SAINSoundType, Vector3, PlayerComponent, float, float> AISoundPlayed { get; set; }
        public Action<EPhraseTrigger, ETagStatus, Player> PlayerTalk { get; set; }
        public Action<EftBulletClass> BulletImpact { get; set; }

        public event Action<Grenade, float> OnGrenadeCollision;

        public Dictionary<string, BotComponent> Bots => BotSpawnController.Bots;
        public GameWorld GameWorld => SAINGameWorld.GameWorld;
        public IBotGame BotGame => Singleton<IBotGame>.Instance;

        public BotEventHandler BotEventHandler
        {
            get
            {
                if (_eventHandler == null)
                {
                    _eventHandler = Singleton<BotEventHandler>.Instance;
                    if (_eventHandler != null)
                    {
                        _eventHandler.OnGrenadeThrow += GrenadeThrown;
                        _eventHandler.OnGrenadeExplosive += GrenadeExplosion;
                    }
                }
                return _eventHandler;
            }
        }

        private BotEventHandler _eventHandler;

        public GameWorldComponent SAINGameWorld { get; private set; }
        public BotsController DefaultController { get; set; }

        public BotSpawner BotSpawner
        {
            get
            {
                return _spawner;
            }
            set
            {
                BotSpawnController.Subscribe(value);
                _spawner = value;
            }
        }

        private BotSpawner _spawner;
        public CoverManager CoverManager { get; private set; }
        public LineOfSightManager LineOfSightManager { get; private set; }
        public BotExtractManager BotExtractManager { get; private set; }
        public TimeClass TimeVision { get; private set; }
        public BotController.SAINWeatherClass WeatherVision { get; private set; }
        public BotSpawnController BotSpawnController { get; private set; }
        public BotSquads BotSquads { get; private set; }
        public BotHearingClass BotHearing { get; private set; }

        public Action<string, IFirearmHandsController> OnBotWeaponChange { get; set; }

        public void BotChangedWeapon(BotOwner botOwner, IFirearmHandsController firearmController)
        {
           // if (botOwner != null)
           //     OnBotWeaponChange?.Invoke(botOwner.name, firearmController);
        }

        public void PlayerEnviromentChanged(string profileID, IndoorTrigger trigger)
        {
            SAINGameWorld.PlayerTracker.GetPlayerComponent(profileID)?.AIData.PlayerLocation.UpdateEnvironment(trigger);
        }

        public void BulletImpacted(EftBulletClass bullet)
        {
            //Logger.LogInfo($"Shot By: {bullet.Player?.iPlayer?.Profile.Nickname} at Time: {Time.time}");
            //DebugGizmos.Sphere(bullet.CurrentPosition);
            BulletImpact?.Invoke(bullet);
        }

        private void Awake()
        {
            Instance = this;
            SAINGameWorld = this.GetComponent<GameWorldComponent>();
            CoverManager = new CoverManager(this);
            LineOfSightManager = new LineOfSightManager(this);
            BotExtractManager = new BotExtractManager(this);
            TimeVision = new TimeClass(this);
            WeatherVision = new BotController.SAINWeatherClass(this);
            BotSpawnController = new BotSpawnController(this);
            BotSquads = new BotSquads(this);
            PathManager = new PathManager(this);
            BotHearing = new BotHearingClass(this);
            GameWorld.OnDispose += Dispose;
        }

        private void Update()
        {
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

        public void GrenadeCollided(Grenade grenade, float maxRange)
        {
            OnGrenadeCollision?.Invoke(grenade, maxRange);
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

        private void GrenadeExplosion(Vector3 explosionPosition, string playerProfileID, bool isSmoke, float smokeRadius, float smokeLifeTime)
        {
            if (!Singleton<BotEventHandler>.Instantiated || playerProfileID == null)
            {
                return;
            }
            Player player = GameWorldInfo.GetAlivePlayer(playerProfileID);
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

                    if (DefaultController != null)
                    {
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
        }

        private void registerGrenadeExplosionForSAINBots(Vector3 explosionPosition, Player player, string playerProfileID, float range)
        {
            // Play a sound with the input range.
            Singleton<BotEventHandler>.Instance?.PlaySound(player, explosionPosition, range, AISoundType.gun);

            // We dont want bots to think the grenade explosion was a place they heard an enemy, so set this manually.
            foreach (var bot in Bots.Values)
            {
                if (bot?.BotActive == true)
                {
                    float distance = (bot.Position - explosionPosition).magnitude;
                    if (distance < range)
                    {
                        Enemy enemy = bot.EnemyController.GetEnemy(playerProfileID, true);
                        if (enemy != null)
                        {
                            float dispersion = distance / 10f;
                            Vector3 random = UnityEngine.Random.onUnitSphere * dispersion;
                            random.y = 0;
                            Vector3 estimatedThrowPosition = enemy.EnemyPosition + random;
                            enemy.Hearing.SetHeard(estimatedThrowPosition, SAINSoundType.GrenadeExplosion, true);
                        }
                    }
                }
            }
        }

        private void GrenadeThrown(Grenade grenade, Vector3 position, Vector3 force, float mass)
        {
            if (grenade == null)
            {
                return;
            }

            Player player = GameWorldInfo.GetAlivePlayer(grenade.ProfileId);
            if (player == null)
            {
                Logger.LogError($"Player Null from ID {grenade.ProfileId}");
                return;
            }
            if (!player.HealthController.IsAlive)
            {
                return;
            }

            Vector3 dangerPoint = Vector.DangerPoint(position, force, mass);
            Singleton<BotEventHandler>.Instance?.PlaySound(player, grenade.transform.position, 30f, AISoundType.gun);

            foreach (var bot in Bots.Values)
            {
                if (bot?.BotActive == true &&
                    bot.EnemyController.IsPlayerAnEnemy(player.ProfileId) &&
                    (dangerPoint - bot.Position).sqrMagnitude < 100f * 100f)
                {
                    bot.Grenade.EnemyGrenadeThrown(grenade, dangerPoint);
                }
            }
        }

        public List<string> Groups = new List<string>();

        public PathManager PathManager { get; private set; }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                GameWorld.OnDispose -= Dispose;
                StopAllCoroutines();
                LineOfSightManager?.Dispose();
                BotSpawnController?.UnSubscribe();

                if (BotEventHandler != null)
                {
                    BotEventHandler.OnGrenadeThrow -= GrenadeThrown;
                    BotEventHandler.OnGrenadeExplosive -= GrenadeExplosion;
                }

                if (Bots != null && Bots.Count > 0)
                {
                    foreach (var bot in Bots.Values)
                    {
                        bot?.Dispose();
                    }
                }

                Bots?.Clear();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose SAIN BotController Error: {ex}");
            }

            Destroy(this);
        }

        public bool GetSAIN(BotOwner botOwner, out BotComponent bot)
        {
            StringBuilder debugString = null;
            bot = BotSpawnController.GetSAIN(botOwner, debugString);
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