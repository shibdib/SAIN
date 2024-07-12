using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyVisionChecker : EnemyBase, IBotClass
    {
        public float LastCheckLookTime { get; set; }
        public Vector3 LastSeenPoint { get; private set; }
        public bool LineOfSight => EnemyParts.LineOfSight;
        public EnemyPartsClass EnemyParts { get; }

        public EnemyVisionChecker(Enemy enemy) : base(enemy)
        {
            EnemyParts = new EnemyPartsClass(enemy);
            _transform = enemy.Bot.Transform;
            _startVisionTime = Time.time + UnityEngine.Random.Range(0.0f, 0.33f);
        }

        public void Init()
        {
            SubscribeToPreset(UpdatePresetSettings);
        }

        public void Update()
        {
            EnemyParts.Update();
            Enemy.Events.OnEnemyLineOfSightChanged.CheckToggle(LineOfSight);
        }

        public void Dispose()
        {
        }

        public void CheckVision(out bool didCheck)
        {
            // staggers ai vision over a few quarters of a second
            if (!shallStart()) {
                didCheck = false;
                return;
            }

            didCheck = true;
            checkLOS(out Vector3? successPoint);
            Enemy.Events.OnEnemyLineOfSightChanged.CheckToggle(LineOfSight);
            if (successPoint != null) {
                LastSeenPoint = successPoint.Value;
            }
        }

        private bool shallStart()
        {
            if (_visionStarted) {
                return true;
            }
            if (_startVisionTime < Time.time) {
                _visionStarted = true;
                return true;
            }
            return false;
        }

        private const float MAX_LOS_RANGE_HEAD_HUMAN = 125f;
        private const float MAX_LOS_RANGE_LIMBS_AI = 200f;

        private bool checkLOS(out Vector3? seenPosition)
        {
            Vector3 lookPoint = _transform.EyePosition;
            float maxRange = AIVisionRangeLimit();

            if (Enemy.RealDistance > maxRange) {
                seenPosition = null;
                return false;
            }

            bool isAI = Enemy.IsAI;
            if (EnemyParts.CheckBodyLineOfSight(lookPoint, maxRange, out seenPosition)) {
                return true;
            }
            if (isAI && Enemy.RealDistance > MAX_LOS_RANGE_LIMBS_AI) {
                return false;
            }
            if (EnemyParts.CheckRandomPartLineOfSight(lookPoint, maxRange, out seenPosition)) {
                return true;
            }
            if (isAI) {
                return false;
            }

            // Do an extra check if the bot has this enemy as their active primary enemy or the enemy is not AI
            if (Enemy.IsCurrentEnemy &&
                EnemyParts.CheckRandomPartLineOfSight(lookPoint, maxRange, out seenPosition)) {
                return true;
            }
            if (EnemyParts.CheckHeadLineOfSight(lookPoint, MAX_LOS_RANGE_HEAD_HUMAN, out seenPosition)) {
                return true;
            }
            return false;
        }

        public float AIVisionRangeLimit()
        {
            if (!Enemy.IsAI) {
                return float.MaxValue;
            }
            var aiLimit = GlobalSettingsClass.Instance.General.AILimit;
            if (!aiLimit.LimitAIvsAIGlobal) {
                return float.MaxValue;
            }
            if (!aiLimit.LimitAIvsAIVision) {
                return float.MaxValue;
            }
            var enemyBot = Enemy.EnemyPerson.AIInfo.BotComponent;
            if (enemyBot == null) {
                // if an enemy bot is not a sain bot, but has this bot as an enemy, dont limit at all.
                if (Enemy.EnemyPerson.AIInfo.BotOwner?.Memory.GoalEnemy?.ProfileId == Bot.ProfileId) {
                    return float.MaxValue;
                }
                return getMaxVisionRange(Bot.CurrentAILimit);
            }
            else {
                if (enemyBot.Enemy?.EnemyProfileId == Bot.ProfileId) {
                    return float.MaxValue;
                }
                return getMaxVisionRange(enemyBot.CurrentAILimit);
            }
        }

        private static float getMaxVisionRange(AILimitSetting aiLimit)
        {
            switch (aiLimit) {
                default:
                    return float.MaxValue;

                case AILimitSetting.Far:
                    return _farDistance;

                case AILimitSetting.VeryFar:
                    return _veryFarDistance;

                case AILimitSetting.Narnia:
                    return _narniaDistance;
            }
        }

        public void UpdatePresetSettings(SAINPresetClass preset)
        {
            var aiLimit = preset.GlobalSettings.General.AILimit;
            _farDistance = aiLimit.MaxVisionRanges[AILimitSetting.Far];
            _veryFarDistance = aiLimit.MaxVisionRanges[AILimitSetting.VeryFar];
            _narniaDistance = aiLimit.MaxVisionRanges[AILimitSetting.Narnia];

            if (SAINPlugin.DebugMode) {
                Logger.LogDebug($"Updated AI Vision Limit Settings: [{_farDistance}, {_veryFarDistance}, {_narniaDistance}]");
            }
        }

        private bool _visionStarted;
        private float _startVisionTime;
        private PersonTransformClass _transform;
        private static float _farDistance;
        private static float _veryFarDistance;
        private static float _narniaDistance;
    }
}