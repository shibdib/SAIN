using SAIN.SAINComponent;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace SAIN.Components
{
    public struct BotRaycastTotalCheck
    {
        public BotComponent Bot;
        public int Count;
        public BotRaycastData[] LineOfSightChecks;
        public BotRaycastData[] ShootChecks;
        public BotRaycastData[] VisibleChecks;

        public void Complete()
        {
            for (int i = 0; i < Count; i++) {
                LineOfSightChecks[i].Job.Handle.Complete();
                ShootChecks[i].Job.Handle.Complete();
                VisibleChecks[i].Job.Handle.Complete();
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < Count; i++) {
                LineOfSightChecks[i].Job.Dispose();
                ShootChecks[i].Job.Dispose();
                VisibleChecks[i].Job.Dispose();
            }
        }
    }
}