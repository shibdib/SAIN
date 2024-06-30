using EFT;
using SAIN.Components;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class GainSightClass : EnemyBase
    {
        public GainSightClass(Enemy enemy) : base(enemy)
        {
        }

        public float Value
        {
            get
            {
                if (_nextCheckVisTime < Time.time)
                {
                    _nextCheckVisTime = Time.time + 0.05f;
                    _gainSightModifier = GetGainSightModifier() * calcRepeatSeenCoef();
                }
                return _gainSightModifier;
            }
        }

        private float calcRepeatSeenCoef()
        {
            float result = calcVisionSpeedPositional(
                Enemy.KnownPlaces.EnemyDistanceFromLastSeen,
                _minSeenSpeedCoef,
                _minDistRepeatSeen,
                _maxDistRepeatSeen,
                SeenSpeedCheck.Vision);

            result *= calcVisionSpeedPositional(
                Enemy.KnownPlaces.EnemyDistanceFromLastHeard,
                _minHeardSpeedCoef,
                _minDistRepeatHeard,
                _maxDistRepeatHeard,
                SeenSpeedCheck.Audio);

            return result;
        }

        private enum SeenSpeedCheck
        {
            None = 0,
            Vision = 1,
            Audio = 2,
        }

        private float calcVisionSpeedPositional(float distance, float minSpeedCoef, float minDist, float maxDist, SeenSpeedCheck check)
        {
            if (distance <= minDist)
            {
                return minSpeedCoef;
            }
            if (distance >= maxDist)
            {
                return 1f;
            }

            float num = maxDist - minDist;
            float num2 = distance - minDist;
            float ratio = num2 / num;
            float result = Mathf.Lerp(minSpeedCoef, 1f, ratio);
            //Logger.LogInfo($"{check} Distance from Position: {distance} Result: {result}");
            return result;
        }

        private float _minSeenSpeedCoef = 0.01f;
        private float _minDistRepeatSeen = 1f;
        private float _maxDistRepeatSeen = 20f;

        private float _minHeardSpeedCoef = 0.25f;
        private float _minDistRepeatHeard = 1f;
        private float _maxDistRepeatHeard = 10f;

        private float _gainSightModifier;
        private float _nextCheckVisTime;

        private float calcGearMod()
        {
            return Enemy.EnemyPlayerComponent.AIData.AIGearModifier.StealthModifier(Enemy.RealDistance);
        }

        private float calcTimeModifier(bool flareEnabled)
        {
            float baseModifier = baseTimeModifier(flareEnabled);
            if (baseModifier <= 1f)
            {
                return 1f;
            }
            var flashlight = Enemy.EnemyPlayerComponent.Flashlight;
            if (flashlight.WhiteLight)
            {
                return 0.75f;
            }
            if (flashlight.Laser)
            {
                return 1f;
            }
            bool usingNVGS = BotOwner.NightVision.UsingNow;
            if (usingNVGS && (flashlight.IRLaser || flashlight.IRLight))
            {
                return 0.8f;
            }

            float max = 2f;
            float min = 1f;
            float maxDist = 150f;
            float minDist = 10f;
            float enemyDist = Enemy.RealDistance;

            if (enemyDist >= maxDist)
            {
                return baseModifier * max;
            }
            if (enemyDist < minDist)
            {
                return baseModifier;
            }

            float num = maxDist - minDist;
            float num2 = enemyDist - minDist;
            float ratio = num2 / num;
            float scaled = Mathf.Lerp(min, max, ratio);
            float result = baseModifier * scaled;

            bool moving = Enemy.Vision.EnemyVelocity > 0.1f;
            if (!moving)
            {
                result *= 2f;
            }

            if (usingNVGS)
            {
                result /= 3f;
                result = Mathf.Clamp(result, 1f, float.MaxValue);
            }

            if (_nextLogTime < Time.time)
            {
                _nextLogTime = Time.time + 30f;
                Logger.LogDebug($"Vision Time Mod Result: [{result}] : EnemyDist: [{enemyDist}] Enemy Moving? [{moving}, {Enemy.Vision.EnemyVelocity}] Base Modifier: [{baseModifier}]");
            }
            return result;
        }

        private float calcWeatherMod(bool flareEnabled)
        {
            float baseModifier = baseWeatherMod(flareEnabled);
            if (baseModifier <= 1f)
            {
                return 1f;
            }

            float max = 2f;
            float min = 1f;
            float maxDist = 125f;
            float minDist = 10f;
            float enemyDist = Enemy.RealDistance;

            if (enemyDist >= maxDist)
            {
                return baseModifier * max;
            }
            if (enemyDist < minDist)
            {
                return baseModifier;
            }

            float num = maxDist - minDist;
            float num2 = enemyDist - minDist;
            float ratio = num2 / num;
            float scaled = Mathf.Lerp(min, max, ratio);
            float result = baseModifier * scaled;

            bool moving = Enemy.Vision.EnemyVelocity > 0.1f;
            if (!moving)
            {
                result *= 2f;
            }

            if (_nextLogTime < Time.time)
            {
                Logger.LogDebug($"Vision Weather Mod Result: [{result}] : EnemyDist: [{enemyDist}] Enemy Moving? [{moving}, {Enemy.Vision.EnemyVelocity}] Base Modifier: [{baseModifier}]");
            }
            return result;
        }

        private static float _nextLogTime;

        private float baseWeatherMod(bool flareEnabled)
        {
            float weatherMod = SAINBotController.Instance.WeatherVision.GainSightModifier;
            if (flareEnabled)
            {
                return Mathf.Clamp(weatherMod / 2f, 1f, 1.5f);
            }
            return weatherMod;
        }

        private float baseTimeModifier(bool flareEnabled)
        {
            float timeMod = SAINBotController.Instance.TimeVision.TimeGainSightModifier;
            if (flareEnabled)
            {
                return Mathf.Clamp(timeMod / 2f, 1f, 1.5f);
            }
            return timeMod;
        }

        private float GetGainSightModifier()
        {
            float partMod = calcPartsMod();
            float gearMod = calcGearMod();

            bool flareEnabled = EnemyPlayer.AIData?.GetFlare == true &&
                Enemy.EnemyPlayerComponent?.Equipment.CurrentWeapon?.HasSuppressor == false;
            float weatherMod = calcWeatherMod(flareEnabled);
            float timeMod = calcTimeModifier(flareEnabled);

            float moveMod = calcMoveModifier();
            float elevMod = calcElevationModifier();
            float thirdPartyMod = calcThirdPartyMod();
            float angleMod = calcAngleMod();

            float notLookMod = 1f;
            if (!Enemy.IsAI)
                notLookMod = SAINNotLooking.GetVisionSpeedDecrease(Enemy.EnemyInfo);

            float result = 1f * partMod * gearMod * weatherMod * timeMod * moveMod * elevMod * thirdPartyMod * angleMod * notLookMod;

            //if (EnemyPlayer.IsYourPlayer && result != 1f)
            //{
            //    Logger.LogWarning($"GainSight Time Result: [{result}] : partMod {partMod} : gearMod {gearMod} : flareMod {flareMod} : moveMod {moveMod} : elevMod {elevMod} : posFlareMod {posFlareMod} : thirdPartyMod {thirdPartyMod} : angleMod {angleMod} : notLookMod {notLookMod} ");
            //}

            return result;
        }

        // private static float _nextLogTime;

        private float calcPartsMod()
        {
            if (Enemy.IsAI)
            {
                return 1f;
            }

            float max = 1.75f;

            if (!Enemy.InLineOfSight)
            {
                return max;
            }
            float partRatio = GetRatioPartsVisible(EnemyInfo, out int visibleCount);
            if (visibleCount < 1)
            {
                return max;
            }
            float min = 0.9f;
            if (partRatio >= 1f)
            {
                return min;
            }
            float result = Mathf.Lerp(max, min, partRatio);
            return result;
        }

        private static float GetRatioPartsVisible(EnemyInfo enemyInfo, out int visibleCount)
        {
            var enemyParts = enemyInfo.AllActiveParts;
            int partCount = 0;
            visibleCount = 0;

            var bodyPartData = enemyInfo.BodyData().Value;
            if (bodyPartData.IsVisible || bodyPartData.LastVisibilityCastSucceed)
            {
                visibleCount++;
            }
            partCount++;

            foreach (var part in enemyParts)
            {
                if (part.Value.LastVisibilityCastSucceed || part.Value.IsVisible)
                {
                    visibleCount++;
                }
                partCount++;
            }

            return (float)visibleCount / (float)partCount;
        }

        private float calcMoveModifier()
        {
            LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
            return Mathf.Lerp(1, globalLookSettings.SprintingVisionModifier, Enemy.Vision.EnemyVelocity);
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

        private float calcThirdPartyMod()
        {
            if (!Enemy.IsCurrentEnemy)
            {
                Enemy activeEnemy = Enemy.Bot.Enemy;
                if (activeEnemy != null)
                {
                    Vector3? activeEnemyLastKnown = activeEnemy.LastKnownPosition;
                    if (activeEnemyLastKnown != null)
                    {
                        Vector3 currentEnemyDir = (activeEnemyLastKnown.Value - Enemy.Bot.Position).normalized;
                        Vector3 myDir = Enemy.EnemyDirection.normalized;

                        float angle = Vector3.Angle(currentEnemyDir, myDir);

                        float minAngle = 20f;
                        float maxAngle = Enemy.Vision.MaxVisionAngle;
                        if (angle > minAngle && 
                            angle < maxAngle)
                        {
                            float num = maxAngle - minAngle;
                            float num2 = angle - minAngle;

                            float maxRatio = 1.5f;
                            float ratio = num2 / num;
                            float reductionMod = Mathf.Lerp(1f, maxRatio, ratio);
                            return reductionMod;
                        }
                    }
                }
            }
            return 1f;
        }

        private static bool _reduceVisionSpeedOnPeriphVis = true;
        private static float _periphVisionStart = 30f;
        private static float _maxPeriphVisionSpeedReduction = 2.5f;

        private float calcAngleMod()
        {
            if (!_reduceVisionSpeedOnPeriphVis)
            {
                return 1f;
            }

            float angle = Enemy.Vision.AngleToEnemy;

            float minAngle = _periphVisionStart;
            if (angle < minAngle)
            {
                return 1f;
            }
            float maxAngle = Enemy.Vision.MaxVisionAngle;
            float maxRatio = _maxPeriphVisionSpeedReduction;
            if (angle > maxAngle)
            {
                return maxRatio;
            }

            float angleDiff = maxAngle - minAngle;
            float enemyAngleDiff = angle - minAngle;
            float ratio = enemyAngleDiff / angleDiff;
            return Mathf.Lerp(1f, maxRatio, ratio);
        }
    }
}