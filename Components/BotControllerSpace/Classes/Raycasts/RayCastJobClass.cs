using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components
{
    public struct BotRaycastJob
    {
        public BotComponent Bot;
        public JobHandle Handle;
        public NativeArray<RaycastCommand> Commands;
        public NativeArray<RaycastHit> Hits;

        public void Dispose()
        {
            Commands.Dispose();
            Hits.Dispose();
        }
    }

    public struct BotEnemyRaycastData
    {
        public Enemy Enemy;
        public Vector3[] Points;
        public EBodyPartColliderType[] ColliderTypes;
        public EBodyPart[] BodyParts;
    }

    public struct BotRaycastData
    {
        public BotRaycastJob Job;
        public BotEnemyRaycastData Data;
    }

    public class RayCastJobClass : SAINControllerBase
    {
        public RayCastJobClass(SAINBotController botController) : base(botController)
        {
        }

        public BotRaycastTotalCheck? ScheduleJobs(BotComponent bot)
        {
            Enemy[] enemies = GetEnemies(bot);
            if (enemies == null || enemies.Length == 0) {
                return null;
            }
            BotRaycastTotalCheck result = new BotRaycastTotalCheck {
                Bot = bot,
                Count = enemies.Length,
                LineOfSightChecks = SetupJobsForBot(bot, bot.Transform.EyePosition, enemies, LayerMaskClass.HighPolyWithTerrainMask),
                ShootChecks = SetupJobsForBot(bot, bot.Transform.WeaponFirePort, enemies, LayerMaskClass.HighPolyWithTerrainMask),
                VisibleChecks = SetupJobsForBot(bot, bot.Transform.EyePosition, enemies, LayerMaskClass.AI),
            };
            return result;
        }

        public Enemy[] GetEnemies(BotComponent bot)
        {
            float time = Time.time;
            _enemiesToCheck.Clear();
            var enemies = bot.EnemyController.Enemies;
            foreach (var enemy in enemies.Values) {
                float delay = enemy.IsAI ? 0.1f : 0.05f;
                if (time - enemy.Vision.VisionChecker.LastCheckLOSTime < delay) continue;
                if (!enemy.CheckValid()) continue;
                _enemiesToCheck.Add(enemy);
            }
            int count = _enemiesToCheck.Count;
            if (count == 0) {
                return null;
            }
            return _enemiesToCheck.ToArray();
        }

        public BotRaycastData[] SetupJobsForBot(BotComponent bot, Vector3 origin, Enemy[] enemies, LayerMask mask)
        {
            if (enemies == null) {
                return null;
            }
            int count = enemies.Length;
            BotRaycastData[] datas = new BotRaycastData[count];
            for (int i = 0; i < count; i++) {
                var enemyData = GetEnemyData(enemies[i], origin);

                var job = CreateJob(bot, origin, enemyData.Points, mask);

                datas[i] = new BotRaycastData {
                    Job = job,
                    Data = enemyData,
                };
            }
            return datas;
        }

        private readonly List<Enemy> _enemiesToCheck = new List<Enemy>();

        public BotRaycastJob CreateJob(BotComponent bot, Vector3 origin, Vector3[] targets, LayerMask mask)
        {
            int count = targets.Length;

            var casts = new NativeArray<RaycastCommand>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++) {
                Vector3 target = targets[i];
                Vector3 direction = target - origin;
                casts[i] = new RaycastCommand(origin, direction, direction.magnitude, mask);
            }

            var hits = new NativeArray<RaycastHit>(count, Allocator.TempJob);
            JobHandle handle = RaycastCommand.ScheduleBatch(casts, hits, 5);

            return new BotRaycastJob {
                Bot = bot,
                Handle = handle,
                Commands = casts,
                Hits = hits,
            };
        }

        public BotEnemyRaycastData GetEnemyData(Enemy enemy, Vector3 origin)
        {
            return enemy.Vision.VisionChecker.GetPartsToCheck2(origin);
        }

        private List<Player> Players => GameWorldInfo.AlivePlayers;
        private readonly List<Player> _players = new List<Player>();

        private void raycastCommandAllParts(List<Player> players, List<BotComponent> bots, int partCount)
        {
        }

        private void setSpherecastTargetsHuman(List<BotComponent> botList, List<Player> players, NativeArray<SpherecastCommand> allSpherecastCommands)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++) {
                var bot = botList[i];
                Vector3 head = getHeadPoint(bot);
                float maxVisDist = getMaxVisionDist(bot);

                for (int j = 0; j < _players.Count; j++) {
                    Player player = _players[j];
                    foreach (var part in player.MainParts.Values) {
                        Vector3 target = getTarget(part, head);
                        Vector3 direction = target - head;
                        float maxRange = getMaxRange(bot, player.ProfileId, direction, head, maxVisDist);
                        allSpherecastCommands[total] =
                            new SpherecastCommand(head, SpherecastRadius, direction, maxRange, SightLayer);
                        total++;
                    }
                }
            }
        }

        private float getMaxRange(BotComponent bot, string profileID, Vector3 direction, Vector3 head, float maxVisDist)
        {
            float maxRange = maxVisDist;
            if (head == Vector3.zero || bot == null || bot.ProfileId == profileID) {
                maxRange = 0f;
            }
            else if (bot.EnemyController.Enemies.TryGetValue(profileID, out var enemy)) {
                maxRange += enemy.Vision.VisionDistance;
            }
            maxRange = Mathf.Clamp(maxRange, 0f, direction.magnitude);
            return maxRange;
        }

        private void analyzeHitsHuman(List<BotComponent> botList, List<Player> players, NativeArray<RaycastHit> hits)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++) {
                var bot = botList[i];
                List<string> visiblePlayerIDs = null;
                for (int j = 0; j < players.Count; j++) {
                    Player player = players[j];
                    string profileID = player.ProfileId;

                    bool lineOfSight = false;

                    foreach (var part in player.MainParts.Values) {
                        if (!lineOfSight)
                            lineOfSight = hits[total].collider == null;
                        total++;
                    }

                    if (lineOfSight)
                        visiblePlayerIDs.Add(profileID);

                    //if (bot.EnemyController.Enemies.TryGetValue(profileID, out var enemy))
                    //    enemy.Vision.InLineOfSight = lineOfSight;
                }
            }
        }

        private void checkLineOfSightAI(List<BotComponent> botList)
        {
            getAIPlayers(_players, out _);
            int total = botList.Count * _players.Count;
            if (total > 0) {
                NativeArray<SpherecastCommand> allSpherecastCommands = new NativeArray<SpherecastCommand>(total, Allocator.TempJob);
                setSpherecastTargetsAI(botList, _players, allSpherecastCommands);
                NativeArray<RaycastHit> allRaycastHits = new NativeArray<RaycastHit>(total, Allocator.TempJob);

                SpherecastCommand.ScheduleBatch(allSpherecastCommands, allRaycastHits, MinJobSize).Complete();

                analyzeHitsAI(botList, _players, allRaycastHits);

                allSpherecastCommands.Dispose();
                allRaycastHits.Dispose();
            }

            _players.Clear();
        }

        private float getMaxVisionDist(BotComponent bot)
        {
            float maxVisDist;
            if (bot != null && bot.BotOwner.LookSensor != null) {
                maxVisDist = bot.BotOwner.LookSensor.VisibleDist;
            }
            else {
                maxVisDist = 0f;
            }
            return maxVisDist;
        }

        private void setSpherecastTargetsAI(List<BotComponent> botList, List<Player> players, NativeArray<SpherecastCommand> allSpherecastCommands)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++) {
                var bot = botList[i];
                Vector3 head = getHeadPoint(bot);
                float maxVisDist = getMaxVisionDist(bot);

                for (int j = 0; j < players.Count; j++) {
                    Player player = players[j];
                    Vector3 target = getTarget(player.MainParts[BodyPartType.body], head);
                    Vector3 direction = target - head;
                    float maxRange = getMaxRange(bot, player.ProfileId, direction, head, maxVisDist);
                    allSpherecastCommands[total] = new SpherecastCommand(head, SpherecastRadius, direction.normalized, maxRange, SightLayer);
                    total++;
                }
            }
        }

        private void analyzeHitsAI(List<BotComponent> botList, List<Player> players, NativeArray<RaycastHit> allRaycastHits)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++) {
                BotComponent bot = botList[i];
                string botProfileId = bot.ProfileId;

                List<string> visPlayerIds = null;
                visPlayerIds?.Clear();

                for (int j = 0; j < players.Count; j++) {
                    Player player = players[j];
                    string profileId = player.ProfileId;
                    if (botProfileId != profileId) {
                        bool lineOfSight =
                            allRaycastHits[total].collider == null;

                        if (lineOfSight)
                            visPlayerIds.Add(profileId);

                        //if (bot.EnemyController.Enemies.TryGetValue(profileId, out var enemy))
                        //    enemy.Vision.InLineOfSight = lineOfSight;
                    }
                    total++;
                }
            }
        }

        private void getHumanPlayers(List<Player> playerList, out int partCount)
        {
            getPlayers(playerList, false, out partCount);
        }

        private void getAIPlayers(List<Player> playerList, out int partCount)
        {
            getPlayers(playerList, true, out partCount);
        }

        private void getPlayers(List<Player> playerList, bool forAI, out int partCount)
        {
            partCount = 0;
            var players = Players;
            if (players == null)
                return;

            foreach (var player in players) {
                if (!shallCheckPlayer(player))
                    continue;

                if (player.IsAI != forAI)
                    continue;

                if (partCount == 0)
                    partCount = player.MainParts.Count;

                playerList.Add(player);
            }
        }

        private bool shallCheckPlayer(Player player)
        {
            if (player == null ||
                player.Transform == null ||
                !player.gameObject.activeInHierarchy) {
                return false;
            }
            if (player.IsAI &&
                player.AIData.BotOwner?.BotState != EBotState.Active) {
                return false;
            }
            return player.HealthController.IsAlive;
        }

        private Vector3 getHeadPoint(BotComponent bot)
        {
            if (bot == null) {
                //Logger.LogError("BotComponent Null");
                return Vector3.zero;
            }
            var botOwner = bot?.BotOwner;
            if (botOwner == null) {
                //Logger.LogError("botOwner Null");
                return Vector3.zero;
            }
            var lookSensor = botOwner?.LookSensor;
            if (lookSensor == null) {
                //Logger.LogError("lookSensor Null");
                return Vector3.zero;
            }
            Vector3 head = lookSensor != null ? lookSensor._headPoint : Vector3.zero;
            return head;
        }

        private Vector3 getTarget(EnemyPart part, Vector3 head)
        {
            Vector3 target;
            if (part.Collider != null) {
                target = part.Collider.GetRandomPointToCastLocal(head);
            }
            else {
                target = part.Position;
            }
            return target;
        }

        private float SpherecastRadius = 0.025f;
        private LayerMask SightLayer => LayerMaskClass.HighPolyWithTerrainMask;
        private int MinJobSize = 2;
    }
}