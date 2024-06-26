using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings.Categories
{
    public class MindSettings : SAINSettingsBase<MindSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        [MinMax(0.1f, 5f, 100f)]
        public float GlobalAggression = 1f;

        [Name("Bots can use Stealth Search")]
        [Description("If a bot thinks he was not heard, and isn't currently fighting an enemy, they can decide to be stealthy while they seek out an enemy, if they are inside a building.")]
        public bool SneakyBots = true;

        [Name("Only Sneaky Personalities can be Stealthy")]
        [Description("Only allow sneaky personality types (rat, snapping turtle) to be stealthy while searching for an enemy, ignored if Stealth Search is disabled above")]
        public bool OnlySneakyPersonalitiesSneaky = false;

        [Description("The distance from a bot's search destination that they will begin to be stealthy, if enabled.")]
        [Advanced]
        [MinMax(5f, 200f, 10f)]
        public float MaximumDistanceToBeSneaky = 80f;

        [Description("The maximum distance between the bullet, and a bot's head to be considered Suppressing fire.")]
        [MinMax(1f, 30f, 10f)]
        [Advanced]
        public float MaxSuppressionDistance = 10f;

        [Description("The maximum distance between the bullet, and a bot's head to be considered under active enemy fire.")]
        [MinMax(0.1f, 20f, 10f)]
        [Advanced]
        public float MaxUnderFireDistance = 2f;
    }
}