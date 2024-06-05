using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Interpolation;
using RootMotion.FinalIK;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.Preset.Personalities;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.SAINComponent.Classes.WeaponFunction;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace SAIN.SAINComponent.Classes
{
    public class SAINHearingSensorClass : SAINBase, ISAINClass
    {
        public SAINHearingSensorClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            Singleton<BotEventHandler>.Instance.OnSoundPlayed += HearSound;
            Singleton<BotEventHandler>.Instance.OnKill += cleanupKilledPlayer;
        }

        public void Update()
        {
        }

        private float cleanupTimer;

        private void cleanupKilledPlayer(IPlayer killer, IPlayer target)
        {
            if (target != null && SoundsHeardFromPlayer.ContainsKey(target.ProfileId))
            {
                var list = SoundsHeardFromPlayer[target.ProfileId].SoundList;
                if (SAINPlugin.EditorDefaults.DebugHearing)
                {
                    Logger.LogDebug($"Cleaned up {list.Count} sounds from killed player: {target.Profile?.Nickname}");
                }
                list.Clear();
                SoundsHeardFromPlayer.Remove(target.ProfileId);
            }
        }

        private void ManualCleanup(bool force = false)
        {
            if (SoundsHeardFromPlayer.Count == 0)
            {
                return;
            }
            foreach (var kvp in SoundsHeardFromPlayer)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Cleanup(force);
                }
            }
        }

        public void Dispose()
        {
            ManualCleanup(true);
            Singleton<BotEventHandler>.Instance.OnSoundPlayed -= HearSound;
            Singleton<BotEventHandler>.Instance.OnKill -= cleanupKilledPlayer;
        }

        private bool shallLimitAI(IPlayer iPlayer)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.General.LimitAIvsAI)
            {
                return false;
            }
            var currentLimit = Bot.CurrentAILimit;
            if (currentLimit == AILimitSetting.Close)
            {
                return false;
            }

            float sqrMag = (iPlayer.Position - Bot.Position).sqrMagnitude;
            float max = getMaxRange().Sqr();
            return sqrMag > max;
        }

        private float getMaxRange()
        {
            var currentLimit = Bot.CurrentAILimit;
            var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
            switch (currentLimit)
            {
                case AILimitSetting.Far:
                case AILimitSetting.VeryFar:
                    return settings.LimitAIvsAIMaxAudioRange;
                case AILimitSetting.Narnia:
                    return settings.LimitAIvsAIMaxAudioRangeVeryFar;
                default:
                    return float.MaxValue;
            }
        }

        public void HearSound(IPlayer iPlayer, Vector3 position, float power, AISoundType type)
        {
            if (Bot?.Player?.HealthController?.IsAlive == true && 
                iPlayer?.HealthController?.IsAlive == true)
            {
                if (shallIgnore(iPlayer, position))
                {
                    return;
                }

                if (iPlayer.IsAI && 
                    shallLimitAI(iPlayer))
                {
                    return;
                }

                const float freq = 1f;
                if (cleanupTimer < Time.time)
                {
                    cleanupTimer = Time.time + freq;
                    ManualCleanup();
                }

                EnemySoundHeard(iPlayer, position, power, type);
            }
        }

        private bool shallIgnore(IPlayer iPlayer, Vector3 position)
        {
            return 
                !Bot.BotActive ||
                Bot.GameIsEnding ||
                Bot.Person.ProfileId == iPlayer.ProfileId ||
                (iPlayer.Position - position).sqrMagnitude > 5f * 5f;
        }

        private readonly Dictionary<string, SAINSoundCollection> SoundsHeardFromPlayer = new Dictionary<string, SAINSoundCollection>();

        public bool EnemySoundHeard(IPlayer iPlayer, Vector3 soundPosition, float power, AISoundType type)
        {
            if (Bot.EnemyController.IsPlayerFriendly(iPlayer))
            {
                return false;
            }

            if (type == AISoundType.step && 
                iPlayer?.IsYourPlayer == true &&
                _nextChecktime < Time.time)
            {
                //Logger.LogDebug($"MainPlayer Footstep Sound Range {power}");
                _nextChecktime = Time.time + 1f;
            }

            bool wasHeard = checkIfSoundHeard(iPlayer, soundPosition, power, type, out float distance);
            bool bulletFelt = BulletFelt(iPlayer, type, soundPosition);

            return 
                (wasHeard || bulletFelt) && 
                ReactToSound(iPlayer, soundPosition, distance, wasHeard, bulletFelt, type);
        }

        private static float _nextChecktime;

        private bool checkIfSoundHeard(IPlayer player, Vector3 position, float power, AISoundType soundType, out float distance)
        {
            return DoIHearSound(player, position, power, soundType, out distance);
        }

        private float getSoundRangeModifier(AISoundType soundType, float soundDistance)
        {
            float modifier = 1f;
            var globalHearing = GlobalSettings.Hearing;
            if (soundType == AISoundType.step)
            {
                modifier *= globalHearing.FootstepAudioMultiplier;
            }
            else
            {
                modifier *= globalHearing.GunshotAudioMultiplier;
            }

            float hearSense = Bot.Info.FileSettings.Core.HearingSense;
            if (hearSense != 1f)
            {
                modifier *= hearSense;
            }

            if (soundType == AISoundType.step && 
                soundDistance > 15f)
            {
                if (!Bot.Equipment.HasEarPiece)
                {
                    modifier *= 0.65f;
                }
                if (Bot.Equipment.HasHeavyHelmet)
                {
                    modifier *= 0.8f;
                }
                if (Bot.Memory.Health.Dying && 
                    !Bot.Memory.Health.OnPainKillers)
                {
                    modifier *= 0.8f;
                }
                if (Player.IsSprintEnabled)
                {
                    modifier *= 0.85f;
                }
                if (Player.HeavyBreath)
                {
                    modifier *= 0.65f;
                }
            }

            return modifier;
        }

        private bool GetShotDirection(IPlayer person, out Vector3 result)
        {
            Player player = GameWorldInfo.GetAlivePlayer(person);
            if (player != null)
            {
                result = player.LookDirection;
                return true;
            }
            result = Vector3.zero;
            return false;
        }

        private bool BulletFelt(IPlayer person, AISoundType type, Vector3 pos)
        {
            const float bulletfeeldistance = 500f;
            const float DotThreshold = 0.5f;

            bool isGunSound = type == AISoundType.gun || type == AISoundType.silencedGun;

            Vector3 shooterDirectionToBot = BotOwner.Transform.position - pos;
            float shooterDistance = shooterDirectionToBot.magnitude;

            if (isGunSound && shooterDistance <= bulletfeeldistance && GetShotDirection(person, out Vector3 shotDirection))
            {
                Vector3 shotDirectionNormal = shotDirection.normalized;
                Vector3 botDirectionNormal = shooterDirectionToBot.normalized;

                float dot = Vector3.Dot(shotDirectionNormal, botDirectionNormal);

                if (dot >= DotThreshold)
                {
                    if (SAINPlugin.EditorDefaults.DebugHearing)
                    {
                        Logger.LogInfo($"Got Shot Direction from [{person.Profile?.Nickname}] to [{BotOwner?.name}] : Dot Product Result: [{dot}]");

                        //Vector3 start = person.MainParts[BodyPartType.head].Position;
                        //DebugGizmos.Ray(start, shotDirectionNormal, Color.red, 3f, 0.05f, true, 3f);
                        //DebugGizmos.Ray(start, botDirectionNormal, Color.yellow, 3f, 0.05f, true, 3f);
                    }
                    return true;
                }
            }
            return false;
        }

        private IEnumerator hearSound(IPlayer iPlayer, Vector3 soundPosition, float power, AISoundType type)
        {
            yield return null;
        }

        public bool ReactToSound(IPlayer person, Vector3 soundPosition, float power, bool wasHeard, bool bulletFelt, AISoundType type)
        {
            bool reacting = false;
            bool isGunSound = type == AISoundType.gun || type == AISoundType.silencedGun;
            float shooterDistance = (BotOwner.Transform.position - soundPosition).magnitude;

            Player player = GameWorldInfo.GetAlivePlayer(person);
            if (player != null && !BotOwner.EnemiesController.IsEnemy(player))
            {
                return false;
            }

            Vector3 vector = getSoundDispersion(person, soundPosition, type);

            if (person != null && 
                bulletFelt && 
                isGunSound)
            {
                float maxSuppressionDist = SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxSuppressionDistance;
                if (DidShotFlyByMe(person, maxSuppressionDist, out Vector3 projectionPoint, out float projectionPointDist))
                {
                    Bot.StartCoroutine(delayReact(vector, type, person, shooterDistance, projectionPoint, projectionPointDist));
                    reacting = true;
                }
            }

            if (!reacting && 
                !wasHeard)
            {
                return false;
            }

            if (!reacting && 
                !shallChaseGunshot(shooterDistance, soundPosition))
            {
                return reacting;
            }

            if (wasHeard)
            {
                //SAIN.Squad.SquadInfo.AddPointToSearch(vector, power, SAIN, type, person);
                Bot.StartCoroutine(delayAddSearch(vector, power, type, person));
                reacting = true;
            }
            else
            {
                Vector3 estimate = GetEstimatedPoint(vector);
                Bot.StartCoroutine(delayAddSearch(estimate, power, type, person));
                //SAIN.Squad.SquadInfo.AddPointToSearch(vector, power, SAIN, type, person);
                reacting = true;
            }
            return reacting;
        }

        private bool shallChaseGunshot(float shooterDistance, Vector3 soundPosition)
        {
            var searchSettings = Bot.Info.PersonalitySettings.Search;
            if (searchSettings.WillChaseDistantGunshots)
            {
                return true;
            }
            if (shooterDistance >= searchSettings.AudioStraightDistanceToIgnore)
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

        private IEnumerator baseHearDelay()
        {
            float delay;
            if (Bot?.EnemyController?.NoEnemyContact == true)
            {
                delay = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseHearingDelayAtPeace;
            }
            else
            {
                delay = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseHearingDelayWithEnemy;
            }
            delay = Mathf.Clamp(delay - 0.1f, 0f, 5f);
            yield return new WaitForSeconds(delay);
        }

        private IEnumerator delayReact(Vector3 soundPos, AISoundType type, IPlayer person, float shooterDistance, Vector3 projectionPoint, float projectionPointDist)
        {
            yield return baseHearDelay();

            if (Bot != null && 
                person != null && 
                Bot?.Player?.HealthController?.IsAlive == true &&
                person?.HealthController?.IsAlive == true)
            {
                float maxUnderFireDist = SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxUnderFireDistance;
                bool underFire = projectionPointDist <= maxUnderFireDist;
                if (underFire)
                {
                    BotOwner?.HearingSensor?.OnEnemySounHearded?.Invoke(soundPos, shooterDistance, type);
                    Bot.Memory.SetUnderFire(person, soundPos);
                }
                Bot.Suppression.AddSuppression(projectionPointDist);
                SAINEnemy enemy = Bot.EnemyController.CheckAddEnemy(person);
                if (enemy != null)
                {
                    enemy.SetEnemyAsSniper(shooterDistance > 100f);
                    enemy.EnemyStatus.ShotAtMeRecently = true;
                }
                CheckCalcGoal();
            }
        }

        private IEnumerator delayAddSearch(Vector3 vector, float power, AISoundType type, IPlayer person)
        {
            yield return baseHearDelay();

            if (Bot?.Player?.HealthController?.IsAlive == true && 
                person?.HealthController?.IsAlive == true)
            {
                Bot?.Squad?.SquadInfo?.AddPointToSearch(vector, power, Bot, type, person);
                CheckCalcGoal();
            }
        }

        private Vector3 getSoundDispersion(IPlayer person, Vector3 soundPosition, AISoundType soundType)
        {
            float shooterDistance = (Bot.Position - soundPosition).magnitude;
            float baseDispersion = getBaseDispersion(shooterDistance, soundType);

            float finalDispersion = 
                modifyDispersion(baseDispersion, person, out int soundCount) * 
                getDispersionFromDirection(soundPosition);

            float minDispersion = shooterDistance < 10 ? 0f : 0.5f;
            Vector3 randomdirection = getRandomizedDirection(finalDispersion, minDispersion);
            if (SAINPlugin.EditorDefaults.DebugHearing)
            {
                Logger.LogDebug($"Dispersion: [{randomdirection.magnitude}] :: Distance: [{shooterDistance}] : Sounds Heard: [{soundCount}] : PreCount Dispersion Num: [{baseDispersion}] : PreRandomized Dispersion Result: [{finalDispersion}] : SoundType: [{soundType}]");
            }
            return soundPosition + randomdirection;
        }

        private float getBaseDispersion(float shooterDistance, AISoundType soundType)
        {
            const float dispSuppGun = 12.5f;
            const float dispGun = 17.5f;
            const float dispStep = 6.25f;

            float dispersion;
            if (soundType == AISoundType.gun)
            {
                dispersion = shooterDistance / dispGun;
            }
            else if (soundType == AISoundType.silencedGun)
            {
                dispersion = shooterDistance / dispSuppGun;
            }
            else
            {
                dispersion = shooterDistance / dispStep;
            }
            return dispersion;
        }

        private float getDispersionFromDirection(Vector3 soundPosition)
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

            //Logger.LogInfo($"Random Height: [{randomHeight}]");

            Vector3 randomdirection = new Vector3(randomX, 0, randomZ);
            if (min > 0 && randomdirection.magnitude < min)
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

        private Vector3 GetEstimatedPoint(Vector3 source)
        {
            Vector3 randomPoint = UnityEngine.Random.onUnitSphere * (Vector3.Distance(source, BotOwner.Transform.position) / 10f);
            randomPoint.y = Mathf.Clamp(randomPoint.y, -5f, 5f);
            return source + randomPoint;
        }

        private bool DidShotFlyByMe(IPlayer player, float maxDist, out Vector3 projectionPoint, out float distance)
        {
            projectionPoint = calcProjectionPoint(player);
            distance = (projectionPoint - Bot.Position).magnitude;
            bool shotNearMe = distance <= maxDist;

            // if the direction the player shot hits a wall, and the point that they hit is further than our input max distance, the shot did not fly by the bot.
            Vector3 weaponRoot = player.WeaponRoot.position;
            Vector3 direction = projectionPoint - weaponRoot;
            if (shotNearMe && 
                Physics.Raycast(player.WeaponRoot.position, direction, out var hit, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask) && 
                (hit.point - Bot.Position).magnitude > maxDist)
            {
                return false;
            }

            if (SAINPlugin.EditorDefaults.DebugHearing && 
                shotNearMe)
            {
                DebugGizmos.Sphere(projectionPoint, 0.25f, Color.red, true, 60f);
                DebugGizmos.Line(projectionPoint, player.WeaponRoot.position, Color.red, 0.1f, true, 60f, true);
            }

            return shotNearMe;
        }

        public Vector3 calcProjectionPoint(IPlayer player)
        {
            Vector3 lookDir = player.LookDirection.normalized;
            Vector3 shotPos = player.WeaponRoot.position;
            Vector3 botDir = Bot.Position - shotPos;
            float dist = botDir.magnitude;
            Vector3 projectionPoint = shotPos + lookDir * dist;
            return projectionPoint;
        }

        private bool CheckFootStepDetectChance(float distance)
        {
            bool hasheadPhones = Bot.Equipment.HasEarPiece;

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
            bool midRange = distance < farhearing * 0.66f;
            if (midRange && 
                Player.Velocity.magnitude < 0.5f)
            {
                minimumChance += 5f;
            }

            // Random chance to hear at any range within maxdistance if a bot has headphones
            if (midRange && 
                Bot.Equipment.HasEarPiece && 
                EFTMath.RandomBool(5))
            {
                minimumChance += 5f;
            }

            float num = farhearing - closehearing;
            float num2 = distance - closehearing;

            float chanceToHear = 1f - num2 / num;

            chanceToHear = Mathf.Clamp(chanceToHear, minimumChance, 1f);

            return EFTMath.RandomBool(chanceToHear);
        }

        public bool DoIHearSound(IPlayer iPlayer, Vector3 position, float range, AISoundType type, out float soundDistance)
        {
            bool wasHeard = false;
            soundDistance = (Bot.Position - position).magnitude;

            if (type == AISoundType.step && 
                !CheckFootStepDetectChance(soundDistance))
            {
                return false;
            }

            float rangeModifier = getSoundRangeModifier(type, soundDistance);
            if (rangeModifier != 1f)
            {
                range = Mathf.Round(range * rangeModifier * 100) / 100;
            }

            // Is sound within hearing Distance at all?
            if (soundDistance < range)
            {
                // if so, is sound blocked by obstacles?
                float occludedPower = GetOcclusion(iPlayer, position, range, type);
                if (soundDistance < occludedPower)
                {
                    wasHeard = true;
                }
            }
            return wasHeard;
        }

        private float GetOcclusion(IPlayer player, Vector3 position, float power, AISoundType type)
        {
            Vector3 botheadpos = BotOwner.MyHead.position;

            if (type == AISoundType.step)
            {
                position.y += 0.1f;
            }

            Vector3 direction = (botheadpos - position).normalized;
            float soundDistance = direction.magnitude;

            float result = power;
            // Checks if something is within line of sight
            if (Physics.Raycast(botheadpos, direction, power, LayerMaskClass.HighPolyWithTerrainNoGrassMask))
            {
                if (player == null)
                {
                    result *= 0.8f;
                }
                // If the sound source is the iPlayer, raycast and find number of collisions
                else if (player.IsYourPlayer)
                {
                    // Check if the sound originates from an environment other than the BotOwner's
                    float environmentmodifier = EnvironmentCheck(player);

                    // Raycast check
                    float finalmodifier = RaycastCheck(botheadpos, position, environmentmodifier);

                    // Reduce occlusion for unsuppressed guns
                    if (type == AISoundType.gun) finalmodifier = Mathf.Sqrt(finalmodifier);

                    // Apply Modifier
                    result = power * finalmodifier;
                }
                else
                {
                    // Only check environment for bots vs bots
                    result = power * EnvironmentCheck(player);
                }
            }
            return result;
        }

        private float EnvironmentCheck(IPlayer enemy)
        {
            int botlocation = BotOwner.AIData.EnvironmentId;
            int enemylocation = enemy.AIData.EnvironmentId;
            return botlocation == enemylocation ? 1f : 0.5f;
        }

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