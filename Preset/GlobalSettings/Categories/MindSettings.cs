using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Helpers;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings.Categories
{
    public class MindSettings : SAINSettingsBase<MindSettings>, ISAINSettings
    {
        public override void Update()
        {
            MaximumDistanceToBeSneaky_SQR = MaximumDistanceToBeSneaky.Sqr();
            MaxSuppressionDistance_SQR = MaxSuppressionDistance.Sqr();
            MaxUnderFireDistance_SQR = MaxUnderFireDistance.Sqr();
        }

        [Name("Force Single Personality For All Bots")]
        [Description("All Spawned SAIN bots will be assigned the selected Personality, if any are set to true, no matter what.")]
        public Dictionary<EPersonality, bool> ForcePersonality = new Dictionary<EPersonality, bool>()
        {
            { EPersonality.Wreckless, false},
            { EPersonality.GigaChad, false },
            { EPersonality.Chad, false },
            { EPersonality.SnappingTurtle, false},
            { EPersonality.Rat, false },
            { EPersonality.Coward, false },
            { EPersonality.Timmy, false},
            { EPersonality.Normal, false},
        };

        [MinMax(0.1f, 5f, 100f)]
        public float GlobalAggression = 1f;

        [Name("Bots can use Stealth Search")]
        [Description("If a bot thinks he was not heard, and isn't currently fighting an enemy, they can decide to be stealthy while they seek out an enemy, if they are inside a building.")]
        public bool SneakyBots = true;

        [Name("Only Sneaky Personalities can be Stealthy")]
        [Description("Only allow sneaky personality types (rat, snapping turtle) to be stealthy while searching for an enemy, ignored if Stealth Search is disabled above")]
        public bool OnlySneakyPersonalitiesSneaky = true;

        [Description("The distance from a bot's search destination that they will begin to be stealthy, if enabled.")]
        [Advanced]
        [MinMax(5f, 200f, 10f)]
        public float MaximumDistanceToBeSneaky = 80f;

        [JsonIgnore]
        [Hidden]
        public float MaximumDistanceToBeSneaky_SQR;

        [Description("The maximum distance between the bullet, and a bot's head to be considered Suppressing fire.")]
        [MinMax(1f, 30f, 10f)]
        [Advanced]
        public float MaxSuppressionDistance = 10f;

        [JsonIgnore]
        [Hidden]
        public float MaxSuppressionDistance_SQR;

        [Description("The maximum distance between the bullet, and a bot's head to be considered under active enemy fire.")]
        [MinMax(0.1f, 20f, 10f)]
        [Advanced]
        public float MaxUnderFireDistance = 2f;

        [JsonIgnore]
        [Hidden]
        public float MaxUnderFireDistance_SQR;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}