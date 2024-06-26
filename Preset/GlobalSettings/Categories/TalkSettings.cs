using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class TalkSettings : SAINSettingsBase<TalkSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        [Name("Talkative Scavs")]
        [Description("When at peace, scavs will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativeScavs = true;

        [Name("Talkative PMCs")]
        [Description("When at peace, pmcs will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativePMCs = false;

        [Name("Talkative Raiders and Rogues")]
        [Description("When at peace, raiders and rogues will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativeRaidersRogues = true;

        [Name("Talkative Bosses")]
        [Description("When at peace, Bosses and boss guards will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativeBosses = true;

        [Name("Talkative Goons")]
        [Description("When at peace, The Goons will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativeGoons = false;

        [Percentage]
        public float FriendlyReponseChance = 85f;

        [Percentage]
        public float FriendlyReponseChanceAI = 80f;

        [Percentage]
        public float FriendlyReponseDistance = 65f;

        [Percentage]
        public float FriendlyReponseDistanceAI = 35f;

        [MinMax(0.5f, 10f)]
        public float FriendlyResponseFrequencyLimit = 1f;

        [MinMax(0.25f, 3f)]
        public float FriendlyResponseMinRandomDelay = 0.33f;

        [MinMax(0.25f, 3f)]
        public float FriendlyResponseMaxRandomDelay = 0.75f;

        [Name("Vanilla Bot Talking")]
        [Description("Disable all SAIN based handling of bot talking. No more squad chatter, no more quiet bots, completely disables SAIN's handling of bot voices")]
        public bool DisableBotTalkPatching = false;
    }
}