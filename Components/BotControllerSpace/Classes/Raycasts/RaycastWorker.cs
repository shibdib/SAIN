using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.Components
{
    public class RaycastWorker
    {
        public bool WorkerRunning { get; private set; }

        public BotRaycastTotalCheck BotRaycastCheck { get; private set; }
        public JobHandle CurrentJob { get; private set; }
        public NativeArray<JobHandle> Handles { get; private set; }

        private readonly List<JobHandle> _handles = new List<JobHandle>();

        public void AssignWork(BotRaycastTotalCheck check)
        {
            CompleteWork();
            BotRaycastCheck = check;
            combineDependencies(check);
            WorkerRunning = true;
        }

        private void combineDependencies(BotRaycastTotalCheck check)
        {
            _handles.Clear();
            foreach (var raycast in check.LineOfSightChecks) {
                _handles.Add(raycast.Job.Handle);
            }
            foreach (var raycast in check.ShootChecks) {
                _handles.Add(raycast.Job.Handle);
            }
            foreach (var raycast in check.VisibleChecks) {
                _handles.Add(raycast.Job.Handle);
            }

            Handles = new NativeArray<JobHandle>(_handles.ToArray(), Allocator.TempJob);
            CurrentJob = JobHandle.CombineDependencies(Handles);
            _handles.Clear();
        }

        public void CompleteWork()
        {
            if (!WorkerRunning) {
                return;
            }

            CurrentJob.Complete();
            updateResults(BotRaycastCheck);

            BotRaycastCheck.Dispose();
            Handles.Dispose();

            WorkerRunning = false;
        }

        private void updateResults(BotRaycastTotalCheck cachedJobs)
        {
            float time = Time.time;
            int count = cachedJobs.Count;
            for (int i = 0; i < count; i++) {
                var losCheck = cachedJobs.LineOfSightChecks[i];
                losCheck.Data.Enemy?.Vision.VisionChecker.EnemyParts.ReadRaycastResult(losCheck, ERaycastCheck.LineofSight, time);
                var shootCheck = cachedJobs.ShootChecks[i];
                shootCheck.Data.Enemy?.Vision.VisionChecker.EnemyParts.ReadRaycastResult(shootCheck, ERaycastCheck.Shoot, time);
                var visionCheck = cachedJobs.VisibleChecks[i];
                visionCheck.Data.Enemy?.Vision.VisionChecker.EnemyParts.ReadRaycastResult(visionCheck, ERaycastCheck.Vision, time);
            }
        }
    }
}