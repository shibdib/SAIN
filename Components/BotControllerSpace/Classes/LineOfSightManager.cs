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
            if (_jobCoroutine == null)
            {
                _jobCoroutine = BotController.StartCoroutine(
                    raycastJobLoop());
            }
        }

        public void Dispose()
        {
            PresetHandler.OnPresetUpdated -= updateSettings;
        }

        private Coroutine _jobCoroutine;

        private IEnumerator checkVisionForBots(bool forAI, List<BotComponent> _localList)
        {
            //WaitForSeconds wait = new WaitForSeconds(delay);
            while (true)
            {
                _localList.AddRange(Bots.Values);

                int totalUpdated = 0;
                foreach (var bot in _localList)
                {
                    if (bot != null && bot.BotActive)
                    {
                        int numUpdated = bot.Vision.BotLook.UpdateLook(forAI);
                        totalUpdated += numUpdated;
                        if (numUpdated > 0)
                        {
                            yield return null;
                        }
                        //if (forAI &&
                        //    _nextLogUpdatedTime2 < Time.time)
                        //{
                        //    _nextLogUpdatedTime2 = Time.time + 1f;
                        //    Logger.LogDebug($"Updated Vision for [{numUpdated}] enemies for [{bot.BotOwner.name}]");
                        //}
                    }
                }

                //if (forAI && 
                //    _nextLogUpdatedTime < Time.time)
                //{
                //    _nextLogUpdatedTime = Time.time + 5f;
                //    Logger.LogDebug($"Updated Vision for [{totalUpdated}] enemies for [{_localList.Count}] Bots.");
                //}

                _localList.Clear();

                yield return null;
            }
        }

        //private static float _nextLogUpdatedTime;
        //private static float _nextLogUpdatedTime2;

        private readonly List<BotComponent> _localVisionCheckListAI = new List<BotComponent>();
        private readonly List<BotComponent> _localVisionCheckListHumans = new List<BotComponent>();

        private IEnumerator raycastJobLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(0.05f);
            while (true)
            {
                int max = maxBotsPerFrame;
                if (Bots != null)
                {
                    _localBotList.AddRange(Bots.Values);
                    int i = 0;
                    foreach (var Bot in _localBotList)
                    {
                        if (Bot?.BotActive == true && 
                            Bot.NextCheckVisiblePlayerTime < Time.time)
                        {
                            Bot.NextCheckVisiblePlayerTime = Time.time + 0.1f;
                            _tempBotList.Add(Bot);
                            i++;
                            if (i > max)
                            {
                                i = 0;
                                yield return doRaycasts(_tempBotList);
                                _tempBotList.Clear();
                            }
                        }
                    }

                    if (_tempBotList.Count > 0)
                    {
                        yield return doRaycasts(_tempBotList);
                        _tempBotList.Clear();
                    }

                    _localBotList.Clear();
                }
                yield return wait;
            }
        }

        private int maxBotsPerFrame = 3;

        private readonly List<BotComponent> _localBotList = new List<BotComponent>();
        private readonly List<BotComponent> _tempBotList = new List<BotComponent>();

        private IEnumerator doRaycasts(List<BotComponent> botList)
        {
            CheckVisiblePlayers(botList);
            CheckHumanVisibility(botList);
            float time = Time.time;
            foreach (var bot in botList)
            {
                if (bot != null)
                    bot.NextCheckVisiblePlayerTime = time + 0.1f;
            }
            yield return null;
        }

        private readonly List<Player> _humanPlayers = new List<Player>();
        private readonly List<Player> _validPlayers = new List<Player>();

        private void CheckHumanVisibility(List<BotComponent> botList)
        {
            getHumanPlayers(_humanPlayers, out int partCount);

            //Logger.LogInfo(_humanPlayers.Count);

            int total = botList.Count * _humanPlayers.Count * partCount;

            if (total <= 0)
            {
                return;
            }

            //Logger.LogInfo(total);

            NativeArray<SpherecastCommand> allSpherecastCommands = new NativeArray<SpherecastCommand>(total, Allocator.TempJob);
            setSpherecastTargetsHumanVisibility(botList, _humanPlayers, allSpherecastCommands);

            NativeArray<RaycastHit> allRaycastHits = new NativeArray<RaycastHit>(total, Allocator.TempJob);
            SpherecastCommand.ScheduleBatch(allSpherecastCommands, allRaycastHits, MinJobSize).Complete();

            analyzeHitsHumanVisibility(botList, _humanPlayers, allRaycastHits);

            allSpherecastCommands.Dispose();
            allRaycastHits.Dispose();
            _humanPlayers.Clear();
        }

        private void setSpherecastTargetsHumanVisibility(List<BotComponent> botList, List<Player> players, NativeArray<SpherecastCommand> allSpherecastCommands)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];
                Vector3 head = getHeadPoint(bot);

                for (int j = 0; j < _humanPlayers.Count; j++)
                {
                    Player player = _humanPlayers[j];

                    foreach (var part in player.MainParts.Values)
                    {
                        Vector3 target = getTarget(part, head);
                        Vector3 direction = target - head;

                        float maxRange;
                        if (head == Vector3.zero || bot == null)
                        {
                            maxRange = 0f;
                        }
                        else
                        {
                            maxRange = direction.magnitude;
                        }

                        allSpherecastCommands[total] = new SpherecastCommand(head, SpherecastRadius, direction.normalized, maxRange, SightLayer);

                        total++;
                    }
                }
            }
        }

        private void analyzeHitsHumanVisibility(List<BotComponent> botList, List<Player> players, NativeArray<RaycastHit> allRaycastHits)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];

                for (int j = 0; j < players.Count; j++)
                {
                    Player player = players[j];
                    bool lineOfSight = false;

                    foreach (var part in player.MainParts.Values)
                    {
                        if (!lineOfSight)
                        {
                            lineOfSight = allRaycastHits[total].collider == null;
                        }
                        total++;
                    }

                    SAINEnemy sainEnemy = bot?.EnemyController?.GetEnemy(player.ProfileId);
                    if (sainEnemy != null)
                    {
                        sainEnemy.Vision.InLineOfSight = lineOfSight;
                    }
                }
            }
        }

        private void CheckVisiblePlayers(List<BotComponent> botList)
        {
            _validPlayers.Clear();
            foreach (var player in Players)
            {
                if (shallCheckPlayer(player))
                {
                    _validPlayers.Add(player);
                }
            }

            int total = botList.Count * _validPlayers.Count;

            if (total <= 0)
            {
                return;
            }

            //Logger.LogInfo(total);

            NativeArray<SpherecastCommand> allSpherecastCommands = new NativeArray<SpherecastCommand>(total, Allocator.TempJob);

            setSpherecastTargetsVisiblePlayers(botList, _validPlayers, allSpherecastCommands);

            NativeArray<RaycastHit> allRaycastHits = new NativeArray<RaycastHit>(total, Allocator.TempJob);

            SpherecastCommand.ScheduleBatch(allSpherecastCommands, allRaycastHits, MinJobSize).Complete();

            analyzeHitsVisiblePlayers(botList, _validPlayers, allRaycastHits);

            allSpherecastCommands.Dispose();
            allRaycastHits.Dispose();
            _validPlayers.Clear();
        }

        private void setSpherecastTargetsVisiblePlayers(List<BotComponent> botList, List<Player> players, NativeArray<SpherecastCommand> allSpherecastCommands)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];
                Vector3 head = getHeadPoint(bot);

                for (int j = 0; j < players.Count; j++)
                {
                    Player player = players[j];
                    Vector3 target = getTarget(player.MainParts[BodyPartType.body], head);
                    Vector3 direction = target - head;

                    float magnitude = direction.magnitude;
                    float max = player.IsAI ? player.AIData.BotOwner.LookSensor.VisibleDist * 1.5f : magnitude;

                    if (bot == null ||
                        head == Vector3.zero ||
                        !player.HealthController.IsAlive ||
                        player.ProfileId == bot.Player.ProfileId)
                    {
                        max = 0.1f;
                    }

                    float maxRange = Mathf.Clamp(magnitude, 0f, max);
                    allSpherecastCommands[total] = new SpherecastCommand(head, SpherecastRadius, direction.normalized, maxRange, SightLayer);

                    total++;
                }
            }
        }

        private void analyzeHitsVisiblePlayers(List<BotComponent> botList, List<Player> players, NativeArray<RaycastHit> allRaycastHits)
        {
            int total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                BotComponent bot = botList[i];

                var visPlayers = bot?.Memory?.VisiblePlayers;
                visPlayers?.Clear();

                for (int j = 0; j < players.Count; j++)
                {
                    Player player = players[j];

                    if (player != null && 
                        bot != null &&
                        bot?.Player != null &&
                        player.ProfileId != bot.Player.ProfileId)
                    {
                        bool lineOfSight = 
                            allRaycastHits[total].collider == null && 
                            player.HealthController.IsAlive;

                        if (lineOfSight)
                        {
                            visPlayers.Add(player);
                        }

                        if (player.IsAI)
                        {
                            var sainEnemy = bot.EnemyController.GetEnemy(player.ProfileId);
                            if (sainEnemy != null)
                            {
                                sainEnemy.Vision.InLineOfSight = lineOfSight;
                            }
                        }
                    }
                    total++;
                }
            }
        }

        private void getHumanPlayers(List<Player> playerList, out int partCount)
        {
            playerList.Clear();
            partCount = 0;
            foreach (var player in Players)
            {
                if (player != null &&
                    player.Transform != null &&
                    !player.IsAI)
                {
                    if (partCount == 0)
                    {
                        partCount = player.MainParts.Count;
                    }
                    playerList.Add(player);
                }
            }
        }

        private bool shallCheckPlayer(Player player)
        {
            if (player == null || player.Transform == null)
            {
                return false;
            }
            if (player.IsAI)
            {
                if (player.AIData.BotOwner?.BotState != EBotState.Active ||
                    player.AIData.BotOwner?.StandBy?.StandByType != BotStandByType.active)
                {
                    return false;
                }
            }
            return true;
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