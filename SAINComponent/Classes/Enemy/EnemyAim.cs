using SAIN.Preset.GlobalSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class EnemyAim : EnemyBase
    {
        public EnemyAim(SAINEnemy enemy) : base(enemy)
        {
        }

        public float AimAndScatterMultiplier
        {
            get
            {
                float pose = PoseFactor;
                float visibility = VisibilityFactor;
                float opticMod = OpticModifier;
                float result = visibility * pose * opticMod;

                //if (EnemyPlayer.IsYourPlayer && _nextLogTime < Time.time)
                //{
                //    _nextLogTime = Time.time + 1f;
                //    Logger.LogDebug($"Aim Modifier for [{BotOwner.name}] Result: [{result}] : PoseFactor: [{pose}] : Pose Level: [{PoseLevel}] : VisFactor: [{visibility}] : Optic Mod: {opticMod} Enemy Distance: {Enemy.RealDistance}");
                //}

                return result;
            }
        }

        private static AimSettings _aimSettings => SAINPlugin.LoadedPreset.GlobalSettings.Aiming;

        private float OpticModifier
        {
            get
            {
                var gear = Enemy.Bot.Equipment.CurrentWeaponInfo;
                if (gear == null)
                {
                    return 1f;
                }

                float enemyDistance = Enemy.RealDistance;

                if (gear.HasOptic)
                {
                    if (enemyDistance >= _aimSettings.OpticFarDistance)
                    {
                        return _aimSettings.OpticFarMulti;
                    }
                    else if (enemyDistance <= _aimSettings.OpticCloseDistance)
                    {
                        return _aimSettings.OpticCloseMulti;
                    }
                }

                if (gear.HasRedDot)
                {
                    if (enemyDistance <= _aimSettings.RedDotCloseDistance)
                    {
                        return _aimSettings.RedDotCloseMulti;
                    }
                    else if (enemyDistance >= _aimSettings.RedDotFarDistance)
                    {
                        return _aimSettings.RedDotFarMulti;
                    }
                }

                if (!gear.HasRedDot && 
                    !gear.HasOptic)
                {
                    float min = _aimSettings.IronSightScaleDistanceStart;
                    if (enemyDistance < min)
                    {
                        return 1f;
                    }

                    float multi = _aimSettings.IronSightFarMulti;
                    float max = _aimSettings.IronSightScaleDistanceEnd;
                    if (enemyDistance > max)
                    {
                        return multi;
                    }
                    float num = max - min;
                    float num2 = enemyDistance - min;
                    float scaled = 1f - num2 / num;
                    float result = Mathf.Lerp(multi, 1f, scaled);
                    //Logger.LogInfo($"{result} : Dist: {enemyDistance}");
                    return result;
                }
                return 1f;
            }
        }

        private float PoseLevel => EnemyPlayer.PoseLevel;

        private float PoseFactor
        {
            get
            {
                if (EnemyPlayer.IsInPronePose)
                {
                    return _aimSettings.ProneScatterMulti;
                }

                float min = _aimSettings.PoseLevelScatterMulti;
                float max = 1f;
                float result = Mathf.Lerp(min, max, PoseLevel);

                return result;
            }
        }

        private float VisibilityFactor
        {
            get
            {
                if (_checkVisTime < Time.time)
                {
                    _checkVisTime = Time.time + _checkVisFreq;
                    _visFactor = calcVisFactor();
                }
                return _visFactor;
            }
        }

        private float calcVisFactor()
        {
            var enemyParts = Enemy.EnemyInfo.AllActiveParts;
            if (enemyParts == null || enemyParts.Count < 1)
            {
                return 1f;
            }
            int visCount = 0;
            int totalCount = 0;
            foreach (var part in enemyParts)
            {
                totalCount++;
                if (part.Value.IsVisible)
                {
                    visCount++;
                }
            }

            totalCount++;
            var bodyPart = Enemy.EnemyInfo.BodyData().Value;
            if (bodyPart.IsVisible)
            {
                visCount++;
            }

            float ratio = (float)visCount / (float)totalCount;

            float min = _aimSettings.PartVisScatterMulti;
            float max = 1f;

            float result = Mathf.Lerp(min, max, ratio);

            return result;
        }

        private float _visFactor;
        private float _checkVisTime;
        private float _checkVisFreq = 0.1f;
    }
}
