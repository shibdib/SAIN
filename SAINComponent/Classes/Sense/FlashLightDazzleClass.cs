using BepInEx.Logging;
using EFT;
using SAIN.Components;
using UnityEngine;
using static SAIN.Helpers.HelpersGClass;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Enemy;

namespace SAIN.SAINComponent.Classes.Sense
{
    public class FlashLightDazzleClass : SAINBase, ISAINClass
    {
        private TemporaryStatModifiers Modifiers = new TemporaryStatModifiers(1f, 1f, 1f, 1f, 1f);

        public FlashLightDazzleClass(BotComponent owner) : base(owner)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        private FlashLightComponent getFlashlight(SAINEnemy enemy)
        {
            if (enemy == null)
            {
                return null;
            }

            FlashLightComponent flashlight = null;
            if (enemy.EnemyPerson?.IsSAINBot == true)
            {
                flashlight = enemy.EnemyPerson?.BotComponent?.FlashLight;
            }
            else if (GameWorldInfo.IsEnemyMainPlayer(enemy))
            {
                flashlight = GameWorldHandler.SAINMainPlayer?.MainPlayerLight;
            }

            if (flashlight == null)
            {
                flashlight = enemy.EnemyPlayer?.GetComponent<FlashLightComponent>();
            }

            return flashlight;
        }

        public void CheckIfDazzleApplied(SAINEnemy enemy)
        {
            if (enemy?.EnemyIPlayer == null)
            {
                return;
            }

            FlashLightComponent flashlight = getFlashlight(enemy);
            if (flashlight != null)
            {
                bool usingNVGs = BotOwner.NightVision.UsingNow;
                if (flashlight.WhiteLight || (usingNVGs && flashlight.IRLight))
                {
                    EnemyWithFlashlight(enemy.EnemyIPlayer);
                }
                else if (flashlight.Laser || (usingNVGs && flashlight.IRLaser))
                {
                    EnemyWithLaser(enemy.EnemyIPlayer);
                }
            }
        }

        /// <summary>
        /// Checks if the enemy is within range of the flashlight and applies dazzle and gain sight modifiers if so.
        /// </summary>
        /// <param value="BotOwner">The BotOwner object.</param>
        /// <param value="person">The IPlayer object.</param>
        public void EnemyWithFlashlight(IPlayer person)
        {
            Vector3 position = BotOwner.MyHead.position;
            Vector3 weaponRoot = person.WeaponRoot.position;
            float enemyDist = (position - weaponRoot).magnitude;

            if (enemyDist < 80f)
            {
                if (FlashLightVisionCheck(person))
                {
                    if (!Physics.Raycast(weaponRoot, (position - weaponRoot).normalized, (position - weaponRoot).magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        float gainSight = GetGainSightModifier(enemyDist);

                        float dazzlemodifier = 1f;

                        if (enemyDist < MaxDazzleRange)
                            dazzlemodifier = GetDazzleModifier(person);

                        ApplyDazzle(dazzlemodifier, gainSight);
                    }
                }
            }
        }

        /// <summary>
        /// Applies dazzle to the enemy if they are within the Max dazzle range and the raycast between the BotOwner and the enemy is not blocked.
        /// </summary>
        public void EnemyWithLaser(IPlayer person)
        {
            Vector3 position = BotOwner.MyHead.position;
            Vector3 weaponRoot = person.WeaponRoot.position;
            float enemyDist = (position - weaponRoot).magnitude;

            if (enemyDist < 80f)
            {
                if (LaserVisionCheck(person))
                {
                    if (!Physics.Raycast(weaponRoot, (position - weaponRoot).normalized, (position - weaponRoot).magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        float gainSight = GetGainSightModifier(enemyDist);

                        float dazzlemodifier = 1f;
                        if (enemyDist < MaxDazzleRange)
                        {
                            dazzlemodifier = GetDazzleModifier(person);
                        }

                        ApplyDazzle(dazzlemodifier, gainSight);
                    }
                }
            }
        }

        private static float MaxDazzleRange => SAINPlugin.LoadedPreset.GlobalSettings.Flashlight.MaxDazzleRange;
        private static float Effectiveness => SAINPlugin.LoadedPreset.GlobalSettings.Flashlight.DazzleEffectiveness;

        public void ApplyDazzle(float dazzleModif, float gainSightModif)
        {
            // If modifier is already applied, don't re-apply it
            if (Modifiers.Modifiers.IsApplyed)
            {
                return;
            }

            Modifiers.Modifiers.PrecicingSpeedCoef = Mathf.Clamp(dazzleModif, 1f, 5f) * Effectiveness;
            Modifiers.Modifiers.AccuratySpeedCoef = Mathf.Clamp(dazzleModif, 1f, 5f) * Effectiveness;
            Modifiers.Modifiers.GainSightCoef = gainSightModif;
            Modifiers.Modifiers.ScatteringCoef = Mathf.Clamp(dazzleModif, 1f, 5f) * Effectiveness * 3;
            Modifiers.Modifiers.PriorityScatteringCoef = Mathf.Clamp(dazzleModif, 1f, 2.5f) * Effectiveness;

            BotOwner.Settings.Current.Apply(Modifiers.Modifiers, 0.1f);
        }

        /// <summary>
        /// Checks if the enemy is looking at the BotOwner using a flashlight vision check.
        /// </summary>
        /// <param value="BotOwner">The BotOwner to check.</param>
        /// <param value="person">The enemy to check.</param>
        /// <returns>True if the enemy is looking at the BotOwner, false otherwise.</returns>
        private bool FlashLightVisionCheck(IPlayer person)
        {
            Vector3 position = BotOwner.MyHead.position;
            Vector3 weaponRoot = person.WeaponRoot.position;

            float flashAngle = Mathf.Clamp(0.9770526f, 0.8f, 1f);
            bool enemylookatme = Vector.IsAngLessNormalized(Vector.NormalizeFastSelf(position - weaponRoot), person.LookDirection, flashAngle);

            return enemylookatme;
        }

        /// <summary>
        /// Checks if the enemy is looking at the BotOwner using a laser vision check.
        /// </summary>
        /// <param value="bot">The BotOwner to check.</param>
        /// <param value="person">The enemy to check.</param>
        /// <returns>True if the enemy is looking at the BotOwner, false otherwise.</returns>
        private bool LaserVisionCheck(IPlayer person)
        {
            Vector3 position = BotOwner.MyHead.position;
            Vector3 weaponRoot = person.WeaponRoot.position;

            float laserAngle = 0.990f;
            bool enemylookatme = Vector.IsAngLessNormalized(Vector.NormalizeFastSelf(position - weaponRoot), person.LookDirection, laserAngle);

            return enemylookatme;
        }

        /// <summary>
        /// Calculates the dazzle modifier for a given BotOwner and ActiveEnemy.
        /// </summary>
        /// <param value="___botOwner_0">The BotOwner to calculate the dazzle modifier for.</param>
        /// <param value="person">The ActiveEnemy Shining the flashlight</param>
        /// <returns>The calculated dazzle modifier.</returns>
        private float GetDazzleModifier(IPlayer person)
        {
            Vector3 position = BotOwner.MyHead.position;
            Vector3 weaponRoot = person.WeaponRoot.position;
            float enemyDist = (position - weaponRoot).magnitude;

            float dazzlemodifier = 1f - enemyDist / MaxDazzleRange;
            dazzlemodifier = 2 * dazzlemodifier + 1f;

            if (BotOwner.NightVision.UsingNow)
            {
                dazzlemodifier *= 1.5f;
            }

            return dazzlemodifier;
        }

        /// <summary>
        /// Calculates the gain sight modifier based on the Distance to the enemy.
        /// </summary>
        /// <param value="enemyDist">The Distance to the enemy.</param>
        /// <returns>The gain sight modifier.</returns>
        private float GetGainSightModifier(float enemyDist)
        {
            float gainsightdistance = Mathf.Clamp(enemyDist, 25f, 80f);
            float gainsightmodifier = gainsightdistance / 80f;
            float gainsightscaled = gainsightmodifier * 0.25f + 0.75f;
            return gainsightscaled;
        }
    }
}