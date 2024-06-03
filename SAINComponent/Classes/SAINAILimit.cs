using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SAIN.SAINComponent.BotComponent;
using UnityEngine.UIElements;
using UnityEngine;
using Comfort.Common;
using SAIN.Components;

namespace SAIN.SAINComponent.Classes
{
    public class SAINAILimit : SAINBase, ISAINClass
    {
        public SAINAILimit(BotComponent sain) : base(sain)
        {
        }

        public void Init() { }

        public void Update()
        {
            checkAILimit();
        }

        public void Dispose() { }

        public AILimitSetting CurrentAILimit { get; private set; }

        private void checkAILimit()
        {
            var result = CurrentAILimit;
            if (Bot.EnemyController.ActiveHumanEnemy)
            {
                result = AILimitSetting.Close;
            }
            else if (_checkDistanceTime < Time.time)
            {
                _checkDistanceTime = Time.time + 3f;
                var gameWorld = GameWorldHandler.SAINGameWorld;
                if (gameWorld != null && gameWorld.PlayerTracker.FindClosestHumanPlayer(out float closestPlayerSqrMag, Bot.Position) != null)
                {
                    result = CheckDistances(closestPlayerSqrMag);
                }
            }
            CurrentAILimit = result;
        }

        private AILimitSetting CheckDistances(float closestPlayerSqrMag)
        {
            const float NarniaDist = 600;
            const float VeryFarDist = 400;
            const float FarDist = 200f;

            if (closestPlayerSqrMag < FarDist * FarDist)
            {
                return AILimitSetting.Close;
            }
            else if (closestPlayerSqrMag < VeryFarDist * VeryFarDist)
            {
                return AILimitSetting.Far;
            }
            else if (closestPlayerSqrMag < NarniaDist * NarniaDist)
            {
                return AILimitSetting.VeryFar;
            }
            else
            {
                return AILimitSetting.Narnia;
            }
        }

        private float _checkDistanceTime;
    }
}
