using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class TalkSettings : SAINSettingsBase<TalkSettings>, ISAINSettings
    {
        [Category("Peaceful Talk")]
        [Name("Talkative Scavs")]
        [Description("When at peace, scavs will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativeScavs = true;

        [Category("Peaceful Talk")]
        [Name("Talkative PMCs")]
        [Description("When at peace, pmcs will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativePMCs = false;

        [Category("Peaceful Talk")]
        [Name("Talkative Raiders and Rogues")]
        [Description("When at peace, raiders and rogues will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativeRaidersRogues = true;

        [Category("Peaceful Talk")]
        [Name("Talkative Bosses")]
        [Description("When at peace, Bosses and boss guards will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativeBosses = true;

        [Category("Peaceful Talk")]
        [Name("Talkative Goons")]
        [Description("When at peace, The Goons will talk to each other and be noisy. Revealing their location.")]
        public bool TalkativeGoons = false;

        [Category("Friendly Response")]
        [Percentage]
        public float FriendlyReponseChance = 85f;

        [Category("Friendly Response")]
        [Percentage]
        public float FriendlyReponseChanceAI = 80f;

        [Category("Friendly Response")]
        [Percentage]
        public float FriendlyReponseDistance = 65f;

        [Category("Friendly Response")]
        [Percentage]
        public float FriendlyReponseDistanceAI = 35f;

        [Category("Friendly Response")]
        [MinMax(0.5f, 10f)]
        public float FriendlyResponseFrequencyLimit = 1f;

        [Category("Friendly Response")]
        [MinMax(0.25f, 3f)]
        public float FriendlyResponseMinRandomDelay = 0.33f;

        [Category("Friendly Response")]
        [MinMax(0.25f, 3f)]
        public float FriendlyResponseMaxRandomDelay = 0.75f;

        [Name("Vanilla Bot Talking")]
        [Description("Disable all SAIN based handling of bot talking. No more squad chatter, no more quiet bots, completely disables SAIN's handling of bot voices")]
        public bool DisableBotTalkPatching = false;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}