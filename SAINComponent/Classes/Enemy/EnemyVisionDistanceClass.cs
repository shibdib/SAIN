using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
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
            // Increase or decrease vis distance based on pose and if sprinting.
            float sprint = calcMovementMod();
            float gear = calcGearStealthMod();
            float angle = calcAngleMod();
            float flareMod = getFlare();

            SAINEnemyStatus status = Enemy.EnemyStatus;
            bool posFlare = status.PositionalFlareEnabled;
            bool shotAtMe = status.ShotAtMeRecently;

            float positionalFlareMod = posFlare ? 1.25f : 1f;
            float underFire = shotAtMe ? 1.25f : 1f;

            // Reduce vision distance for ai vs ai vision checks
            bool shallLimit = Enemy.IsAI
                && Enemy.SAINBot.CurrentAILimit != AILimitSetting.Close
                && SAINPlugin.LoadedPreset.GlobalSettings.General.LimitAIvsAI;

            float aiReduction = shallLimit ? 0.8f : 1f;

            float finalModifier = (sprint * angle * flareMod * positionalFlareMod * underFire * aiReduction) / gear;

            float defaultVisDist = BotOwner.LookSensor.VisibleDist;
            float result = (defaultVisDist * finalModifier) - defaultVisDist;

            if (EnemyPlayer.IsYourPlayer &&
                _nextLogTime < Time.time)
            {
                _nextLogTime = Time.time + 0.5f;
                Logger.LogWarning($"Result: [{result}] : Final Mod: {finalModifier} : defaultVisDist {defaultVisDist} : sprint {sprint} : gear {gear} : angle {angle} : flareMod {flareMod} : positionalFlareMod {positionalFlareMod} : underFire {underFire} : aiReduction {aiReduction} ");
            }

            return result;
        }

        private float calcMovementMod()
        {
            float velocity = Enemy.Vision.EnemyVelocity;
            float result = Mathf.Lerp(1f, _sprintMod, velocity);

            if (EnemyPlayer.IsYourPlayer && 
                _nextLogTime < Time.time)
            {
                Logger.LogWarning($"Velocity: [{velocity}] : Vision Distance mod: {result}");
            }

            return result;
        }

        private static float _nextLogTime;

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

            float minAngle = 45f;
            if (angleToEnemy > minAngle)
            {
                float num = maxAngle - minAngle;
                float num2 = angleToEnemy - minAngle;
                float ratio = 1f - num2 / num;
                float min = 0.25f;
                float max = 1f;
                float result = Mathf.InverseLerp(min, max, ratio);
                if (EnemyPlayer.IsYourPlayer &&
                    _nextLogTime < Time.time)
                {
                    Logger.LogWarning($"[{result}] : angleToEnemy: {angleToEnemy}");
                }
                return Mathf.InverseLerp(min, max, ratio);
            }
            return 1f;
        }

        private float calcGearStealthMod()
        {
            if (GearInfo == null)
            {
                GearInfo = SAINGearInfoHandler.GetGearInfo(EnemyPlayer);
            }
            if (GearInfo != null)
            {
                return GearInfo.GetStealthModifier(Enemy.RealDistance);
            }
            return 1f;
        }

        private float getFlare()
        {
            // if player shot a weapon recently
            // if player is using suppressed weapon, and has shot recently, don't increase vis distance as much.
            bool usingSuppressor = false;
            bool flareEnabled = EnemyPlayer.AIData.GetFlare;
            if (flareEnabled &&
                EnemyPlayer.HandsController.Item is Weapon weapon)
            {
                var weaponInfo = SAINGearInfoHandler.GetGearInfo(EnemyPlayer);
                usingSuppressor = weaponInfo?.GetWeaponInfo(weapon)?.HasSuppressor == true;
            }

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
        private float _calcFreq = 0.1f;
        private float _visionDist;
        private GearInfoContainer GearInfo;
    }
}