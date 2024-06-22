using Comfort.Common;
using EFT;
using EFT.Airdrop;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static EFT.Interactive.BetterPropagationGroups;

namespace SAIN.SAINComponent.Classes
{
    public class SAINHearingSensorClass : SAINBase, ISAINClass
    {
        public SAINHearingSensorClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            SAINBotController.Instance.AISoundPlayed += soundHeard;
            SAINBotController.Instance.BulletImpact += bulletImpacted;
        }

        private void bulletImpacted(EftBulletClass bullet)
        {
            if (!Bot.BotActive)
                return;

            if (_nextHearImpactTime > Time.time)
                return;

            if (Bot.HasEnemy)
                return;

            var player = bullet.Player?.iPlayer;
            if (player == null)
                return;

            if (!Bot.EnemyController.Enemies.TryGetValue(player.ProfileId, out var enemy))
                return;

            if (!enemy.IsValid)
                return;

            if (Bot.PlayerComponent.AIData.PlayerLocation.InBunker != enemy.EnemyPlayerComponent.AIData.PlayerLocation.InBunker)
                return;

            float distance = (bullet.CurrentPosition - Bot.Position).sqrMagnitude;
            if (distance > IMPACT_MAX_HEAR_DISTANCE)
            {
                _nextHearImpactTime = Time.time + IMPACT_HEAR_FREQUENCY_FAR;
                return;
            }

            _nextHearImpactTime = Time.time + IMPACT_HEAR_FREQUENCY;

            float dispersion = distance / IMPACT_DISPERSION;
            Vector3 random = UnityEngine.Random.onUnitSphere;
            random.y = 0;
            random = random.normalized * dispersion;
            Vector3 estimatedPos = enemy.EnemyPosition + random;
            enemy.SetHeardStatus(true, estimatedPos, SAINSoundType.BulletImpact, true);
        }

        private float _nextHearImpactTime;
        private const float IMPACT_HEAR_FREQUENCY = 0.5f;
        private const float IMPACT_HEAR_FREQUENCY_FAR = 0.05f;
        private const float IMPACT_MAX_HEAR_DISTANCE = 50f * 50f;
        private const float IMPACT_DISPERSION = 5f * 5f;

        private void soundHeard(SAINSoundType soundType, Vector3 soundPosition, PlayerComponent playerComponent, float power, float volume)
        {
            if (volume <= 0)
                return;

            if (!Bot.EnemyController.IsPlayerAnEnemy(playerComponent.ProfileId))
                return;

            if (shallIgnore(playerComponent))
                return;

            bool gunshot = soundType.IsGunShot();

            float sqrMagnitude = (soundPosition - Bot.Transform.HeadPosition).sqrMagnitude;

            if (!gunshot && power * power < sqrMagnitude)
                return;

            if (shallLimitAI(playerComponent, sqrMagnitude))
                return;

            float baseRange = power * volume;
            float bunkerVolumeReduction = calcBunkerVolumeReduction(playerComponent);
            float range = baseRange * bunkerVolumeReduction;

            float maxAffectDist = HEAR_MODIFIER_MAX_AFFECT_DIST / 2;
            if (range > maxAffectDist)
            {
                float rangeModifier = getSoundRangeModifier(sqrMagnitude, gunshot);
                range = Mathf.Clamp(range * rangeModifier, maxAffectDist, float.MaxValue);
            }

            if (!gunshot && range * range < sqrMagnitude)
                return;

            // The sound originated from somewhere far from the player's position, typically from a grenade explosion, which is handled elsewhere
            if ((playerComponent.Position - soundPosition).sqrMagnitude > 5f * 5f)
                return;

            float distance = Mathf.Sqrt(sqrMagnitude);
            bool wasHeard = doIHearSound(playerComponent, soundPosition, range, soundType, distance, gunshot, out bool visibleSource);
            bool bulletFelt = gunshot && doIFeelBullet(playerComponent, soundPosition, distance);

            if (!wasHeard && !bulletFelt)
            {
                return;
            }

            Vector3 randomizedSoundPos;
            if (bulletFelt &&
                DidShotFlyByMe(playerComponent, distance, SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxSuppressionDistance, out Vector3 projectionPoint, out float projectionPointDist))
            {
                float addDisp = wasHeard ? 1f : 2f;
                randomizedSoundPos = getSoundDispersion(playerComponent, soundPosition, soundType, distance, addDisp);
                Bot.StartCoroutine(delayReact(randomizedSoundPos, range, soundType, playerComponent.IPlayer, distance, projectionPoint, projectionPointDist));
                return;
            }

            if (!wasHeard)
            {
                return;
            }
            if (gunshot && 
                !shallChaseGunshot(distance, soundPosition))
            {
                return;
            }

            randomizedSoundPos = getSoundDispersion(playerComponent, soundPosition, soundType, distance, 1f);
            Bot.StartCoroutine(delayAddSearch(randomizedSoundPos, range, soundType, playerComponent.IPlayer, distance));
        }

        private float calcBunkerVolumeReduction(PlayerComponent playerComponent)
        {
            bool botinBunker = Bot.PlayerComponent.AIData.PlayerLocation.InBunker;
            bool playerinBunker = playerComponent.AIData.PlayerLocation.InBunker;
            if (botinBunker != playerinBunker)
            {
                return 0.2f;
            }
            else if (botinBunker)
            {
                float botDepth = Bot.PlayerComponent.AIData.PlayerLocation.BunkerDepth;
                float playerDepth = playerComponent.AIData.PlayerLocation.BunkerDepth;
                float diff = Mathf.Abs(botDepth - playerDepth);
                if (diff >= 0)
                {
                    return 0.66f;
                }
            }
            return 1f;
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            SAINBotController.Instance.AISoundPlayed -= soundHeard;
            SAINBotController.Instance.BulletImpact -= bulletImpacted;
        }

        private bool shallLimitAI(PlayerComponent playerComponent, float sqrMagnitude)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.General.LimitAIvsAI)
                return false;

            if (!playerComponent.IsAI)
                return false;

            var currentLimit = Bot.CurrentAILimit;
            if (currentLimit == AILimitSetting.Close)
                return false;

            if (Bot.Enemy?.EnemyProfileId == playerComponent.ProfileId)
                return false;

            if (playerComponent.IsSAINBot && playerComponent.BotComponent?.CurrentAILimit == AILimitSetting.Close)
                return false;

            float maxRange = getMaxRange(currentLimit);
            if (sqrMagnitude > maxRange * maxRange)
            {
                return false;
            }

            return true;
        }

        private float getMaxRange(AILimitSetting aiLimit)
        {
            switch (aiLimit)
            {
                case AILimitSetting.Far:
                case AILimitSetting.VeryFar:
                    return SAINPlugin.LoadedPreset.GlobalSettings.General.LimitAIvsAIMaxAudioRange;

                case AILimitSetting.Narnia:
                    return SAINPlugin.LoadedPreset.GlobalSettings.General.LimitAIvsAIMaxAudioRangeVeryFar;

                default:
                    return float.MaxValue;
            }
        }

        private bool shallIgnore(PlayerComponent playerComponent)
        {
            return !Bot.BotActive ||
                Bot.GameIsEnding ||
                Bot.ProfileId == playerComponent.ProfileId;
        }

        private readonly Dictionary<string, SAINSoundCollection> SoundsHeardFromPlayer = new Dictionary<string, SAINSoundCollection>();

        private float getSoundRangeModifier(float sqrMagnitude, bool isGunshot)
        {
            float modifier = 1f;
            if (!isGunshot)
            {
                modifier *= GlobalSettings.Hearing.FootstepAudioMultiplier;
            }
            else
            {
                modifier *= GlobalSettings.Hearing.GunshotAudioMultiplier;
            }
            modifier *= Bot.Info.FileSettings.Core.HearingSense;

            if (!isGunshot && sqrMagnitude > HEAR_MODIFIER_MAX_AFFECT_DIST)
            {
                if (!Bot.PlayerComponent.Equipment.GearInfo.HasEarPiece)
                {
                    modifier *= HEAR_MODIFIER_NO_EARS;
                }
                if (Bot.PlayerComponent.Equipment.GearInfo.HasHeavyHelmet)
                {
                    modifier *= HEAR_MODIFIER_HEAVY_HELMET;
                }
                if (Bot.Memory.Health.Dying &&
                    !Bot.Memory.Health.OnPainKillers)
                {
                    modifier *= HEAR_MODIFIER_DYING;
                }
                if (Player.IsSprintEnabled)
                {
                    modifier *= HEAR_MODIFIER_SPRINT;
                }
                if (Player.HeavyBreath)
                {
                    modifier *= HEAR_MODIFIER_HEAVYBREATH;
                }
            }

            modifier = Mathf.Clamp(modifier, HEAR_MODIFIER_MIN_CLAMP, HEAR_MODIFIER_MAX_CLAMP);
            return modifier;
        }

        private const float HEAR_MODIFIER_MAX_AFFECT_DIST = 15f * 15f;
        private const float HEAR_MODIFIER_NO_EARS = 0.65f;
        private const float HEAR_MODIFIER_HEAVY_HELMET = 0.8f;
        private const float HEAR_MODIFIER_DYING = 0.8f;
        private const float HEAR_MODIFIER_SPRINT = 0.85f;
        private const float HEAR_MODIFIER_HEAVYBREATH = 0.65f;
        private const float HEAR_MODIFIER_MIN_CLAMP = 0.2f;
        private const float HEAR_MODIFIER_MAX_CLAMP = 5f;

        private bool doIFeelBullet(PlayerComponent playerComponent, Vector3 pos, float distance)
        {
            if (distance > BULLET_FEEL_MAX_DIST)
            {
                return false;
            }
            Vector3 directionToBot = (Bot.Position - pos).normalized;
            float dot = Vector3.Dot(playerComponent.LookDirection, directionToBot);
            return dot >= BULLET_FEEL_DOT_THRESHOLD;
        }

        const float BULLET_FEEL_DOT_THRESHOLD = 0.75f;
        const float BULLET_FEEL_MAX_DIST = 500f;

        private bool shallChaseGunshot(float distance, Vector3 soundPosition)
        {
            var searchSettings = Bot.Info.PersonalitySettings.Search;
            if (searchSettings.WillChaseDistantGunshots)
            {
                return true;
            }
            if (distance >= searchSettings.AudioStraightDistanceToIgnore)
            {
                return false;
            }
            if (isPathTooFar(soundPosition, searchSettings.AudioPathLengthDistanceToIgnore))
            {
                return false;
            }
            return true;
        }

        private bool isPathTooFar(Vector3 soundPosition, float maxPathLength)
        {
            NavMeshPath path = new NavMeshPath();
            Vector3 sampledPos = samplePos(soundPosition);
            if (NavMesh.CalculatePath(samplePos(Bot.Position), sampledPos, -1, path))
            {
                float pathLength = path.CalculatePathLength();
                if (path.status == NavMeshPathStatus.PathPartial)
                {
                    Vector3 directionFromEndOfPath = path.corners[path.corners.Length - 1] - sampledPos;
                    pathLength += directionFromEndOfPath.magnitude;
                }
                return pathLength >= maxPathLength;
            }
            return false;
        }

        private Vector3 samplePos(Vector3 pos)
        {
            if (NavMesh.SamplePosition(pos, out var hit, 1f, -1))
            {
                return hit.position;
            }
            return pos;
        }

        private const float _speedOfSound = 343;

        private IEnumerator baseHearDelay(float distance)
        {
            float delay = distance / _speedOfSound;
            if (Bot?.EnemyController?.NoEnemyContact == true)
            {
                delay += SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseHearingDelayAtPeace;
            }
            else
            {
                delay += SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseHearingDelayWithEnemy;
            }
            delay = Mathf.Clamp(delay - 0.1f, 0f, 5f);
            yield return new WaitForSeconds(delay);
        }

        private IEnumerator delayReact(Vector3 soundPos, float power, SAINSoundType type, IPlayer person, float distance, Vector3 projectionPoint, float projectionPointDist)
        {
            yield return baseHearDelay(distance);

            if (Bot != null &&
                person != null &&
                Bot.Player?.HealthController?.IsAlive == true &&
                person.HealthController?.IsAlive == true)
            {
                if (projectionPointDist <= SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxUnderFireDistance)
                {
                    BotOwner?.HearingSensor?.OnEnemySounHearded?.Invoke(soundPos, distance, type.Convert());
                    Bot.Memory.SetUnderFire(person, soundPos);
                }
                Bot.Suppression.AddSuppression(projectionPointDist);
                Enemy enemy = Bot.EnemyController.CheckAddEnemy(person);
                if (enemy != null)
                {
                    enemy.SetEnemyAsSniper(distance > 100f);
                    enemy.EnemyStatus.ShotAtMeRecently = true;
                }
                Bot?.Squad?.SquadInfo?.AddPointToSearch(soundPos, power, Bot, type, person);
                CheckCalcGoal();
            }
        }

        private IEnumerator delayAddSearch(Vector3 vector, float power, SAINSoundType type, IPlayer person, float shooterDistance)
        {
            yield return baseHearDelay(shooterDistance);

            if (Bot?.Player?.HealthController?.IsAlive == true &&
                person?.HealthController?.IsAlive == true)
            {
                Bot?.Squad?.SquadInfo?.AddPointToSearch(vector, power, Bot, type, person);
                CheckCalcGoal();
            }
        }

        private Vector3 getSoundDispersion(PlayerComponent playerComponent, Vector3 soundPosition, SAINSoundType soundType, float distance, float addDispersion)
        {
            float baseDispersion = getBaseDispersion(distance, soundType);
            float dispersionMod = getDispersionModifier(soundPosition) * addDispersion;
            float finalDispersion = baseDispersion * dispersionMod;
            float min = distance < 10 ? 0f : 0.5f;
            Vector3 randomdirection = getRandomizedDirection(finalDispersion, min);

            if (SAINPlugin.DebugSettings.DebugHearing)
                Logger.LogDebug($"Dispersion: [{randomdirection.magnitude}] Distance: [{distance}] Base Dispersion: [{baseDispersion}] DispersionModifier [{dispersionMod}] Final Dispersion: [{finalDispersion}] : SoundType: [{soundType}]");

            return soundPosition + randomdirection;
        }

        private float getBaseDispersion(float shooterDistance, SAINSoundType soundType)
        {
            const float dispSuppGun = 12.5f;
            const float dispGun = 17.5f;
            const float dispStep = 6.25f;

            float dispersion;
            switch (soundType)
            {
                case SAINSoundType.Gunshot:
                    dispersion = shooterDistance / dispGun;
                    break;

                case SAINSoundType.SuppressedGunShot:
                    dispersion = shooterDistance / dispSuppGun;
                    break;

                default:
                    dispersion = shooterDistance / dispStep;
                    break;
            }

            return dispersion;
        }

        private float getDispersionModifier(Vector3 soundPosition)
        {
            float dispersionModifier = 1f;
            float dotProduct = Vector3.Dot(Bot.LookDirection.normalized, (soundPosition - Bot.Position).normalized);
            // The sound originated from behind us
            if (dotProduct <= 0f)
            {
                dispersionModifier = Mathf.Lerp(1.5f, 0f, dotProduct + 1f);
            }
            // The sound originated from infront of me.
            if (dotProduct > 0)
            {
                dispersionModifier = Mathf.Lerp(1f, 0.5f, dotProduct);
            }
            //Logger.LogInfo($"Dispersion Modifier for Sound [{dispersionModifier}] Dot Product [{dotProduct}]");
            return dispersionModifier;
        }

        private float modifyDispersion(float dispersion, IPlayer person, out int soundCount)
        {
            // If a bot is hearing multiple sounds from a iPlayer, they will now be more accurate at finding the source soundPosition based on how many sounds they have heard from this particular iPlayer
            soundCount = 0;
            float finalDispersion = dispersion;
            if (person != null && SoundsHeardFromPlayer.ContainsKey(person.ProfileId))
            {
                SAINSoundCollection collection = SoundsHeardFromPlayer[person.ProfileId];
                if (collection != null)
                {
                    soundCount = collection.Count;
                }
            }
            if (soundCount > 1)
            {
                finalDispersion = (dispersion / soundCount).Round100();
            }
            return finalDispersion;
        }

        private Vector3 getRandomizedDirection(float dispersion, float min = 0.5f)
        {
            float randomHeight = dispersion / 10f;
            float randomX = UnityEngine.Random.Range(-dispersion, dispersion);
            float randomY = UnityEngine.Random.Range(-randomHeight, randomHeight);
            float randomZ = UnityEngine.Random.Range(-dispersion, dispersion);
            Vector3 randomdirection = new Vector3(randomX, 0, randomZ);

            if (min > 0 && randomdirection.sqrMagnitude < min * min)
            {
                randomdirection = Vector3.Normalize(randomdirection) * min;
            }
            return randomdirection;
        }

        private void CheckCalcGoal()
        {
            if (BotOwner.Memory.GoalEnemy == null)
            {
                try
                {
                    BotOwner.BotsGroup.CalcGoalForBot(BotOwner);
                }
                catch { }
            }
        }

        private Vector3 GetEstimatedPoint(Vector3 source, float distance)
        {
            Vector3 randomPoint = UnityEngine.Random.onUnitSphere;
            randomPoint.y = 0;
            randomPoint *= (distance / 10f);
            return source + randomPoint;
        }

        private bool DidShotFlyByMe(PlayerComponent playerComponent, float realDistance, float maxDist, out Vector3 projectionPoint, out float projectionPointDist)
        {
            float maxDistSqr = maxDist * maxDist;
            projectionPoint = calcProjectionPoint(playerComponent, realDistance);
            projectionPointDist = (projectionPoint - Bot.Position).sqrMagnitude;

            bool shotNearMe = projectionPointDist <= maxDistSqr;
            if (!shotNearMe)
            {
                return false;
            }

            // if the direction the player shot hits a wall, and the point that they hit is further than our input max distance, the shot did not fly by the bot.
            Vector3 firePort = playerComponent.Transform.WeaponFirePort;
            Vector3 direction = projectionPoint - firePort;
            if (Physics.Raycast(firePort, direction, out var hit, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask) &&
                (hit.point - Bot.Position).sqrMagnitude > maxDistSqr)
            {
                return false;
            }

            if (SAINPlugin.DebugSettings.DebugHearing)
            {
                DebugGizmos.Sphere(projectionPoint, 0.25f, Color.red, true, 60f);
                DebugGizmos.Line(projectionPoint, firePort, Color.red, 0.1f, true, 60f, true);
            }

            projectionPointDist = Mathf.Sqrt(projectionPointDist);
            return true;
        }

        public Vector3 calcProjectionPoint(PlayerComponent playerComponent, float realDistance)
        {
            Vector3 weaponPointDir = playerComponent.Transform.WeaponPointDirection;
            Vector3 shotPos = playerComponent.Transform.WeaponFirePort;
            Vector3 projectionPoint = shotPos + (weaponPointDir * realDistance);
            return projectionPoint;
        }

        private bool doIDetectFootsteps(float distance, PlayerComponent player)
        {
            bool hasheadPhones = Bot.PlayerComponent.Equipment.GearInfo.HasEarPiece;

            float closehearing = hasheadPhones ? 3f : 0f;
            if (distance <= closehearing)
            {
                return true;
            }

            float farhearing = hasheadPhones ? SAINPlugin.LoadedPreset.GlobalSettings.Hearing.MaxFootstepAudioDistance : SAINPlugin.LoadedPreset.GlobalSettings.Hearing.MaxFootstepAudioDistanceNoHeadphones;
            if (distance > farhearing)
            {
                return false;
            }

            // Random chance to hear at any range within maxdistance if a bot is stationary or moving slow

            float minimumChance = 0f;

            if (hasheadPhones)
            {
                if (distance < farhearing * 0.66f)
                {
                    minimumChance += 10f;
                }
                else
                {
                    minimumChance += 5f;
                }
            }

            if (Player.Velocity.magnitude < 0.5f)
            {
                minimumChance += 5f;
            }

            if (Bot.HasEnemy &&
                Bot.Enemy.EnemyProfileId == player.ProfileId)
            {
                minimumChance += 25f;
            }

            float num = farhearing - closehearing;
            float num2 = distance - closehearing;
            float chanceToHear = 1f - num2 / num;
            chanceToHear *= 100f;

            chanceToHear = Mathf.Clamp(chanceToHear, minimumChance, 100f);
            return EFTMath.RandomBool(chanceToHear);
        }

        private static bool isGunshot(SAINSoundType soundType)
        {
            switch (soundType)
            {
                case SAINSoundType.Gunshot:
                case SAINSoundType.SuppressedGunShot:
                    return true;

                default:
                    return false;
            }
        }

        public bool doIHearSound(PlayerComponent playerComponent, Vector3 position, float range, SAINSoundType type, float distance, bool isGunshot, out bool visibleSource)
        {
            if (!isGunshot && !doIDetectFootsteps(distance, playerComponent))
            {
                visibleSource = false;
                return false;
            }

            float occludedRange = GetOcclusion(playerComponent, position, range, type, distance, isGunshot, out visibleSource);

            if (distance > occludedRange)
                return false;

            return true;
        }

        private float GetOcclusion(PlayerComponent playerComponent, Vector3 position, float range, SAINSoundType type, float distance, bool isGunshot, out bool visibleSource)
        {
            float result = range * EnvironmentCheck(playerComponent, isGunshot);
            visibleSource = true;

            if (playerComponent.IsAI)
            {
                return result;
            }

            if (!isGunshot)
            {
                position.y += 0.1f;
            }

            Vector3 botheadpos = BotOwner.MyHead.position;
            if (Physics.Raycast(botheadpos, position - botheadpos, range, LayerMaskClass.HighPolyWithTerrainNoGrassMask))
            {
                visibleSource = false;
                if (isGunshot)
                {
                    result *= GUNSHOT_OCCLUSION_MOD;
                }
                else
                {
                    result *= FOOTSTEP_OCCLUSION_MOD;
                }
            }

            return result;
        }

        const float GUNSHOT_OCCLUSION_MOD = 0.85f;
        const float FOOTSTEP_OCCLUSION_MOD = 0.75f;

        private float EnvironmentCheck(PlayerComponent enemy, bool isGunshot)
        {
            if (Player.AIData.EnvironmentId == enemy.Player.AIData.EnvironmentId)
            {
                return 1f;
            }
            return isGunshot ? GUNSHOT_ENVIR_MOD : FOOTSTEP_ENVIR_MOD;
        }

        const float GUNSHOT_ENVIR_MOD = 0.85f;
        const float FOOTSTEP_ENVIR_MOD = 0.7f;

        private float RaycastCheck(Vector3 botpos, Vector3 enemypos, float environmentmodifier)
        {
            if (raycasttimer < Time.time)
            {
                raycasttimer = Time.time + 0.25f;

                occlusionmodifier = 1f;

                LayerMask mask = LayerMaskClass.HighPolyWithTerrainNoGrassMask;

                // AddColor a RaycastHit array and set it to the Physics.RaycastAll
                var direction = botpos - enemypos;
                RaycastHit[] hits = Physics.RaycastAll(enemypos, direction, direction.magnitude, mask);

                int hitCount = 0;

                // Loop through each hit in the hits array
                for (int i = 0; i < hits.Length; i++)
                {
                    // Check if the hitCount is 0
                    if (hitCount == 0)
                    {
                        // If the hitCount is 0, set the occlusionmodifier to 0.8f multiplied by the environmentmodifier
                        occlusionmodifier *= 0.85f;
                    }
                    else
                    {
                        // If the hitCount is not 0, set the occlusionmodifier to 0.95f multiplied by the environmentmodifier
                        occlusionmodifier *= 0.95f;
                    }

                    // Increment the hitCount
                    hitCount++;
                }
                occlusionmodifier *= environmentmodifier;
            }

            return occlusionmodifier;
        }

        private float occlusionmodifier = 1f;
        private float raycasttimer = 0f;
    }
}