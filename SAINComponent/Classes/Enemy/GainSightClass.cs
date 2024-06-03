using EFT;
using EFT.InventoryLogic;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class GainSightClass : EnemyBase
    {
        public GainSightClass(SAINEnemy enemy) : base(enemy)
        {
        }

        public float GainSightCoef
        {
            get
            {
                calcVisionModifiers();
                return _gainSightModifier;
            }
        }

        private void calcVisionModifiers()
        {
            if (_nextCheckVisTime < Time.time)
            {
                _nextCheckVisTime = Time.time + 0.1f;
                _gainSightModifier = GetGainSightModifier() * calcRepeatSeenCoef();
            }
        }

        private float calcRepeatSeenCoef()
        {
            float result = 1f;
            if (Enemy.Seen)
            {
                Vector3? lastSeenPos = Enemy.LastSeenPosition;
                if (lastSeenPos != null)
                {
                    result = calcVisionSpeedPositional(
                        lastSeenPos.Value,
                        _minSeenSpeedCoef,
                        _minDistRepeatSeen,
                        _maxDistRepeatSeen,
                        SeenSpeedCheck.Vision);
                }
            }

            if (Enemy.Heard)
            {
                Vector3? lastHeardPosition = Enemy.LastHeardPosition;
                if (lastHeardPosition != null)
                {
                    result *= calcVisionSpeedPositional(
                        lastHeardPosition.Value,
                        _minHeardSpeedCoef,
                        _minDistRepeatHeard,
                        _maxDistRepeatHeard,
                        SeenSpeedCheck.Audio);
                }
            }
            return result;
        }

        private enum SeenSpeedCheck
        {
            None = 0,
            Vision = 1,
            Audio = 2,
        }

        private float calcVisionSpeedPositional(Vector3 position, float minSpeedCoef, float minDist, float maxDist, SeenSpeedCheck check)
        {
            float distance = (position - EnemyPosition).magnitude;
            if (distance <= minDist)
            {
                return minSpeedCoef;
            }
            if (distance >= maxDist)
            {
                return 1f;
            }

            float seenSpeedDiff = maxDist - minDist;
            float distanceDiff = distance - minDist;
            float scaled = distanceDiff / seenSpeedDiff;
            float result = Mathf.Lerp(minSpeedCoef, 1f, scaled);
            //Logger.LogInfo($"{check} Distance from Position: {distance} Result: {result}");
            return result;
        }

        private float _minSeenSpeedCoef = 1E-05f;
        private float _minDistRepeatSeen = 3f;
        private float _maxDistRepeatSeen = 15f;

        private float _minHeardSpeedCoef = 0.2f;
        private float _minDistRepeatHeard = 5f;
        private float _maxDistRepeatHeard = 25f;

        private float _gainSightModifier;
        private float _nextCheckVisTime;

        private float calcGearMod()
        {
            getGear();

            if (_gearInfo != null)
                return _gearInfo.GetStealthModifier(Enemy.RealDistance);

            return 1f;
        }

        private void getGear()
        {
            if (_gearInfo == null)
                _gearInfo = SAINGearInfoHandler.GetGearInfo(EnemyPlayer);
        }

        private float calcFlareMod()
        {
            getGear();

            bool flare = EnemyPlayer.AIData.GetFlare;
            bool usingSuppressor =
                EnemyPlayer.HandsController.Item is Weapon weapon &&
                _gearInfo?.GetWeaponInfo(weapon)?.HasSuppressor == true;

            // Only apply vision speed debuff from weather if their enemy has not shot an unsuppressed weapon
            if (!flare || usingSuppressor)
            {
                return SAINPlugin.BotController.WeatherVision.InverseWeatherModifier;
            }
            return 1f;
        }

        private GearInfoContainer _gearInfo;

        private float GetGainSightModifier()
        {
            float partMod = calcPartsMod();
            float gearMod = calcGearMod();
            float flareMod = calcFlareMod();
            float moveMod = calcMoveModifier();
            float elevMod = calcElevationModifier();
            float posFlareMod = calcPosFlareMod();
            float thirdPartyMod = calcThirdPartyMod();
            float angleMod = calcAngleMod();

            float notLookMod = 1f;
            if (!Enemy.IsAI)
                notLookMod = SAINNotLooking.GetVisionSpeedDecrease(Enemy.EnemyInfo);

            float result = 1f * partMod * gearMod * flareMod * moveMod * elevMod * posFlareMod * thirdPartyMod * angleMod * notLookMod;

            // if (EnemyPlayer.IsYourPlayer &&
            //     _nextLogTime < Time.time)
            // {
            //     _nextLogTime = Time.time + 0.5f;
            //     Logger.LogWarning($"Result: [{result}] : partMod {partMod} : gearMod {gearMod} : flareMod {flareMod} : moveMod {moveMod} : elevMod {elevMod} : posFlareMod {posFlareMod} : thirdPartyMod {thirdPartyMod} : angleMod {angleMod} : notLookMod {notLookMod} ");
            // }

            return result;
        }

        // private static float _nextLogTime;

        private float calcPartsMod()
        {
            if (!Enemy.IsAI)
            {
                float partRatio = SAINVisionClass.GetRatioPartsVisible(EnemyInfo, out int visibleCount);
                float min = 0.6f;
                float max = 1.1f;
                float ratio = Mathf.Lerp(min, max, partRatio);
                return ratio;
            }
            return 1f;
        }

        private float calcMoveModifier()
        {
            if (EnemyPlayer.IsSprintEnabled)
            {
                LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
                return Mathf.Lerp(1, globalLookSettings.SprintingVisionModifier, Mathf.InverseLerp(0, 5f, EnemyPlayer.Velocity.magnitude)); // 5f is the observed max sprinting speed with gameplays (with Realism, which gives faster sprinting)
            }
            return 1f;
        }

        private float calcElevationModifier()
        {
            LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;

            Vector3 botEyeToPlayerBody = EnemyPlayer.MainParts[BodyPartType.body].Position - BotOwner.MainParts[BodyPartType.head].Position;
            var visionAngleDeviation = Vector3.Angle(new Vector3(botEyeToPlayerBody.x, 0f, botEyeToPlayerBody.z), botEyeToPlayerBody);

            if (botEyeToPlayerBody.y >= 0)
            {
                float angleFactor = Mathf.InverseLerp(0, globalLookSettings.HighElevationMaxAngle, visionAngleDeviation);
                return Mathf.Lerp(1f, globalLookSettings.HighElevationVisionModifier, angleFactor);
            }
            else
            {
                float angleFactor = Mathf.InverseLerp(0, globalLookSettings.LowElevationMaxAngle, visionAngleDeviation);
                return Mathf.Lerp(1f, globalLookSettings.LowElevationVisionModifier, angleFactor);
            }
        }

        private float calcPosFlareMod()
        {
            if (Enemy.EnemyStatus.PositionalFlareEnabled
                && Enemy.Heard
                && Enemy.TimeSinceHeard < 300f)
            {
                return 0.8f;
            }
            return 1f;
        }

        private float calcThirdPartyMod()
        {
            if (!Enemy.IsCurrentEnemy)
            {
                SAINEnemy activeEnemy = Enemy.Bot.Enemy;
                if (activeEnemy != null)
                {
                    Vector3? activeEnemyLastKnown = activeEnemy.LastKnownPosition;
                    if (activeEnemyLastKnown != null)
                    {
                        Vector3 currentEnemyDir = (activeEnemyLastKnown.Value - Enemy.Bot.Position).normalized;
                        Vector3 myDir = Enemy.EnemyDirection.normalized;

                        float angle = Vector3.Angle(currentEnemyDir, myDir);

                        float minAngle = 10f;
                        float maxAngle = Enemy.Vision.MaxVisionAngle;
                        if (angle > 10 && angle < maxAngle)
                        {
                            float num = angle - minAngle;
                            float num2 = maxAngle - minAngle;
                            float ratio = 1f - num2 / num;
                            float reductionMod = Mathf.Lerp(0.65f, 1f, ratio);
                            return reductionMod;
                        }
                    }
                }
            }
            return 1f;
        }

        private static bool _reduceVisionSpeedOnPeriphVis = true;
        private static float _periphVisionStart = 30f;
        private static float _maxPeriphVisionSpeedReduction = 0.5f;

        private float calcAngleMod()
        {
            if (!_reduceVisionSpeedOnPeriphVis)
            {
                return 1f;
            }

            if (!BotOwner.LookSensor.IsPointInVisibleSector(Enemy.EnemyPosition))
            {
                return 1f;
            }

            Vector3 myLookDir = BotOwner.LookDirection;
            myLookDir.y = 0f;
            Vector3 enemyDir = Enemy.EnemyDirection;
            enemyDir.y = 0f;
            float angle = Vector3.Angle(myLookDir, enemyDir);

            if (angle < _periphVisionStart || angle > 90)
            {
                return 1f;
            }

            float angleDiff = Enemy.Vision.MaxVisionAngle - _periphVisionStart;
            float enemyAngleDiff = angle - _periphVisionStart;
            float modifier = 1f - enemyAngleDiff / angleDiff;
            return Mathf.Lerp(_maxPeriphVisionSpeedReduction, 1f, modifier);
        }
    }
}