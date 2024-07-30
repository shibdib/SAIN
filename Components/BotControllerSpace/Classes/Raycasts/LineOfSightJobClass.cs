using SAIN.Components.BotController;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components
{
    public class LineOfSightJobClass : SAINControllerBase
    {
        public RaycastWorkDelegator RaycastJobDelegator { get; }
        public BatchRaycastJob1 BatchRaycasts { get; }

        public LineOfSightJobClass(SAINBotController botController) : base(botController)
        {
            RaycastJobDelegator = new RaycastWorkDelegator(botController);
            BatchRaycasts = new BatchRaycastJob1(botController);
        }

        public void Update()
        {
            RaycastJobDelegator.Update();
        }

        public void Dispose()
        {
            RaycastJobDelegator.Dispose();
        }
    }

    public class BatchRaycastJob1 : SAINControllerBase
    {
        public BatchRaycastJob1(SAINBotController botcontroller) : base(botcontroller)
        {
            botcontroller.StartCoroutine(checkVisionLoop());
        }

        private IEnumerator checkVisionLoop()
        {
            yield return null;

            while (true) {
                yield return null;

                if (BotController == null) {
                    continue;
                }

                var bots = BotController.BotSpawnController?.BotDictionary;
                if (bots == null || bots.Count == 0) {
                    continue;
                }

                if (BotController?.BotGame?.Status == EFT.GameStatus.Stopping) {
                    continue;
                }

                findEnemies(bots, _enemies);
                int enemyCount = _enemies.Count;
                if (enemyCount == 0) {
                    continue;
                }

                if (_partCount < 0) {
                    _partCount = _enemies[0].Vision.VisionChecker.EnemyParts.PartsArray.Length;
                }
                int partCount = _partCount;

                int totalRaycasts = enemyCount * partCount * RAYCAST_CHECKS;

                NativeArray<RaycastHit> raycastHits = new NativeArray<RaycastHit>(totalRaycasts, Allocator.TempJob);
                NativeArray<RaycastCommand> raycastCommands = new NativeArray<RaycastCommand>(totalRaycasts, Allocator.TempJob);

                _colliderTypes.Clear();
                _castPoints.Clear();

                int commands = 0;
                for (int i = 0; i < enemyCount; i++) {
                    var enemy = _enemies[i];
                    var transform = enemy.Bot.Transform;
                    Vector3 eyePosition = transform.EyePosition;
                    Vector3 weaponFirePort = transform.WeaponFirePort;
                    var parts = enemy.Vision.VisionChecker.EnemyParts.PartsArray;

                    for (int j = 0; j < partCount; j++) {
                        var part = parts[j];
                        BodyPartRaycast raycastData = part.GetRaycast(eyePosition, float.MaxValue);
                        Vector3 castPoint = raycastData.CastPoint;

                        _colliderTypes.Add(raycastData.ColliderType);
                        _castPoints.Add(castPoint);

                        Vector3 weaponDir = castPoint - weaponFirePort;
                        Vector3 eyeDir = castPoint - eyePosition;
                        float eyeDirMag = eyeDir.magnitude;

                        raycastCommands[commands] = new RaycastCommand(eyePosition, eyeDir, eyeDirMag, _LOSMask);
                        commands++;

                        raycastCommands[commands] = new RaycastCommand(eyePosition, eyeDir, eyeDirMag, _VisionMask);
                        commands++;

                        raycastCommands[commands] = new RaycastCommand(weaponFirePort, weaponDir, weaponDir.magnitude, _ShootMask);
                        commands++;
                    }
                }

                JobHandle handle = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 24);
                yield return null;
                handle.Complete();

                float time = Time.time;
                int hits = 0;

                for (int i = 0; i < enemyCount; i++) {
                    var enemy = _enemies[i];
                    var transform = enemy.Bot.Transform;
                    Vector3 origin = transform.EyePosition;
                    Vector3 weaponFirePort = transform.WeaponFirePort;
                    var visionChecker = enemy.Vision.VisionChecker;
                    var parts = visionChecker.EnemyParts.PartsArray;
                    visionChecker.LastCheckLOSTime = time;

                    for (int j = 0; j < partCount; j++) {
                        var part = parts[j];
                        EBodyPartColliderType colliderType = _colliderTypes[i + j];
                        Vector3 castPoint = _castPoints[i + j];

                        part.SetLineOfSight(castPoint, colliderType, raycastHits[hits], ERaycastCheck.LineofSight, time);
                        hits++;
                        part.SetLineOfSight(castPoint, colliderType, raycastHits[hits], ERaycastCheck.Vision, time);
                        hits++;
                        part.SetLineOfSight(castPoint, colliderType, raycastHits[hits], ERaycastCheck.Shoot, time);
                        hits++;
                    }
                }

                raycastCommands.Dispose();
                raycastHits.Dispose();
            }
        }

        private const int RAYCAST_CHECKS = 3;
        private readonly LayerMask _LOSMask = LayerMaskClass.HighPolyWithTerrainMask;
        private readonly LayerMask _VisionMask = LayerMaskClass.AI;
        private readonly LayerMask _ShootMask = LayerMaskClass.HighPolyWithTerrainMask;
        private int _partCount = -1;
        private readonly List<EBodyPartColliderType> _colliderTypes;
        private readonly List<Vector3> _castPoints;

        private static void findEnemies(BotDictionary bots, List<Enemy> result)
        {
            result.Clear();
            float time = Time.time;
            foreach (var bot in bots.Values) {
                if (bot == null || !bot.BotActive) continue;
                if (bot.Vision.TimeSinceCheckedLOS < 0.05f) continue;
                foreach (var enemy in bot.EnemyController.Enemies.Values) {
                    if (!enemy.CheckValid()) continue;
                    var visionChecker = enemy.Vision.VisionChecker;
                    if (visionChecker.LastCheckLOSTime < time) {
                        visionChecker.LastCheckLOSTime = time + (enemy.IsAI ? 0.1f : 0.05f);
                        result.Add(enemy);
                    }
                }
            }
        }

        private readonly List<Enemy> _enemies = new List<Enemy>();
    }
}