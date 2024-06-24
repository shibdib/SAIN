using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Components;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using System.Text;

namespace SAIN.Layers
{
    public abstract class SAINLayer : CustomLayer
    {
        public static string BuildLayerName(string name)
        {
            return $"SAIN : {name}";
        }

        public SAINLayer(BotOwner botOwner, int priority, string layerName) : base(botOwner, priority)
        {
            LayerName = layerName;
        }

        private readonly string LayerName;

        public override string GetName() => LayerName;

        public SAINBotController BotController => SAINBotController.Instance;

        public BotComponent Bot
        {
            get
            {
                if (_bot == null && 
                    BotController.GetSAIN(BotOwner, out var bot))
                {
                    _bot = bot;
                }
                if (_bot == null)
                {
                    Logger.LogWarning($"Had to getcomponent to find bot component?");
                    _bot = BotOwner.GetComponent<BotComponent>();
                }
                return _bot;
            }
        }

        private BotComponent _bot;
        

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            if (Bot != null)
            {
                DebugOverlay.AddBaseInfo(Bot, BotOwner, stringBuilder);
            }
        }
    }
}