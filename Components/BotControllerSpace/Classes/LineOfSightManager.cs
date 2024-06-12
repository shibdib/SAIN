using BepInEx.Logging;
using Comfort.Common;
using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using System.Collections;
using SAIN.Plugin;
using UnityEngine.UIElements;
using SAIN.SAINComponent.Classes.Enemy;
using System.Diagnostics;
using SAIN.Preset.GlobalSettings.Categories;

namespace SAIN.Components
{
    public class LineOfSightManager : SAINControl
    {
        public LineOfSightManager(SAINBotController botController) : base(botController)
        {
            PresetHandler.OnPresetUpdated += updateSettings;
            updateSettings();
        }

        private void updateSettings()
        {
            MinJobSize = Mathf.RoundToInt(SAINPlugin.LoadedPreset.GlobalSettings.Performance.MinJobSize);
            SpherecastRadius = SAINPlugin.LoadedPreset.GlobalSettings.Performance.SpherecastRadius;
            maxBotsPerFrame = Mathf.RoundToInt(SAINPlugin.LoadedPreset.GlobalSettings.Performance.MaxBotsToCheckVisionPerFrame);
        }

        public void Update()
        {
            //if (_jobCoroutine == null)
            //{
            //    _jobCoroutine = BotController.StartCoroutine(
            //        raycastJobLoop());
            //}
            if (_lookCoroutine == null)
            {
                _lookCoroutine = BotController.StartCoroutine(
                    checkVisionForBots());
            }
        }

        public void Dispose()
        {
            PresetHandler.OnPresetUpdated -= updateSettings;
        }

        private Coroutine _jobCoroutine;
        private Coroutine _lookCoroutine;

        private IEnumerator checkVisionForBots()
        {
            //WaitForSeconds wait = new WaitForSeconds(delay);
            while (true)
            {
                var bots = Bots;
                if (bots != null && bots.Count > 0)
                {
                    getBotList(_localBotList, maxBotsPerFrame);
                    checkVision(_localBotList);
                    _localBotList.Clear();
                }

                yield return null;
            }
        }

        //private static float _nextLogUpdatedTime;
        //private static float _nextLogUpdatedTime2;

        private readonly List<BotComponent> _botsVision = new List<BotComponent>();
        private readonly List<Player> _playersVision = new List<Player>();

        private void getBotList(List<BotComponent> bots, int max)
        {
            int count = 0;
            foreach (var bot in Bots.Values)
            {
                if (bot != null)
                {
                    float timeAdd = bot.BotActive ? 0.1f : 0.5f;
                    if (bot.LastCheckVisibleTime + timeAdd < Time.time)
                    {
                        bot.LastCheckVisibleTime = Time.time;
                        bot.Memory.VisiblePlayersIds.Clear();
                        bots.Add(bot);
                        count++;
                        if (count >= max)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private IEnumerator raycastJobLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);
            while (true)
            {
                var bots = Bots;
                if (bots != null && bots.Count > 0)
                {
                    getBotList(_localBotList, maxBotsPerFrame);
                    checkVision(_localBotList);
                    _localBotList.Clear();
                }
                yield return null;
            }
        }

        private int maxBotsPerFrame = 5;

        private readonly List<BotComponent> _localBotList = new List<BotComponent>();

        private void checkVision(List<BotComponent> botList)
        {
            //checkLineOfSightAI(botList);
            //checkHumanLineOfSight(botList);
            foreach (var bot in botList)
            {
                bot.Vision.BotLook.UpdateLook();
            }
        }

        private readonly List<Player> _players = new List<Player>();

        private void checkHumanLineOfSight(List<BotComponent> bots)
        {
            getHumanPlayers(_players, out int partCount);
            raycastCommandAllParts(_players, bots, partCount);
            _players.Clear();
        }

        private void raycastCommandAllParts(List<Player> players, List<BotComponent> bots, int partCount)
        {
            int total = bots.Count * players.Count * partCount;
            if (total <= 0)
            {
                return;
            }

            var casts = new NativeArray<SpherecastCommand>(total, Allocator.TempJob);
            var hits = new NativeArray<RaycastHit>(total, Allocator.TempJob);

            setSpherecastTargetsHuman(bots, players, casts);
            SpherecastCommand.ScheduleBatch(casts, hits, MinJobSize).Complete();
            analyzeHitsHuman(bots, players, hits);

            casts.Dispose();
            hits.Dispose();
        }

        private void setSpherecastTargetsHuman(List<BotComponent> botList, List<Player> players, NativeArray<SpherecastCommand> allSpherecastCommands)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];
                Vector3 head = getHeadPoint(bot);
                float maxVisDist = getMaxVisionDist(bot);

                for (int j = 0; j < _players.Count; j++)
                {
                    Player player = _players[j];
                    foreach (var part in player.MainParts.Values)
                    {
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
            if (head == Vector3.zero || bot == null || bot.ProfileId == profileID)
            {
                maxRange = 0f;
            }
            else if (bot.EnemyController.Enemies.TryGetValue(profileID, out var enemy))
            {
                maxRange += enemy.Vision.VisionDistance;
            }
            maxRange = Mathf.Clamp(maxRange, 0f, direction.magnitude);
            return maxRange;
        }

        private void analyzeHitsHuman(List<BotComponent> botList, List<Player> players, NativeArray<RaycastHit> hits)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];
                List<string> visiblePlayerIDs = bot.Memory.VisiblePlayersIds;
                for (int j = 0; j < players.Count; j++)
                {
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
            if (total > 0)
            {
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
            if (bot != null && bot.BotOwner.LookSensor != null)
            {
                maxVisDist = bot.BotOwner.LookSensor.VisibleDist;
            }
            else
            {
                maxVisDist = 0f;
            }
            return maxVisDist;
        }

        private void setSpherecastTargetsAI(List<BotComponent> botList, List<Player> players, NativeArray<SpherecastCommand> allSpherecastCommands)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];
                Vector3 head = getHeadPoint(bot);
                float maxVisDist = getMaxVisionDist(bot);

                for (int j = 0; j < players.Count; j++)
                {
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
            for (int i = 0; i < botList.Count; i++)
            {
                BotComponent bot = botList[i];
                string botProfileId = bot.ProfileId;

                var visPlayerIds = bot.Memory.VisiblePlayersIds;
                visPlayerIds?.Clear();

                for (int j = 0; j < players.Count; j++)
                {
                    Player player = players[j];
                    string profileId = player.ProfileId;
                    if (botProfileId != profileId)
                    {
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

        private void getHumanPlayers(List<Player> playerList, out int partCount) {
            getPlayers(playerList, false, out partCount);
        }

        private void getAIPlayers(List<Player> playerList, out int partCount)  {
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
                !player.gameObject.activeInHierarchy)
            {
                return false;
            }
            if (player.IsAI &&
                player.AIData.BotOwner?.BotState != EBotState.Active)
            {
                return false;
            }
            return player.HealthController.IsAlive;
        }

        private Vector3 getHeadPoint(BotComponent bot)
        {
            if (bot == null)
            {
                //Logger.LogError("BotComponent Null");
                return Vector3.zero;
            }
            var botOwner = bot?.BotOwner;
            if (botOwner == null)
            {
                //Logger.LogError("botOwner Null");
                return Vector3.zero;
            }
            var lookSensor = botOwner?.LookSensor;
            if (lookSensor == null)
            {
                //Logger.LogError("lookSensor Null");
                return Vector3.zero;
            }
            Vector3 head = lookSensor != null ? lookSensor._headPoint : Vector3.zero;
            return head;
        }

        private Vector3 getTarget(EnemyPart part, Vector3 head)
        {
            Vector3 target;
            if (part.Collider != null)
            {
                target = part.Collider.GetRandomPointToCastLocal(head);
            }
            else
            {
                target = part.Position;
            }
            return target;
        }

        private float SpherecastRadius = 0.025f;
        private LayerMask SightLayer => LayerMaskClass.HighPolyWithTerrainMask;
        private int MinJobSize = 2;
        private List<Player> Players => GameWorldInfo.AlivePlayers;
    }
}