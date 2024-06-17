using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class EnemyVisionDistanceClass : EnemyBase
    {
        public EnemyVisionDistanceClass(SAINEnemy enemy) : base(enemy)
        {
        }

        public float VisionDistance
        {
            get
            {
                if (_nextCalcTime < Time.time)
                {
                    _nextCalcTime = Time.time + _calcFreq;
                    _visionDist = CalcVisionDistance();
                }
                return _visionDist;
            }
        }

        private float CalcVisionDistance()
        {
            float moveMod = calcMovementMod();
            float gearMod = calcGearStealthMod();
            float angleMod = calcAngleMod();
            float flareMod = getFlare();

            SAINEnemyStatus status = Enemy.EnemyStatus;
            bool posFlare = status.PositionalFlareEnabled;
            bool shotAtMe = status.ShotAtMeRecently;

            float positionalFlareMod = posFlare ? 1.5f : 1f;
            float underFire = shotAtMe ? 1.5f : 1f;

            // Reduce vision distance for ai vs ai vision checks
            bool shallLimit = Enemy.IsAI
                && Enemy.Bot.CurrentAILimit != AILimitSetting.Close
                && SAINPlugin.LoadedPreset.GlobalSettings.General.LimitAIvsAI;

            float aiReduction = shallLimit ? 0.75f : 1f;

            float finalModifier = (moveMod * angleMod * flareMod * positionalFlareMod * underFire * aiReduction) / gearMod;

            float defaultVisDist = BotOwner.LookSensor.VisibleDist;
            float result = (defaultVisDist * finalModifier) - defaultVisDist;

            // if (EnemyPlayer.IsYourPlayer &&
            //     _nextLogTime < Time.time)
            // {
            //     _nextLogTime = Time.time + 0.5f;
            //     Logger.LogWarning($"Result: [{result}] : Final Mod: {finalModifier} : defaultVisDist {defaultVisDist} : sprint {sprint} : gear {gear} : angle {angle} : flareMod {flareMod} : positionalFlareMod {positionalFlareMod} : underFire {underFire} : aiReduction {aiReduction} ");
            // }

            return result;
        }

        private float calcMovementMod()
        {
            float velocity = Enemy.Vision.EnemyVelocity;
            float result = Mathf.Lerp(0.9f, _sprintMod, velocity);

            // if (EnemyPlayer.IsYourPlayer &&
            //     _nextLogTime < Time.time)
            // {
            //     Logger.LogWarning($"Velocity: [{velocity}] : Vision Distance mod: {result}");
            // }

            return result;
        }

        private static float _sprintMod => SAINPlugin.LoadedPreset.GlobalSettings.Look.MovementDistanceModifier;

        private float calcAngleMod()
        {
            // Reduce Bot Periph Vision
            float angleToEnemy = Enemy.Vision.AngleToEnemy;
            float maxAngle = Enemy.Vision.MaxVisionAngle;
            if (angleToEnemy > maxAngle)
            {
                return 0f;
            }

            float minAngle = 15f;
            if (angleToEnemy <= minAngle)
            {
                return 1.5f;
            }

            float num = maxAngle - minAngle;
            float num2 = angleToEnemy - minAngle;
            float ratio = 1f - num2 / num;
            float min = 0.25f;
            float max = 1.5f;
            float result = Mathf.InverseLerp(min, max, ratio);
            return result;
        }

        private float calcGearStealthMod()
        {
            return Enemy.EnemyPlayerComponent.AIData.AIGearModifier.StealthModifier(Enemy.RealDistance);
        }

        private float getFlare()
        {
            // if player shot a weapon recently
            // if player is using suppressed weapon, and has shot recently, don't increase vis distance as much.
            bool flareEnabled = EnemyPlayer.AIData.GetFlare;
            bool usingSuppressor = Enemy.EnemyPlayerComponent?.Equipment.CurrentWeapon?.HasSuppressor == true;

            float flareMod;
            if (flareEnabled && !usingSuppressor)
            {
                flareMod = 1.25f;
            }
            else if (flareEnabled && usingSuppressor)
            {
                flareMod = 1.1f;
            }
            else
            {
                flareMod = 1f;
            }
            return flareMod;
        }

        private float _nextCalcTime;
        private float _calcFreq = 0.05f;
        private float _visionDist;
    }
}