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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace SAIN.Components
{
    public class LineOfSightManager : SAINControl
    {
        public void Update()
        {
            if (_jobCoroutine == null)
            {
                _jobCoroutine = BotController.StartCoroutine(raycastJobLoop());
            }
        }

        private Coroutine _jobCoroutine;

        private IEnumerator raycastJobLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(0.05f);
            while (true)
            {
                if (Bots != null)
                {
                    _localBotList.AddRange(Bots.Values);
                    int i = 0;
                    foreach (var Bot in _localBotList)
                    {
                        if (Bot?.BotActive == true)
                        {
                            _tempBotList.Add(Bot);
                            i++;
                            if (i > 3)
                            {
                                i = 0;
                                yield return doRaycasts(_tempBotList);
                            }
                        }
                    }
                    if (_tempBotList.Count > 0)
                    {
                        yield return doRaycasts(_tempBotList);
                    }
                    _localBotList.Clear();
                }
                yield return wait;
            }
        }

        private readonly List<BotComponent> _localBotList = new List<BotComponent>();
        private readonly List<BotComponent> _tempBotList = new List<BotComponent>();

        private IEnumerator doRaycasts(List<BotComponent> botList)
        {
            CheckVisiblePlayers(botList);
            yield return null;
            CheckHumanVisibility(botList);
            botList.Clear();
        }

        private Vector3 HeadPos(Player player)
        {
            return player.MainParts[BodyPartType.head].Position;
        }

        private Vector3 BodyPos(Player player)
        {
            return player.MainParts[BodyPartType.body].Position;
        }

        private readonly List<Player> _humanPlayers = new List<Player>();
        private readonly List<Player> _validPlayers = new List<Player>();

        private void CheckHumanVisibility(List<BotComponent> botList)
        {
            _humanPlayers.Clear();
            int partCount = 0;
            foreach (var player in Players)
            {
                if (player != null 
                    && player.Transform != null 
                    //&& player.Transform.Original != null 
                    && player.IsAI == false)
                {
                    if (partCount == 0)
                    {
                        partCount = player.MainParts.Count;
                    }
                    _humanPlayers.Add(player);
                }
            }

            int total = botList.Count * _humanPlayers.Count * partCount;

            if (total <= 0)
            {
                return;
            }

            NativeArray<SpherecastCommand> allSpherecastCommands = new NativeArray<SpherecastCommand>(total, Allocator.TempJob);
            NativeArray<RaycastHit> allRaycastHits = new NativeArray<RaycastHit>(total, Allocator.TempJob);

            total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];
                Vector3 head = bot.BotOwner.LookSensor._headPoint;
                for (int j = 0; j < _humanPlayers.Count; j++)
                {
                    Player player = _humanPlayers[j];
                    foreach (var part in player.MainParts.Values)
                    {
                        Vector3 target = part.Position;
                        Vector3 direction = target - head;
                        allSpherecastCommands[total] = new SpherecastCommand(head, SpherecastRadius, direction.normalized, direction.magnitude, SightLayers);
                        total++;
                    }
                }
            }

            SpherecastCommand.ScheduleBatch(allSpherecastCommands, allRaycastHits, MinJobSize).Complete();

            total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];
                Vector3 head = bot.BotOwner.LookSensor._headPoint;
                for (int j = 0; j < _humanPlayers.Count; j++)
                {
                    Player player = _humanPlayers[j];
                    bool lineOfSight = false;
                    foreach (var part in player.MainParts.Values)
                    {
                        if (!lineOfSight)
                        {
                            lineOfSight = allRaycastHits[total].collider == null;
                        }
                        total++;
                    }
                    var sainEnemy = bot.EnemyController.GetEnemy(player.ProfileId);
                    if (sainEnemy != null)
                    {
                        sainEnemy.Vision.InLineOfSight = lineOfSight;
                    }
                }
            }

            allSpherecastCommands.Dispose();
            allRaycastHits.Dispose();
            _humanPlayers.Clear();
        }

        private void CheckVisiblePlayers(List<BotComponent> botList)
        {
            _validPlayers.Clear();
            foreach (var player in Players)
            {
                if (player != null 
                    && player.Transform != null)
                {
                    if (player.IsAI && 
                        player.AIData.BotOwner?.BotState != EBotState.Active)
                    {
                        continue;
                    }
                    _validPlayers.Add(player);
                }
            }
            int total = botList.Count * _validPlayers.Count;

            if (total <= 0)
            {
                return;
            }

            NativeArray<SpherecastCommand> allSpherecastCommands = new NativeArray<SpherecastCommand>(total, Allocator.TempJob);
            NativeArray<RaycastHit> allRaycastHits = new NativeArray<RaycastHit>(total, Allocator.TempJob);

            total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];
                Vector3 head = HeadPos(bot.BotOwner.GetPlayer);

                for (int j = 0; j < _validPlayers.Count; j++)
                {
                    Player player = _validPlayers[j];
                    Vector3 target = BodyPos(player);
                    Vector3 direction = target - head;

                    float magnitude = direction.magnitude;
                    float max = player.IsAI ? player.AIData.BotOwner.LookSensor.VisibleDist : magnitude;

                    if (!player.HealthController.IsAlive || (player.IsAI && player.AIData.BotOwner.BotState != EBotState.Active) || player.ProfileId == bot.Player.ProfileId)
                    {
                        max = 0f;
                    }

                    float rayDistance = Mathf.Clamp(magnitude, 0f, max);
                    allSpherecastCommands[total] = new SpherecastCommand(head, SpherecastRadius, direction.normalized, rayDistance, SightLayers );
                    total++;
                }
            }

            SpherecastCommand.ScheduleBatch(allSpherecastCommands, allRaycastHits, MinJobSize).Complete();

            total = 0;
            for (int i = 0; i < botList.Count; i++)
            {
                BotComponent bot = botList[i];
                var visPlayers = bot.Memory.VisiblePlayers;
                visPlayers.Clear();
                for (int j = 0; j < _validPlayers.Count; j++)
                {
                    Player player = _validPlayers[j];

                    if (player != null && player.ProfileId != bot.Player.ProfileId)
                    {
                        bool lineOfSight = allRaycastHits[total].collider == null && player.HealthController.IsAlive;
                        if (lineOfSight)
                        {
                            visPlayers.Add(player);
                        }
                        var sainEnemy = bot.EnemyController.CheckAddEnemy(player);
                        if (sainEnemy?.IsAI == true)
                        {
                            sainEnemy.Vision.InLineOfSight = lineOfSight;
                        }
                        //if (bot.BotOwner?.EnemiesController.IsEnemy(player) == true)
                        //{
                        //    var sainEnemy = bot.EnemyController.CheckAddEnemy(player);
                        //    if (sainEnemy?.IsAI == true)
                        //    {
                        //        sainEnemy.Vision.InLineOfSight = lineOfSight;
                        //    }
                        //}
                    }
                    total++;
                }
            }

            allSpherecastCommands.Dispose();
            allRaycastHits.Dispose();
            _validPlayers.Clear();
        }

        private void GlobalRaycastJobOld()
        {
            int total = _tempBotList.Count * Players.Count;

            NativeArray<SpherecastCommand> allSpherecastCommands = new NativeArray<SpherecastCommand>(
                total,
                Allocator.TempJob
            );
            NativeArray<RaycastHit> allRaycastHits = new NativeArray<RaycastHit>(
                total,
                Allocator.TempJob
            );

            total = 0;
            for (int i = 0; i < _tempBotList.Count; i++)
            {
                var bot = _tempBotList[i];
                Vector3 head = HeadPos(bot.BotOwner.GetPlayer);

                for (int j = 0; j < Players.Count; j++)
                {
                    Player player = Players[j];
                    Vector3 target = BodyPos(player);
                    Vector3 direction = target - head;
                    float magnitude = direction.magnitude;
                    float max = player.IsAI ? player.AIData.BotOwner.LookSensor.VisibleDist : magnitude;

                    if (!player.HealthController.IsAlive || (player.IsAI && player.AIData.BotOwner.BotState != EBotState.Active))
                    {
                        max = 0f;
                    }

                    float rayDistance = Mathf.Clamp(magnitude, 0f, max);

                    allSpherecastCommands[total] = new SpherecastCommand(
                        head,
                        SpherecastRadius,
                        direction.normalized,
                        rayDistance,
                        SightLayers
                    );
                    total++;
                }
            }

            JobHandle spherecastJob = SpherecastCommand.ScheduleBatch(
                allSpherecastCommands,
                allRaycastHits,
                MinJobSize
            );

            spherecastJob.Complete();
            total = 0;

            for (int i = 0; i < _tempBotList.Count; i++)
            {
                var visPlayers = _tempBotList[i].Memory.VisiblePlayers;
                visPlayers.Clear();
                for (int j = 0; j < Players.Count; j++)
                {
                    Player player = Players[j];
                    if (allRaycastHits[total].collider == null && player != null && player.HealthController.IsAlive)
                    {
                        visPlayers.Add(player);
                        string id = player.ProfileId;
                    }
                    total++;
                }
            }

            allSpherecastCommands.Dispose();
            allRaycastHits.Dispose();
        }

        private readonly float SpherecastRadius = 0.025f;
        private LayerMask SightLayers => LayerMaskClass.HighPolyWithTerrainMask;
        private readonly int MinJobSize = 1;
        private List<Player> Players => EFTInfo.AlivePlayers;
    }
}