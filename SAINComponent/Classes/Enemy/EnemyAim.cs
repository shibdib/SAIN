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

                if (EnemyPlayer.IsYourPlayer && _nextLogTime < Time.time)
                {
                    _nextLogTime = Time.time + 1f;
                    Logger.LogDebug($"Aim Modifier for [{BotOwner.name}] Result: [{result}] : PoseFactor: [{pose}] : Pose Level: [{PoseLevel}] : VisFactor: [{visibility}] : Optic Mod: {opticMod} Enemy Distance: {Enemy.RealDistance}");
                }

                return result;
            }
        }

        private float OpticModifier
        {
            get
            {
                var gear = Enemy.SAINBot.Equipment.CurrentWeaponInfo;
                if (gear == null)
                {
                    return 1f;
                }
                float enemyDistance = Enemy.RealDistance;
                bool isAiming = BotOwner.WeaponManager?.ShootController?.IsAiming == true;
                if (gear.HasOptic)
                {
                    if (enemyDistance >= 100f && 
                        isAiming)
                    {
                        return 1.2f;
                    }
                    else if (enemyDistance <= 75f)
                    {
                        return 0.8f;
                    }
                }
                if (gear.HasRedDot)
                {
                    if (enemyDistance <= 75f && 
                        isAiming)
                    {
                        return 1.15f;
                    }
                    else if (enemyDistance >= 125f)
                    {
                        return 0.8f;
                    }
                }

                if (!gear.HasRedDot && 
                    !gear.HasOptic)
                {
                    if (enemyDistance >= 125f)
                    {
                        return 0.75f;
                    }
                    if (enemyDistance >= 100f)
                    {
                        return 0.825f;
                    }
                    if (enemyDistance >= 75f)
                    {
                        return 0.9f;
                    }
                }
                return 1f;
            }
        }


        private float _nextLogTime;

        private float PoseLevel => EnemyPlayer.PoseLevel;

        private float PoseFactor
        {
            get
            {
                if (EnemyPlayer.IsInPronePose)
                {
                    return 0.55f;
                }

                float min = 0.65f;
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

            float min = 0.65f;
            float max = 1f;

            float result = Mathf.Lerp(min, max, ratio);

            return result;
        }

        private float _visFactor;
        private float _checkVisTime;
        private float _checkVisFreq = 0.1f;
    }
}
