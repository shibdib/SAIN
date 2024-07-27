using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components.BotControllerSpace.Classes.Raycasts
{
    public struct CalcDistanceJob : IJobFor
    {
        [ReadOnly] public NativeArray<Vector3> origins;
        [ReadOnly] public NativeArray<Vector3> targets;

        [WriteOnly] public NativeArray<float> distances;
        [WriteOnly] public NativeArray<Vector3> directions;
        [WriteOnly] public NativeArray<Vector3> normals;

        public void Execute(int index)
        {
            Vector3 origin = origins[index];
            Vector3 target = targets[index];
            Vector3 direction = target - origin;
            directions[index] = direction;
            distances[index] = direction.magnitude;
            normals[index] = direction.normalized;
        }
    }

    public struct EnemyDistStruct
    {
        public Vector3 Origin;
        public Vector3 Target;
        public Vector3 Direction;
        public Vector3 Normal;
        public float Distance;
    }

    internal class EnemyDistanceJob : SAINControllerBase
    {
        private bool hasJobFromLastFrame = false;

        private JobHandle _jobHandle;
        private CalcDistanceJob _distanceJob;

        public EnemyDistanceJob(SAINBotController botController) : base(botController)
        {
        }

        private EnemyDistStruct[] setupJob()
        {
            return null;
        }

        public void Update()
        {
            // Ensure the last frame's job is completed
            if (hasJobFromLastFrame) {
                _jobHandle.Complete();
            }

            // Then we start creating the job for the next frame

            // Create a temporary NativeArray to store the data from verticesArray
            //var verticesNativeArray = new NativeArray<Vector3>(_distanceResults, Allocator.TempJob);
            //
            //// Create the job
            //_distanceJob = new CalcDistanceJob {
            //    vertices = verticesNativeArray,
            //    newVertices = _distanceResultNativeArray
            //};
            //
            //// Schedule the job
            //_jobHandle = _distanceJob.Schedule(vertexCount, new JobHandle());

            // Dispose of temporary NativeArray
            //verticesNativeArray.Dispose();

            // Set this bool to true so the job can complete next frame
            hasJobFromLastFrame = true;
        }

        private void getEnemies(List<Enemy> enemyList)
        {
            enemyList.Clear();
            var bots = Bots;
            if (bots == null || bots.Count == 0) {
                return;
            }
            foreach (var bot in Bots.Values) {
                if (bot == null) continue;
                foreach (var enemy in bot.EnemyController.Enemies.Values) {
                    if (!enemy.WasValid) continue;
                    enemyList.Add(enemy);
                }
            }
        }

        private readonly List<Enemy> _enemies = new List<Enemy>();

        public void Dispose()
        {
        }

        private void Start()
        {
            //_distanceResultNativeArray = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
        }

        private void OnDestroy()
        {
            // Finish ongoing job
            if (hasJobFromLastFrame) {
                _jobHandle.Complete();
            }

            // Dispose of persistent NativeArray
            //_distanceResultNativeArray.Dispose();
        }
    }
}