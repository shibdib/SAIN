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
            EnemyRaycastStruct raycast = Raycasts[index];

            LayerMask lineOfSightMask = LayerMaskClass.HighPolyWithTerrainMask;
            LayerMask shootMask = LayerMaskClass.HighPolyWithTerrainMask;
            LayerMask visionMask = LayerMaskClass.AI;

            float enemyDistance = raycast.EnemyDistance;
            Vector3 eyePos = raycast.EyePosition;
            Vector3 shootPoint = raycast.WeaponFirePort;

            var parts = raycast.Raycasts;
            int count = parts.Length;

            for (int i = 0; i < count; i++) {
                parts[i] = checkPart(
                    parts[i],
                    enemyDistance,
                    eyePos,
                    shootPoint,
                    lineOfSightMask,
                    shootMask,
                    visionMask);
            }
        }

        private BodyPartRaycast checkPart(BodyPartRaycast part, float enemyDistance, Vector3 eyePos, Vector3 shootPoint, LayerMask LOSMask, LayerMask shootMask, LayerMask visionMask)
        {
            part.LineOfSight = false;
            part.CanShoot = false;
            if (enemyDistance > part.MaxRange) {
                return part;
            }

            Vector3 castPoint = part.CastPoint;
            Vector3 direction = castPoint - eyePos;
            float distance = direction.magnitude;
            part.LineOfSight = !Physics.Raycast(eyePos, direction, out RaycastHit losHit, distance, LOSMask);
            part.LOSRaycastHit = losHit;
            if (part.LineOfSight) {
                Vector3 weaponDirection = castPoint - shootPoint;
                part.CanShoot = !Physics.Raycast(shootPoint, weaponDirection, out RaycastHit shootHit, weaponDirection.magnitude, shootMask);
                part.ShootRayCastHit = shootHit;

                part.IsVisible = !Physics.Raycast(eyePos, direction, out RaycastHit visionHit, distance, visionMask);
                part.VisionRaycastHit = visionHit;
            }
            return part;
        }
    }

    public struct EnemyRaycastStruct
    {
        public string BotName;
        public string EnemyProfileId;
        public Vector3 EyePosition;
        public Vector3 WeaponFirePort;
        public float EnemyDistance;
        public BodyPartRaycast[] Raycasts;
    }

    public struct BodyPartRaycast
    {
        public EBodyPart PartType;
        public EBodyPartColliderType ColliderType;

        public float MaxRange;
        public Vector3 CastPoint;

        public RaycastHit LOSRaycastHit;
        public bool LineOfSight;

        public RaycastHit ShootRayCastHit;
        public bool CanShoot;

        public RaycastHit VisionRaycastHit;
        public bool IsVisible;
    }

    public class LineOfSightJobClass : SAINControllerBase
    {
        private const int BOTS_PER_FRAME = 5;

        private bool hasJobFromLastFrame = false;

        private JobHandle _raycastJobHandle;
        private RaycastEnemiesJob _raycastJob;
        private EnemyRaycastStruct[] _raycastArray;

        private readonly List<BotComponent> _localList = new List<BotComponent>();
        private readonly List<EnemyRaycastStruct> _enemyRaycasts = new List<EnemyRaycastStruct>();

        public LineOfSightJobClass(SAINBotController botController) : base(botController)
        {
        }

        public void Update()
        {
            //try {
            finishJob();
            if (Bots.Count == 0) {
                return;
            }
            findBotsForJob();
            setupJob(_enemyRaycasts);

            //}
            //catch (Exception ex) {
            //    Logger.LogError(ex);
            //}
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
            _raycastArray = _raycastJob.Raycasts.ToArray();

            for (int i = 0; i < _raycastArray.Length; i++) {
                EnemyRaycastStruct raycastStruct = _raycastArray[i];
                if (!Bots.TryGetValue(raycastStruct.BotName, out var bot) || bot == null) {
                    continue;
                }
                bot.Vision.TimeLastCheckedLOS = Time.time;
                Enemy enemy = bot.EnemyController.GetEnemy(raycastStruct.EnemyProfileId, false);
                if (enemy == null) {
                    continue;
                }

                var enemyParts = enemy.Vision.VisionChecker.EnemyParts.Parts;

                BodyPartRaycast[] raycasts = raycastStruct.Raycasts;
                for (int j = 0; j < raycasts.Length; j++) {
                    BodyPartRaycast raycast = raycasts[j];
                    enemyParts.TryGetValue(raycast.PartType, out var partData);
                    partData?.SetLineOfSight(raycast);
                }
            }

            _raycastJob.Raycasts.Dispose();
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
                if (bot.Vision.TimeSinceCheckedLOS < 0.05f) continue;

                Vector3 origin = bot.Transform.EyePosition;
                Vector3 firePort = bot.Transform.WeaponFirePort;
                var enemies = bot.EnemyController.Enemies;
                bool gotEnemyToCheck = false;
                float time = Time.time;

                foreach (Enemy enemy in enemies.Values) {
                    if (enemy == null) continue;

                    float delay = enemy.IsAI ? 0.1f : 0.05f;
                    if (time - enemy.Vision.VisionChecker.LastCheckLOSTime < delay) continue;
                    if (!enemy.CheckValid()) continue;

                    List<BodyPartRaycast> rayCasts = enemy.Vision.VisionChecker.GetPartsToCheck(origin);
                    if (rayCasts.Count == 0) continue;

                    EnemyRaycastStruct result = new EnemyRaycastStruct {
                        BotName = bot.name,
                        EnemyProfileId = enemy.EnemyProfileId,
                        EnemyDistance = enemy.RealDistance,
                        EyePosition = origin,
                        WeaponFirePort = firePort,
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
            var enemyArray = enemyList.ToArray();
            int count = enemyArray.Length;
            _raycastJob = new RaycastEnemiesJob {
                Raycasts = new NativeArray<EnemyRaycastStruct>(enemyArray, Allocator.TempJob),
            };
            _raycastJobHandle = _raycastJob.Schedule(count, new JobHandle());
            hasJobFromLastFrame = true;
            //Logger.LogDebug($"Job Scheduled for {count} Enemies...");
        }
    }
}