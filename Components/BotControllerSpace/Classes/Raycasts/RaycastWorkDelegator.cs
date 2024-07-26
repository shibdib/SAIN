using SAIN.SAINComponent;
using System.Collections.Generic;

namespace SAIN.Components
{
    public class RaycastWorkDelegator : SAINControllerBase
    {
        public int WorkerCount { get; }

        public readonly Dictionary<int, RaycastWorker> Workers = new Dictionary<int, RaycastWorker>();

        private RayCastJobClass JobCreator { get; }

        public RaycastWorkDelegator(SAINBotController botController) : base(botController)
        {
            JobCreator = new RayCastJobClass(botController);
            WorkerCount = System.Environment.ProcessorCount - 1;
            createWorkers();
        }

        public void Update()
        {
            UpdateWorkers();
        }

        public void Dispose()
        {
            foreach (var worker in Workers.Values)
                worker.CompleteWork();
        }

        private void createWorkers()
        {
            Logger.LogInfo($"{WorkerCount} CPU Cores to Delegate to.");
            for (int i = 0; i < WorkerCount; i++) {
                Workers.Add(i, new RaycastWorker());
            }
            Logger.LogInfo($"{Workers.Count} Workers Created.");
        }

        public void UpdateWorkers()
        {
            foreach (var worker in Workers.Values)
                if (worker.WorkerRunning)
                    worker.CompleteWork();

            int availableWorkers = AvailableWorkers;
            if (availableWorkers == 0) {
                return;
            }

            if (Bots == null || Bots.Count == 0) {
                return;
            }

            var bots = _localList;
            bots.Clear();
            bots.AddRange(Bots.Values);
            // sort bots by the time they were last run through this function,
            // the lower the TimeLastChecked, the longer the time since they had their enemies checked
            bots.Sort((x, y) => x.Vision.TimeLastCheckedLOS.CompareTo(y.Vision.TimeLastCheckedLOS));

            int botCount = bots.Count;
            int foundBots = 0;
            for (int i = 0; i < botCount; i++) {
                BotComponent bot = bots[i];
                if (bot == null) continue;
                if (!bot.BotActive) continue;
                //if (bot.Vision.TimeSinceCheckedLOS < 0.05f) continue;

                var job = JobCreator.ScheduleJobs(bot);
                if (job == null) continue;

                if (!assignNextWorker(job.Value)) {
                    job.Value.Complete();
                    job.Value.Dispose();
                    break;
                }

                foundBots++;
                if (foundBots >= availableWorkers) {
                    break;
                }
                if (foundBots > MAX_BOTS_SCHEDULED_PER_FRAME) {
                    //break;
                }
            }

            //Logger.LogInfo($"Scheduled {foundBots} bots...");
        }

        private int AvailableWorkers {
            get
            {
                int count = 0;
                foreach (var worker in Workers.Values) {
                    if (!worker.WorkerRunning) {
                        count++;
                    }
                }
                return count;
            }
        }

        private bool assignNextWorker(BotRaycastTotalCheck raycastCheck)
        {
            foreach (var worker in Workers.Values)
                if (!worker.WorkerRunning) {
                    worker.AssignWork(raycastCheck);
                    return true;
                }
            return false;
        }

        private const int MAX_BOTS_SCHEDULED_PER_FRAME = 4;

        private readonly List<BotComponent> _localList = new List<BotComponent>();
    }
}