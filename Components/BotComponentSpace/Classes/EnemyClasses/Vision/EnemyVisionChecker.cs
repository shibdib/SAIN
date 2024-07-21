using SAIN.Components;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.Search;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public enum ERaycastPart
    {
        Head,
        Body,
        RandomPart,
    }

    public class EnemyVisionChecker : EnemyBase, IBotClass
    {
        public float LastCheckLookTime { get; set; }
        public float LastCheckLOSTime { get; set; }
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

        public List<BodyPartRaycast> GetPartsToCheck(Vector3 origin)
        {
            _raycasts.Clear();
            ERaycastPart[] partsToCheck = getPartsToCheck(out float maxRange);
            int count = partsToCheck.Length;

            for (int i = 0; i < count; i++) {
                var type = partsToCheck[i];
                switch (type) {
                    case ERaycastPart.Body:
                        _raycasts.Add(EnemyParts.Parts[EBodyPart.Chest].GetRaycast(origin, maxRange));
                        continue;

                    case ERaycastPart.Head:
                        _raycasts.Add(EnemyParts.Parts[EBodyPart.Head].GetRaycast(origin, maxRange));
                        continue;

                    case ERaycastPart.RandomPart:
                        var part = EnemyParts.GetNextPart();
                        if (part != null) {
                            _raycasts.Add(part.GetRaycast(origin, maxRange));
                        }
                        continue;
                }
            }

            return _raycasts;
        }

        public void CheckVision(out bool didCheck)
        {
            // staggers ai vision over a few quarters of a second
            if (!shallStart()) {
                didCheck = false;
                return;
            }

            didCheck = true;
            //checkLOS(out Vector3? successPoint);
            //if (successPoint != null) {
            //    LastSeenPoint = successPoint.Value;
            //}

            Enemy.Events.OnEnemyLineOfSightChanged.CheckToggle(LineOfSight);
            bool canShoot = EnemyParts.CheckCanShoot(Enemy.Shoot.Targets.CanShootHead);
            Enemy.Events.OnEnemyCanShootChanged.CheckToggle(canShoot);
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

        private bool checkLOS()
        {
            Vector3 lookPoint = _transform.EyePosition;
            float maxRange = AIVisionRangeLimit();
            bool inSight = false;

            if (Enemy.RealDistance > maxRange) {
                return inSight;
            }

            bool isAI = Enemy.IsAI;
            if (EnemyParts.CheckBodyLineOfSight(lookPoint, maxRange)) {
                inSight = true;
            }
            if (isAI && Enemy.RealDistance > MAX_LOS_RANGE_LIMBS_AI) {
                return inSight;
            }
            if (EnemyParts.CheckRandomPartLineOfSight(lookPoint, maxRange)) {
                inSight = true;
            }
            if (isAI) {
                return inSight;
            }

            // Do an extra check if the bot has this enemy as their active primary enemy or the enemy is not AI
            if (Enemy.IsCurrentEnemy &&
                EnemyParts.CheckRandomPartLineOfSight(lookPoint, maxRange)) {
                inSight = true;
            }
            if (EnemyParts.CheckHeadLineOfSight(lookPoint, MAX_LOS_RANGE_HEAD_HUMAN)) {
                inSight = true;
            }
            return inSight;
        }

        private readonly List<BodyPartRaycast> _raycasts = new List<BodyPartRaycast>();

        private static readonly ERaycastPart[] _empty = new ERaycastPart[0];
        private static readonly ERaycastPart[] _onlyBody = new ERaycastPart[] { ERaycastPart.Body };
        private static readonly ERaycastPart[] _bodyPlus1Random = new ERaycastPart[] { ERaycastPart.Body, ERaycastPart.RandomPart };
        private static readonly ERaycastPart[] _bodyHeadPlus1Random = new ERaycastPart[] { ERaycastPart.Body, ERaycastPart.Head, ERaycastPart.RandomPart };
        private static readonly ERaycastPart[] _bodyHeadPlus2Random = new ERaycastPart[] { ERaycastPart.Body, ERaycastPart.Head, ERaycastPart.RandomPart, ERaycastPart.RandomPart };

        private ERaycastPart[] getPartsToCheck(out float maxRange)
        {
            maxRange = AIVisionRangeLimit();
            if (Enemy.RealDistance > maxRange) {
                return _empty;
            }
            bool isAI = Enemy.IsAI;
            if (isAI && Enemy.RealDistance > MAX_LOS_RANGE_LIMBS_AI) {
                return _onlyBody;
            }
            if (isAI) {
                return _bodyPlus1Random;
            }
            // Do an extra check if the bot has this enemy as their active primary enemy or the enemy is not AI
            if (Enemy.IsCurrentEnemy) {
                return _bodyHeadPlus2Random;
            }
            return _bodyHeadPlus1Random;
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