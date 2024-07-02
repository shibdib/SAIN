using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes
{
    public class SAINHearingSensorClass : BotBaseClass, ISAINClass
    {
        public bool SetIgnoreHearingExternal(bool value, bool ignoreUnderFire, float duration, out string reason)
        {
            if (Bot.Enemy?.IsVisible == true)
            {
                reason = "Enemy Visible";
                return false;
            }
            if (BotOwner.Memory.IsUnderFire && !ignoreUnderFire)
            {
                reason = "Under Fire";
                return false;
            }

            _ignoreUnderFire = ignoreUnderFire;
            _hearingSetToIgnore = value;
            if (value && duration > 0f)
            {
                _ignoreUntilTime = Time.time + duration;
            }
            else
            {
                _ignoreUntilTime = -1f;
            }
            reason = string.Empty;
            return true;
        }

        private bool _ignoreUnderFire;
        private bool _hearingSetToIgnore;
        private float _ignoreUntilTime;

        public SAINHearingSensorClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPresetChanges(UpdatePresetSettings);
            SAINBotController.Instance.AISoundPlayed += soundHeard;
            SAINBotController.Instance.BulletImpact += bulletImpacted;
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            SAINBotController.Instance.AISoundPlayed -= soundHeard;
            SAINBotController.Instance.BulletImpact -= bulletImpacted;
        }

        private void bulletImpacted(EftBulletClass bullet)
        {
            if (!Bot.BotActive)
            {
                return;
            }

            if (checkIgnoreExternal())
            {
                return;
            }

            if (_nextHearImpactTime > Time.time)
            {
                return;
            }

            if (Bot.HasEnemy)
            {
                return;
            }

            var player = bullet.Player?.iPlayer;
            if (player == null)
            {
                return;
            }

            if (player.ProfileId == Bot.ProfileId)
            {
                return;
            }

            var enemy = Bot.EnemyController.CheckAddEnemy(player);
            if (enemy == null)
            {
                return;
            }

            if (!enemy.IsValid)
            {
                return;
            }

            if (Bot.PlayerComponent.AIData.PlayerLocation.InBunker != enemy.EnemyPlayerComponent.AIData.PlayerLocation.InBunker)
            {
                return;
            }

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
            enemy.Hearing.SetHeard(estimatedPos, SAINSoundType.BulletImpact, true);
        }

        private float _nextHearImpactTime;
        private const float IMPACT_HEAR_FREQUENCY = 0.5f;
        private const float IMPACT_HEAR_FREQUENCY_FAR = 0.05f;
        private const float IMPACT_MAX_HEAR_DISTANCE = 50f * 50f;
        private const float IMPACT_DISPERSION = 5f * 5f;

        private bool checkIgnoreExternal()
        {
            if (!_hearingSetToIgnore)
            {
                return false;
            }
            if (_ignoreUntilTime > 0 &&
                _ignoreUntilTime < Time.time)
            {
                _hearingSetToIgnore = false;
                _ignoreUnderFire = false;
                return false;
            }
            return true;
        }

        private bool checkShallIgnore(PlayerComponent playerComponent, float volume)
        {
            if (!Bot.BotActive)
            {
                return true;
            }
            if (_hearingSetToIgnore)
            {
                if (_ignoreUntilTime > 0 && _ignoreUntilTime < Time.time)
                {
                    _hearingSetToIgnore = false;
                }
            }
            if (Bot.GameEnding)
            {
                return true;
            }
            if (volume <= 0)
            {
                return true;
            }
            if (shallIgnore(playerComponent))
            {
                return true;
            }
            return false;
        }

        private void calcSqrDist(BotSound sound)
        {
            sound.IsGunShot = sound.SoundType.IsGunShot();

            if (!sound.IsGunShot && checkIgnoreExternal())
            {
                sound.OutOfRange = true;
                return;
            }

            sound.SqrDistance = (sound.OriginalPosition - Bot.Position).sqrMagnitude;

            float power = sound.Power;
            if (!sound.IsGunShot &&
                power * power < sound.SqrDistance)
            {
                sound.OutOfRange = true;
            }
        }

        private void calcRange(BotSound sound)
        {
            sound.BaseRange = sound.Power * sound.Volume;
            calcBunkerVolumeReduction(sound);
            float range = sound.BaseRange * sound.BunkerReduction;

            float maxAffectDist = HEAR_MODIFIER_MAX_AFFECT_DIST / 2;
            if (range > maxAffectDist)
            {
                getSoundRangeModifier(sound);
                range = Mathf.Clamp(range * sound.RangeModifier, maxAffectDist, float.MaxValue);
            }
            sound.Range = range;
            if (!sound.IsGunShot && 
                range * range < sound.SqrDistance)
            {
                sound.OutOfRange = true;
            }
        }

        private void checkDistToPlayer(BotSound sound)
        {
            // The sound originated from somewhere far from the player's position, typically from a grenade explosion, which is handled elsewhere
            if ((sound.PlayerComponent.Position - sound.OriginalPosition).sqrMagnitude > 5f * 5f)
            {
                sound.OutOfRange = true;
            }
        }

        private void soundHeard(
            SAINSoundType soundType, 
            Vector3 soundPosition, 
            PlayerComponent playerComponent, 
            float power, 
            float volume)
        {
            if (checkShallIgnore(playerComponent, volume))
            {
                return;
            }

            Enemy enemy = Bot.EnemyController.CheckAddEnemy(playerComponent.IPlayer);
            if (enemy == null)
            {
                return;
            }

            BotSoundStruct soundStruct = new BotSoundStruct
            {
                Info = new SoundInfoData
                {
                    PlayerComponent = playerComponent,
                    IsAI = playerComponent.IsAI,
                    OriginalPosition = soundPosition,
                    Power = power,
                    Volume = volume,
                    SoundType = soundType,
                    Enemy = enemy,
                }
            };

            var sound = new BotSound(playerComponent, soundPosition, power, volume, soundType, enemy);

            calcSqrDist(sound);
            if (sound.OutOfRange)
            {
                return;
            }
            if (shallLimitAI(sound))
            {
                return;
            }
            calcRange(sound);
            if (sound.OutOfRange)
            {
                return;
            }
            checkDistToPlayer(sound);
            if (sound.OutOfRange)
            {
                return;
            }

            checkSoundHeard(sound);
            doIFeelBullet(sound);

            if (!sound.WasHeard && !sound.BulletFelt)
            {
                return;
            }

            if (DidShotFlyByMe(sound))
            {
                float addDisp = sound.WasHeard ? 1f : 2f;
                getSoundDispersion(sound, addDisp);
                Bot.StartCoroutine(delayReact(sound));
                return;
            }

            if (!sound.WasHeard)
            {
                return;
            }
            if (checkIgnoreExternal())
            {
                return;
            }
            if (!shallChaseGunshot(sound))
            {
                return;
            }

            getSoundDispersion(sound, 1f);
            Bot.StartCoroutine(delayAddSearch(sound));
        }

        private void calcBunkerVolumeReduction(BotSound sound)
        {
            bool botinBunker = Bot.PlayerComponent.AIData.PlayerLocation.InBunker;
            bool playerinBunker = sound.PlayerComponent.AIData.PlayerLocation.InBunker;
            if (botinBunker != playerinBunker)
            {
                sound.BunkerReduction = 0.2f;
                return;
            }
            if (botinBunker)
            {
                float botDepth = Bot.PlayerComponent.AIData.PlayerLocation.BunkerDepth;
                float playerDepth = sound.PlayerComponent.AIData.PlayerLocation.BunkerDepth;
                float diff = Mathf.Abs(botDepth - playerDepth);
                if (Mathf.Abs(botDepth - playerDepth) > 0)
                {
                    sound.BunkerReduction = 0.66f;
                    return;
                }
            }
            sound.BunkerReduction = 1f;
        }

        private bool shallLimitAI(BotSound sound)
        {
            if (!sound.PlayerComponent.IsAI)
                return false;

            var aiLimit = GlobalSettingsClass.Instance.AILimit;
            if (!aiLimit.LimitAIvsAIGlobal)
                return false;

            if (!aiLimit.LimitAIvsAIHearing)
                return false;

            if (Bot.Enemy?.EnemyProfileId == sound.PlayerComponent.ProfileId)
                return false;

            var enemyBot = sound.PlayerComponent.BotComponent;
            float maxRange;
            if (enemyBot == null)
            {
                if (sound.PlayerComponent.BotOwner?.Memory.GoalEnemy.ProfileId == Bot.ProfileId)
                {
                    return false;
                }
                maxRange = getMaxRange(Bot.CurrentAILimit);
            }
            else
            {
                if (enemyBot.Enemy?.EnemyProfileId == Bot.ProfileId)
                {
                    return false;
                }
                maxRange = getMaxRange(enemyBot.CurrentAILimit);
            }

            if (sound.SqrDistance <= maxRange)
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
                    return _farDistance;

                case AILimitSetting.VeryFar:
                    return _veryFarDistance;

                case AILimitSetting.Narnia:
                    return _narniaDistance;

                default:
                    return float.MaxValue;
            }
        }

        private bool shallIgnore(PlayerComponent playerComponent)
        {
            if (Bot.ProfileId == playerComponent.ProfileId)
            {
                return true;
            }
            return false;
        }

        private readonly Dictionary<string, SAINSoundCollection> SoundsHeardFromPlayer = new Dictionary<string, SAINSoundCollection>();

        private void getSoundRangeModifier(BotSound sound)
        {
            float modifier = 1f;
            if (!sound.IsGunShot)
            {
                modifier *= GlobalSettings.Hearing.FootstepAudioMultiplier;
            }
            else
            {
                modifier *= GlobalSettings.Hearing.GunshotAudioMultiplier;
            }
            modifier *= Bot.Info.FileSettings.Core.HearingSense;

            if (!sound.IsGunShot && sound.SqrDistance > HEAR_MODIFIER_MAX_AFFECT_DIST)
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

            sound.RangeModifier = Mathf.Clamp(modifier, HEAR_MODIFIER_MIN_CLAMP, HEAR_MODIFIER_MAX_CLAMP);
        }

        private const float HEAR_MODIFIER_MAX_AFFECT_DIST = 15f;
        private const float HEAR_MODIFIER_NO_EARS = 0.65f;
        private const float HEAR_MODIFIER_HEAVY_HELMET = 0.8f;
        private const float HEAR_MODIFIER_DYING = 0.8f;
        private const float HEAR_MODIFIER_SPRINT = 0.85f;
        private const float HEAR_MODIFIER_HEAVYBREATH = 0.65f;
        private const float HEAR_MODIFIER_MIN_CLAMP = 0.35f;
        private const float HEAR_MODIFIER_MAX_CLAMP = 5f;

        private void doIFeelBullet(BotSound sound)
        {
            if (!sound.IsGunShot)
            {
                return;
            }
            if (sound.Distance > BULLET_FEEL_MAX_DIST)
            {
                return;
            }
            Vector3 directionToBot = (Bot.Position - sound.OriginalPosition).normalized;
            float dot = Vector3.Dot(sound.PlayerComponent.LookDirection, directionToBot);
            sound.BulletFelt = dot >= BULLET_FEEL_DOT_THRESHOLD;
        }

        const float BULLET_FEEL_DOT_THRESHOLD = 0.75f;
        const float BULLET_FEEL_MAX_DIST = 500f;

        private bool shallChaseGunshot(BotSound sound)
        {
            var searchSettings = Bot.Info.PersonalitySettings.Search;
            if (searchSettings.WillChaseDistantGunshots)
            {
                return true;
            }
            if (sound.Distance > searchSettings.AudioStraightDistanceToIgnore)
            {
                return false;
            }
            if (sound.Distance < searchSettings.AudioStraightDistanceToIgnore * 2f)
            {
                return true;
            }
            if (isPathTooFar(sound.OriginalPosition, searchSettings.AudioPathLengthDistanceToIgnore))
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

        private const float SPEED_OF_SOUND = 343;

        private IEnumerator baseHearDelay(float distance)
        {
            float delay = distance / SPEED_OF_SOUND;
            if (Bot?.EnemyController?.AtPeace == true)
            {
                delay += SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseHearingDelayAtPeace;
            }
            else
            {
                delay += SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseHearingDelayWithEnemy;
            }
            yield return new WaitForSeconds(delay);
        }

        private IEnumerator delayReact(BotSound sound)
        {
            yield return baseHearDelay(sound.Distance);

            if (Bot != null && sound?.PlayerComponent != null)
            {
                bool underFire = sound.ProjectionPointDistance <= SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxUnderFireDistance;
                if (!underFire && _hearingSetToIgnore)
                {
                    yield break;
                }

                if (underFire)
                {
                    BotOwner?.HearingSensor?.OnEnemySounHearded?.Invoke(sound.RandomizedPosition, sound.Distance, sound.SoundType.Convert());
                    Bot.Memory.SetUnderFire(sound.PlayerComponent.IPlayer, sound.RandomizedPosition);
                }
                Bot.Suppression.AddSuppression(sound.ProjectionPointDistance);
                Enemy enemy = sound.Enemy;
                if (enemy != null)
                {
                    enemy.SetEnemyAsSniper(sound.Distance > 100f);
                    enemy.Status.ShotAtMeRecently = true;
                }
                addPointToSearch(sound);
            }
        }

        private void addPointToSearch(BotSound sound)
        {
            Bot.Squad.SquadInfo?.AddPointToSearch(sound, Bot);
            CheckCalcGoal();
        }

        private IEnumerator delayAddSearch(BotSound sound)
        {
            yield return baseHearDelay(sound.Distance);

            if (Bot != null && sound?.PlayerComponent != null)
            {
                addPointToSearch(sound);
            }
        }

        private void getSoundDispersion(BotSound sound, float addDispersion)
        {
            float baseDispersion = getBaseDispersion(sound.Distance, sound.SoundType);
            float dispersionMod = getDispersionModifier(sound.OriginalPosition) * addDispersion;
            float finalDispersion = baseDispersion * dispersionMod;
            sound.Dispersion = finalDispersion;
            float min = sound.Distance < 10 ? 0f : 0.5f;
            Vector3 randomdirection = getRandomizedDirection(finalDispersion, min);

            if (SAINPlugin.DebugSettings.DebugHearing)
                Logger.LogDebug($"Dispersion: [{randomdirection.magnitude}] Distance: [{sound.Distance}] Base Dispersion: [{baseDispersion}] DispersionModifier [{dispersionMod}] Final Dispersion: [{finalDispersion}] : SoundType: [{sound.SoundType}]");

            sound.RandomizedPosition = sound.OriginalPosition + randomdirection;
        }

        private float getBaseDispersion(float shooterDistance, SAINSoundType soundType)
        {
            const float dispSuppGun = 12.5f;
            const float dispGun = 17.5f;
            const float dispStep = 6.25f;

            float dispersion;
            switch (soundType)
            {
                case SAINSoundType.Shot:
                    dispersion = shooterDistance / dispGun;
                    break;

                case SAINSoundType.SuppressedShot:
                    dispersion = shooterDistance / dispSuppGun;
                    break;

                default:
                    dispersion = shooterDistance / dispStep;
                    break;
            }

            return dispersion;
        }

        private void calcNewDispersion(BotSound sound)
        {

        }

        private void calcNewDispersionAngle(BotSound sound)
        {

        }

        private void calcNewDispersionDistance(BotSound sound)
        {

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

        private bool DidShotFlyByMe(BotSound sound)
        {
            if (!sound.IsGunShot)
            {
                return false;
            }
            if (_ignoreUnderFire)
            {
                return false;
            }

            float maxDist = SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxSuppressionDistance;
            float maxDistSqr = maxDist * maxDist;
            sound.ProjectionPoint = calcProjectionPoint(sound.PlayerComponent, sound.Distance);
            sound.ProjectionPointDistance = (sound.ProjectionPoint - Bot.Position).sqrMagnitude;

            bool shotNearMe = sound.ProjectionPointDistance <= maxDistSqr;
            if (sound.ProjectionPointDistance > maxDistSqr)
            {
                return false;
            }

            // if the direction the player shot hits a wall, and the point that they hit is further than our input max distance, the shot did not fly by the bot.
            Vector3 firePort = sound.PlayerComponent.Transform.WeaponFirePort;
            Vector3 direction = sound.ProjectionPoint - firePort;
            if (Physics.Raycast(firePort, direction, out var hit, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask) &&
                (hit.point - Bot.Position).sqrMagnitude > maxDistSqr)
            {
                return false;
            }

            if (SAINPlugin.DebugSettings.DebugHearing)
            {
                DebugGizmos.Sphere(sound.ProjectionPoint, 0.25f, Color.red, true, 60f);
                DebugGizmos.Line(sound.ProjectionPoint, firePort, Color.red, 0.1f, true, 60f, true);
            }

            sound.ProjectionPointDistance = Mathf.Sqrt(sound.ProjectionPointDistance);
            return true;
        }

        public Vector3 calcProjectionPoint(PlayerComponent playerComponent, float realDistance)
        {
            Vector3 weaponPointDir = playerComponent.Transform.WeaponPointDirection;
            Vector3 shotPos = playerComponent.Transform.WeaponFirePort;
            Vector3 projectionPoint = shotPos + (weaponPointDir * realDistance);
            return projectionPoint;
        }

        private bool doIDetectFootsteps(BotSound sound)
        {
            if (sound.IsGunShot)
            {
                return true;
            }
            bool hasheadPhones = Bot.PlayerComponent.Equipment.GearInfo.HasEarPiece;
            float closehearing = hasheadPhones ? 3f : 0f;
            if (sound.Distance <= closehearing)
            {
                return true;
            }

            float farhearing = hasheadPhones ? SAINPlugin.LoadedPreset.GlobalSettings.Hearing.MaxFootstepAudioDistance : SAINPlugin.LoadedPreset.GlobalSettings.Hearing.MaxFootstepAudioDistanceNoHeadphones;
            if (sound.Distance > farhearing)
            {
                return false;
            }

            float minimumChance = 0f;

            if (hasheadPhones)
            {
                if (sound.Distance < farhearing * 0.66f)
                {
                    minimumChance += 10f;
                }
                else
                {
                    minimumChance += 5f;
                }
            }

            if (Bot.PlayerComponent.Transform.PlayerVelocity < 0.1f)
            {
                minimumChance += 5f;
            }

            if (Bot.HasEnemy &&
                Bot.Enemy.EnemyProfileId == sound.PlayerComponent.ProfileId)
            {
                minimumChance += 20f;
            }

            float num = farhearing - closehearing;
            float num2 = sound.Distance - closehearing;
            float chanceToHear = 1f - num2 / num;
            chanceToHear *= 100f;

            chanceToHear = Mathf.Clamp(chanceToHear, minimumChance, 100f);
            sound.ChanceToHear = chanceToHear;
            return EFTMath.RandomBool(chanceToHear);
        }

        public void checkSoundHeard(BotSound sound)
        {
            sound.Distance = Mathf.Sqrt(sound.SqrDistance);
            if (!doIDetectFootsteps(sound))
            {
                sound.OutOfRange = true;
                return;
            }
            checkOcclusion(sound);
            if (sound.OutOfRange)
            {
                return;
            }
            sound.WasHeard = true;
        }

        private void checkOcclusion(BotSound sound)
        {
            sound.OcclusionModifier = EnvironmentCheck(sound.PlayerComponent, sound.IsGunShot);
            if (sound.OccludedRange < sound.Distance)
            {
                sound.OutOfRange = true;
                return;
            }

            if (sound.IsAI)
            {
                return;
            }

            Vector3 position = sound.OriginalPosition;
            if (!sound.IsGunShot)
            {
                position.y += 0.5f;
            }

            Vector3 botheadpos = BotOwner.MyHead.position;
            if (!Physics.Raycast(botheadpos, position - botheadpos, sound.OccludedRange, LayerMaskClass.HighPolyWithTerrainNoGrassMask))
            {
                sound.VisibleSource = true;
                return;
            }

            if (sound.IsGunShot)
            {
                sound.OcclusionModifier *= GUNSHOT_OCCLUSION_MOD;
            }
            else
            {
                sound.OcclusionModifier *= FOOTSTEP_OCCLUSION_MOD;
            }

            if (sound.OccludedRange < sound.Distance)
            {
                sound.OutOfRange = true;
            }
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

        protected void UpdatePresetSettings(SAINPresetClass preset)
        {
            var aiLimit = preset.GlobalSettings.AILimit;
            _farDistance = aiLimit.MaxHearingRanges[AILimitSetting.Far].Sqr();
            _veryFarDistance = aiLimit.MaxHearingRanges[AILimitSetting.VeryFar].Sqr();
            _narniaDistance = aiLimit.MaxHearingRanges[AILimitSetting.Narnia].Sqr();
            if (SAINPlugin.DebugMode)
            {
                Logger.LogDebug($"Updated AI Hearing Limit Settings: [{_farDistance.Sqrt()}, {_veryFarDistance.Sqrt()}, {_narniaDistance.Sqrt()}]");
            }
        }

        private static float _farDistance;
        private static float _veryFarDistance;
        private static float _narniaDistance;

        private float occlusionmodifier = 1f;
        private float raycasttimer = 0f;
    }
}