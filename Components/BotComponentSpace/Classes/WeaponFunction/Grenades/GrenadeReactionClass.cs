using EFT;
using SAIN.Components;
using SAIN.SAINComponent.SubComponents;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class GrenadeReactionClass : BotSubClass<BotGrenadeManager>, IBotClass
    {
        public GrenadeTracker DangerGrenade { get; private set; }
        public Vector3? GrenadeDangerPoint => DangerGrenade?.DangerPoint;
        public Dictionary<int, GrenadeTracker> ActiveGrenades { get; private set; } = new Dictionary<int, GrenadeTracker>();

        public GrenadeReactionClass(BotGrenadeManager grenadeClass) : base(grenadeClass)
        {
        }

        public void Init()
        {
            SAINBotController.Instance.OnGrenadeCollision += grenadeCollision;
            SAINBotController.Instance.OnGrenadeThrown += enemyGrenadeThrown;
        }

        public void Update()
        {
            foreach (var tracker in ActiveGrenades.Values)
            {
                tracker?.Update();
            }
        }

        public void Dispose()
        {
            SAINBotController.Instance.OnGrenadeCollision -= grenadeCollision;
            SAINBotController.Instance.OnGrenadeThrown -= enemyGrenadeThrown;
            foreach (var tracker in ActiveGrenades.Values)
            {
                removeGrenade(tracker?.Grenade);
            }
        }

        public void enemyGrenadeThrown(Grenade grenade, Vector3 dangerPoint, string profileId)
        {
            if (Bot == null || profileId == Bot.ProfileId)
            {
                return;
            }
            if (Bot.BotActive && 
                Bot.EnemyController.IsPlayerAnEnemy(profileId) && 
                (grenade.transform.position - Bot.Position).sqrMagnitude < 150f)
            {
                float reactionTime = getReactionTime(Bot.Info.Profile.DifficultyModifier);
                ActiveGrenades.Add(grenade.Id, new GrenadeTracker(Bot, grenade, dangerPoint, reactionTime));
                grenade.DestroyEvent += removeGrenade;
            }
        }

        private void grenadeCollision(Grenade grenade, float maxRange)
        {
            if (Bot == null || grenade.ProfileId == Bot.ProfileId)
            {
                return;
            }
            foreach (var tracker in ActiveGrenades.Values)
            {
                if (tracker.Grenade == grenade)
                {
                    tracker.CheckHeardGrenadeCollision(maxRange);
                }
            }
        }

        private void removeGrenade(Throwable grenade)
        {
            if (grenade != null)
            {
                grenade.DestroyEvent -= removeGrenade;
                ActiveGrenades.Remove(grenade.Id);
            }
        }

        private static float getReactionTime(float diffMod)
        {
            float reactionTime = 0.25f;
            reactionTime /= diffMod;
            reactionTime *= Random.Range(0.75f, 1.25f);

            float min = 0.1f;
            float max = 0.5f;

            return Mathf.Clamp(reactionTime, min, max);
        }
    }
}