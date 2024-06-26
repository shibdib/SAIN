using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class SquadTalkSettings : SAINSettingsBase<SquadTalkSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        [Percentage]
        public float _reportReloadingChance = 33f;

        [MinMax(0.1f, 10f, 100f)]
        public float _reportReloadingFreq = 4f;

        [Percentage]
        public float _reportLostVisualChance = 40f;

        [Percentage]
        public float _reportRatChance = 33f;

        [MinMax(1f, 120f, 1f)]
        public float _reportRatTimeSinceSeen = 60f;

        [Percentage]
        public float _reportEnemyConversationChance = 10f;

        [MinMax(1f, 120f, 1f)]
        public float _reportEnemyMaxDist = 70f;

        [Percentage]
        public float _reportEnemyHealthChance = 40f;

        [MinMax(0.1f, 10f, 100f)]
        public float _reportEnemyHealthFreq = 8f;

        [Percentage]
        public float _reportEnemyKilledChance = 60f;

        [Percentage]
        public float _reportEnemyKilledSquadLeadChance = 60f;

        public bool _reportEnemyKilledToxicSquadLeader = false;

        [MinMax(1f, 120f, 1f)]
        public float _friendCloseDist = 40f;

        [Percentage]
        public float _reportFriendKilledChance = 60f;

        [Percentage]
        public float _talkRetreatChance = 60f;

        [MinMax(0.1f, 10f, 100f)]
        public float _talkRetreatFreq = 10f;

        [Hidden]
        public EPhraseTrigger _talkRetreatTrigger = EPhraseTrigger.CoverMe;

        [Hidden]
        public ETagStatus _talkRetreatMask = ETagStatus.Combat;

        public bool _talkRetreatGroupDelay = true;

        [Percentage]
        public float _underFireNeedHelpChance = 45f;

        [Hidden]
        public EPhraseTrigger _underFireNeedHelpTrigger = EPhraseTrigger.NeedHelp;

        [Hidden]
        public ETagStatus _underFireNeedHelpMask = ETagStatus.Combat;

        public bool _underFireNeedHelpGroupDelay = true;

        [MinMax(0.1f, 10f, 100f)]
        public float _underFireNeedHelpFreq = 1f;

        [Percentage]
        public float _hearNoiseChance = 40f;

        [MinMax(1f, 120f, 1f)]
        public float _hearNoiseMaxDist = 60f;

        [MinMax(0.1f, 10f, 100f)]
        public float _hearNoiseFreq = 1f;

        [Percentage]
        public float _enemyLocationTalkChance = 60f;

        [MinMax(0.1f, 10f, 100f)]
        public float _enemyLocationTalkTimeSinceSeen = 3f;

        [Percentage]
        public float _enemyNeedHelpChance = 40f;

        [MinMax(0.1f, 10f, 100f)]
        public float _enemyLocationTalkFreq = 1f;

        [MinMax(1f, 90f, 1f)]
        public float _enemyLocationBehindAngle = 90f;

        [MinMax(1f, 90f, 1f)]
        public float _enemyLocationSideAngle = 45f;

        [MinMax(1f, 90f, 1f)]
        public float _enemyLocationFrontAngle = 90f;
    }
}