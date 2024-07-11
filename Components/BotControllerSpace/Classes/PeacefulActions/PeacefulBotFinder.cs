using SAIN.SAINComponent;
using System.Collections.Generic;

namespace SAIN.Components.BotController.PeacefulActions
{
    public class PeacefulBotFinder : SAINControllerBase, IBotControllerClass
    {
        public Dictionary<string, BotZoneData> ZoneDatas = new Dictionary<string, BotZoneData>();

        public PeacefulBotFinder(SAINBotController controller) : base(controller)
        {
        }

        public void Init()
        {
            BotController.BotSpawnController.OnBotAdded += botAdded;
            BotController.BotSpawnController.OnBotRemoved += botRemoved;
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            BotController.BotSpawnController.OnBotAdded -= botAdded;
            BotController.BotSpawnController.OnBotRemoved -= botRemoved;
        }

        private void botAdded(BotComponent bot)
        {
            BotZone botZone = bot.BotOwner.BotsGroup.BotZone;
            if (botZone == null) {
                Logger.LogWarning($"Null BotZone for [{bot.BotOwner.name}]");
                return;
            }

            if (!ZoneDatas.TryGetValue(botZone.NameZone, out BotZoneData data)) {
                data = new BotZoneData(botZone);
                ZoneDatas.Add(data.Name, data);
            }

            data.AddBot(bot);
        }

        private void botRemoved(BotComponent bot)
        {
            BotZone botZone = bot.BotOwner.BotsGroup.BotZone;
            if (botZone == null) {
                Logger.LogWarning($"Null BotZone for [{bot.BotOwner.name}]");
                return;
            }
            BotZoneData data;
            if (!ZoneDatas.TryGetValue(botZone.NameZone, out data)) {
                return;
            }
            data.RemoveBot(bot);
        }
    }
}