using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components
{
    public struct RaycastEnemiesJob : IJobFor
    {
        public NativeArray<EnemyRaycastStruct> Raycasts;

        public void Execute(int index)
        {
        }
    }

    public struct EnemyRaycastStruct
    {
        public BotComponent Bot;
        public Enemy Enemy;
        public BodyPartRaycast[] Raycasts;
    }

    public struct BodyPartRaycast
    {
        public EnemyPartDataClass PartData;
        public Vector3 CastPoint;
        public RaycastHit RaycastHit;
        public EBodyPartColliderType PartType;
    }

    public class LineOfSightJobClass : SAINControllerBase
    {
        private const int BOTS_PER_FRAME = 5;

        private bool hasJobFromLastFrame = false;

        private JobHandle _raycastJobHandle;
        private RaycastEnemiesJob _raycastJob;
        private EnemyRaycastStruct[] raycastArray;

        private readonly List<BotComponent> _localList = new List<BotComponent>();
        private readonly List<EnemyRaycastStruct> _enemyRaycasts = new List<EnemyRaycastStruct>();

        public LineOfSightJobClass(SAINBotController botController) : base(botController)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            finishJob();
            if (Bots.Count == 0) {
                return;
            }
            findBotsForJob();
            setupJob(_enemyRaycasts);
        }

        public void Dispose()
        {
        }

        private void finishJob()
        {
            if (!hasJobFromLastFrame) {
                return;
            }

            // Ensure the last frame's job is completed
            _raycastJobHandle.Complete();

            // update each enemy with results
            raycastArray = _raycastJob.Raycasts.ToArray();

            for (int i = 0; i < raycastArray.Length; i++) {
                EnemyRaycastStruct raycastStruct = raycastArray[i];
                BotComponent bot = raycastStruct.Bot;
                if (bot == null) {
                    continue;
                }

                BodyPartRaycast[] raycasts = raycastStruct.Raycasts;
                for (int j = 0; j < raycasts.Length; j++) {
                    BodyPartRaycast raycast = raycasts[j];
                    raycast.PartData.SetLineOfSight(raycast);
                }
            }

            hasJobFromLastFrame = false;
        }

        private void findBotsForJob()
        {
            _localList.Clear();
            _localList.AddRange(Bots.Values);
            _enemyRaycasts.Clear();
            findBotsToCheck(_localList, _enemyRaycasts, BOTS_PER_FRAME);
        }

        private void findBotsToCheck(List<BotComponent> bots, List<EnemyRaycastStruct> enemiesResult, int countToCheck)
        {
            // sort bots by the time they were last run through this function,
            // the lower the TimeLastChecked, the longer the time since they had their enemies checked
            bots.Sort((x, y) => x.Vision.TimeLastCheckedLOS.CompareTo(y.Vision.TimeLastCheckedLOS));

            int foundBots = 0;
            for (int i = 0; i < bots.Count; i++) {
                BotComponent bot = bots[i];
                if (bot == null) continue;
                if (!bot.BotActive) continue;

                Vector3 origin = bot.Transform.EyePosition;
                var enemies = bot.EnemyController.Enemies;
                bool gotEnemyToCheck = false;

                foreach (Enemy enemy in enemies.Values) {
                    if (enemy == null) continue;
                    if (!enemy.CheckValid()) continue;

                    List<BodyPartRaycast> rayCasts = enemy.Vision.VisionChecker.GetPartsToCheck(origin);
                    if (rayCasts.Count == 0) continue;

                    EnemyRaycastStruct result = new EnemyRaycastStruct {
                        Bot = bot,
                        Enemy = enemy,
                        Raycasts = rayCasts.ToArray()
                    };
                    enemiesResult.Add(result);
                    if (!gotEnemyToCheck)
                        gotEnemyToCheck = true;
                }

                if (gotEnemyToCheck) {
                    foundBots++;
                    if (foundBots == countToCheck) {
                        break;
                    }
                }
            }
        }

        private void setupJob(List<EnemyRaycastStruct> enemyList)
        {
            // Then we start creating the job for the next frame

            // Create a temporary NativeArray to store the data
            var enemyArray = enemyList.ToArray();
            int count = enemyArray.Length;

            var raycastNativeArrayTemp = new NativeArray<EnemyRaycastStruct>(enemyArray, Allocator.TempJob);

            // Create the job
            _raycastJob = new RaycastEnemiesJob {
                Raycasts = raycastNativeArrayTemp,
            };

            // Schedule the job
            _raycastJobHandle = _raycastJob.Schedule(count, new JobHandle());

            // Dispose of temporary NativeArray
            raycastNativeArrayTemp.Dispose();

            // Set this bool to true so the job can complete next frame
            hasJobFromLastFrame = true;
        }
    }
}