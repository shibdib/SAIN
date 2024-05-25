using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Sense;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINVisionClass : SAINBase, ISAINClass
    {
        public SAINVisionClass(BotComponent component) : base(component)
        {
            FlashLightDazzle = new DazzleClass(component);
        }

        public void Init()
        {
        }

        public void Update()
        {
            var Enemy = SAINBot.Enemy;
            if (Enemy?.EnemyIPlayer != null && Enemy?.IsVisible == true)
            {
                FlashLightDazzle.CheckIfDazzleApplied(Enemy);
            }
        }

        public static float GetVisibilityModifier(Player player)
        {
            float result = 1f;
            if (player == null)
            {
                return result;
            }
            var pose = player.Pose;
            float speed = player.Velocity.magnitude / 5f; // 5f is the observed max sprinting speed with gameplays (with Realism, which gives faster sprinting)
            if (player.MovementContext.IsSprintEnabled)
            {
                Preset.GlobalSettings.LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
                result *= Mathf.Lerp(1, globalLookSettings.SprintingVisionModifier, Mathf.InverseLerp(0, 5f, player.Velocity.magnitude));
            }
            else switch (pose)
                {
                    case EPlayerPose.Stand:
                        //result *= 1.15f;
                        break;

                    case EPlayerPose.Duck:
                        result *= 0.85f;
                        break;

                    case EPlayerPose.Prone:
                        result *= 0.6f;
                        break;

                    default:
                        break;
                }

            result = result.Round100();
            //Logger.LogInfo($"Result: {result} Speed: {speed} Pose: {pose} Sprint? {player.MovementContext.IsSprintEnabled}");
            return result;
        }

        public static float GetRatioPartsVisible(EnemyInfo enemyInfo, out int visibleCount)
        {
            var enemyParts = enemyInfo.AllActiveParts;
            int partCount = enemyParts.Count;
            visibleCount = 0;

            foreach ( var part in enemyParts )
            {
                if (part.Value?.LastVisibilityCastSucceed == true)
                {
                    visibleCount++;
                }
                else if (part.Value.IsVisible == true)
                {
                    visibleCount++;
                }
            }

            if (_nextLogTime < Time.time && visibleCount > 0 && !enemyInfo.Person.IsAI)
            {
                _nextLogTime = Time.time + 0.1f;
                //Logger.LogDebug($"Visible Ratio for Enemy Parts: [{(float)visibleCount / (float)partCount}] for Bot [{enemyInfo.Owner.name}]");
            }

            return (float) visibleCount / (float) partCount;
        }

        private static float _nextLogTime;

        public void Dispose()
        {
        }

        public DazzleClass FlashLightDazzle { get; private set; }
    }
}