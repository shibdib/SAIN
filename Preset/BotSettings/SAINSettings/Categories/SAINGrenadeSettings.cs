using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINGrenadeSettings : SAINSettingsBase<SAINGrenadeSettings>, ISAINSettings
    {
        public bool CanThrowWhileSprinting = false;

        [NameAndDescription(
            "Can Throw at Visible Enemies",
            "Toggles bots throwing grenades directly at enemies they can see.")]
        public bool ThrowAtVisibleEnemies = false;

        [NameAndDescription(
            "Since Since Enemy Seen before Throw",
            "How long it has been since a bot's enemy has been visible before a bot can consider throwing a grenade.")]
        [MinMax(0.0f, 30f, 100f)]
        public float TimeSinceSeenBeforeThrow = 3f;

        [NameAndDescription(
            "Time Before Next Throw",
            "How much time to wait before a bot is allowed to throw another grenade.")]
        [MinMax(0f, 30f, 100f)]
        public float ThrowGrenadeFrequency = 4f;

        [NameAndDescription(
            "Minimum Friendly Distance to Throw Target",
            "How close a friendly bot can be in Meters to a bot's grenade target before it stops them from throwing it.")]
        [MinMax(0.01f, 30f, 100f)]
        public float MinFriendlyDistance = 8f;

        [NameAndDescription(
            "Minimum Enemy Distance to Bot",
            "How close a enemy can be in Meters before a bot doesn't try throwing grenades.")]
        [MinMax(0.01f, 30f, 100f)]
        public float MinEnemyDistance = 8f;

        [NameAndDescription(
            "Grenade Spread",
            "How much distance, in meters, to randomize a bot's throw target position.")]
        [MinMax(0f, 5f, 100f)]
        [CopyValue]
        public float GrenadePrecision = 0.5f;

        [Percentage0to1]
        [Advanced]
        [CopyValue]
        public float MIN_THROW_DIST_PERCENT_0_1 = 0.25f;

        [Hidden]
        [JsonIgnore]
        public float CHANCE_TO_NOTIFY_ENEMY_GR_100 = 100f;

        [Hidden]
        [JsonIgnore]
        public float DELTA_GRENADE_START_TIME = 0.0f;

        [Hidden]
        [JsonIgnore]
        public int BEWARE_TYPE = 3;
    }
}