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
                float result = visibility * pose;

                if (EnemyPlayer.IsYourPlayer && _nextLogTime < Time.time)
                {
                    _nextLogTime = Time.time + 1f;
                    Logger.LogDebug($"Aim Modifier for [{BotOwner.name}] Result: [{result}] : PoseFactor: [{pose}] : Pose Level: [{PoseLevel}] : VisFactor: [{visibility}]");
                }

                //if (Enemy.BotOwner.ShootData.ShootController.IsAiming)
                //{
                    //result *= 1.25f;
                //}

                return result;
            }
        }

        private float _nextLogTime;

        private float PoseLevel => EnemyPlayer.PoseLevel;

        public float PoseFactor
        {
            get
            {
                if (EnemyPlayer.IsInPronePose)
                {
                    return 0.45f;
                }

                float min = 0.55f;
                float max = 1f;
                float result = Mathf.Lerp(min, max, PoseLevel);

                return result;
            }
        }

        public float VisibilityFactor
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

            float min = 0.5f;
            float max = 1f;

            float result = Mathf.Lerp(min, max, ratio);

            return result;
        }

        private float _visFactor;
        private float _checkVisTime;
        private float _checkVisFreq = 0.1f;
    }
}
