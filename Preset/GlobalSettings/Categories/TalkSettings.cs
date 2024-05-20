using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class TalkSettings
    {
        [Name("Talkative Scavs")]
        [Description("When at peace, scavs will talk to each other and be noisy. Revealing their location.")]
        [Default(true)]
        public bool TalkativeScavs = true;

        [Name("Talkative PMCs")]
        [Description("When at peace, pmcs will talk to each other and be noisy. Revealing their location.")]
        [Default(false)]
        public bool TalkativePMCs = false;

        [Name("Talkative Raiders and Rogues")]
        [Description("When at peace, raiders and rogues will talk to each other and be noisy. Revealing their location.")]
        [Default(true)]
        public bool TalkativeRaidersRogues = true;

        [Name("Talkative Bosses")]
        [Description("When at peace, Bosses and boss guards will talk to each other and be noisy. Revealing their location.")]
        [Default(true)]
        public bool TalkativeBosses = true;

        [Name("Talkative Goons")]
        [Description("When at peace, The Goons will talk to each other and be noisy. Revealing their location.")]
        [Default(false)]
        public bool TalkativeGoons = false;

        [Default(85f)]
        [Percentage]
        public float FriendlyReponseChance = 85f;

        [Default(80f)]
        [Percentage]
        public float FriendlyReponseChanceAI = 80f;

        [Default(65f)]
        [Percentage]
        public float FriendlyReponseDistance = 65f;

        [Default(35f)]
        [Percentage]
        public float FriendlyReponseDistanceAI = 35f;

        [Default(1f)]
        [MinMax(0.5f, 10f)]
        public float FriendlyResponseFrequencyLimit = 1f;

        [Default(0.33f)]
        [MinMax(0.25f, 3f)]
        public float FriendlyResponseMinRandomDelay = 0.33f;

        [Default(0.75f)]
        [MinMax(0.25f, 3f)]
        public float FriendlyResponseMaxRandomDelay = 0.75f;

        [Name("Vanilla Bot Talking")]
        [Description("Disable all SAIN based handling of bot talking. No more squad chatter, no more quiet bots, completely disables SAIN's handling of bot voices")]
        [Default(false)]
        public bool DisableBotTalkPatching = false;
    }

    public class SquadTalkSettings
    {
        [Default(33f)]
        [Percentage]
        public float _reportReloadingChance = 33f;

        [Default(1f)]
        [MinMax(0.1f, 10f, 100f)]
        public float _reportReloadingFreq = 1f;

        [Default(40f)]
        [Percentage]
        public float _reportLostVisualChance = 40f;

        [Default(33f)]
        [Percentage]
        public float _reportRatChance = 33f;

        [Default(60f)]
        [MinMax(1f, 120f, 1f)]
        public float _reportRatTimeSinceSeen = 60f;

        [Default(10f)]
        [Percentage]
        public float _reportEnemyConversationChance = 10f;

        [Default(70f)]
        [MinMax(1f, 120f, 1f)]
        public float _reportEnemyMaxDist = 70f;

        [Default(40f)]
        [Percentage]
        public float _reportEnemyHealthChance = 40f;

        [Default(8f)]
        [MinMax(0.1f, 10f, 100f)]
        public float _reportEnemyHealthFreq = 8f;

        [Default(60f)]
        [Percentage]
        public float _reportEnemyKilledChance = 60f;

        [Default(60f)]
        [Percentage]
        public float _reportEnemyKilledSquadLeadChance = 60f;

        [Default(false)]
        public bool _reportEnemyKilledToxicSquadLeader = false;

        [Default(40f)]
        [MinMax(1f, 120f, 1f)]
        public float _friendCloseDist = 40f;

        [Default(60f)]
        [Percentage]
        public float _reportFriendKilledChance = 60f;

        [Default(60f)]
        [Percentage]
        public float _talkRetreatChance = 60f;

        [Default(10f)]
        [MinMax(0.1f, 10f, 100f)]
        public float _talkRetreatFreq = 10f;

        [Hidden]
        public EPhraseTrigger _talkRetreatTrigger = EPhraseTrigger.CoverMe;

        [Hidden]
        public ETagStatus _talkRetreatMask = ETagStatus.Combat;

        [Default(true)]
        public bool _talkRetreatGroupDelay = true;

        [Default(45f)]
        [Percentage]
        public float _underFireNeedHelpChance = 45f;

        [Hidden]
        public EPhraseTrigger _underFireNeedHelpTrigger = EPhraseTrigger.NeedHelp;

        [Hidden]
        public ETagStatus _underFireNeedHelpMask = ETagStatus.Combat;

        [Default(true)]
        public bool _underFireNeedHelpGroupDelay = true;

        [Default(1f)]
        [MinMax(0.1f, 10f, 100f)]
        public float _underFireNeedHelpFreq = 1f;

        [Default(40f)]
        [Percentage]
        public float _hearNoiseChance = 40f;

        [Default(60f)]
        [MinMax(1f, 120f, 1f)]
        public float _hearNoiseMaxDist = 60f;

        [Default(1f)]
        [MinMax(0.1f, 10f, 100f)]
        public float _hearNoiseFreq = 1f;

        [Default(60f)]
        [Percentage]
        public float _enemyLocationTalkChance = 60f;

        [Default(3f)]
        [MinMax(0.1f, 10f, 100f)]
        public float _enemyLocationTalkTimeSinceSeen = 3f;

        [Default(40f)]
        [Percentage]
        public float _enemyNeedHelpChance = 40f;

        [Default(1f)]
        [MinMax(0.1f, 10f, 100f)]
        public float _enemyLocationTalkFreq = 1f;

        [Default(90f)]
        [MinMax(1f, 90f, 1f)]
        public float _enemyLocationBehindAngle = 90f;

        [Default(45f)]
        [MinMax(1f, 90f, 1f)]
        public float _enemyLocationSideAngle = 45f;

        [Default(90f)]
        [MinMax(1f, 90f, 1f)]
        public float _enemyLocationFrontAngle = 90f;
    }
}