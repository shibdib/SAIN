using GPUInstancer;
using SAIN.Components;
using SAIN.Layers.Peace;

namespace SAIN.Components.BotController.PeacefulActions
{
    public class BotPeacefulActionController : SAINControllerBase, IBotControllerClass
    {
        public PeacefulBotFinder PeacefulBotFinder { get; }
        public PeacefulActionSet Actions { get; } = new PeacefulActionSet();

        public BotPeacefulActionController(SAINBotController controller) : base(controller)
        {
            PeacefulBotFinder = new PeacefulBotFinder(controller);
        }

        public void Init()
        {
            PeacefulBotFinder.Init();
            initActions();
        }

        private void initActions()
        {
            Actions.Add(EPeacefulAction.Gathering, new BotGatheringController(BotController, EPeacefulAction.Gathering));
            Actions.Add(EPeacefulAction.Conversation, new BotConversationController(BotController, EPeacefulAction.Conversation));
        }

        public void Update()
        {
            PeacefulBotFinder.Update();
            Actions.CheckExecute(PeacefulBotFinder.ZoneDatas);
        }

        public void Dispose()
        {
            PeacefulBotFinder.Dispose();
        }
    }
}