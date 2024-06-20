using EFT;
using SAIN.Components.BotComponentSpace.Classes.Enemy;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class SAINEnemy : SAINBase, ISAINClass
    {
        public Action<SAINEnemy> OnEnemyKnown { get; set; }
        public Action<SAINEnemy> OnEnemyForgotten { get; set; }
        public Action<SAINEnemy> OnEnemyHeard { get; set; }
        public Action<SAINEnemy> OnGainVision { get; set; }
        public Action<SAINEnemy> OnLostVision { get; set; }
        public Action<SAINEnemy> OnPathUpdated { get; set; }
        public Action<SAINEnemy> OnVulnerableStateChanged { get; set; }
        public Action<SAINEnemy> OnHealthStatusChanged { get; set; }

        public Action<SAINEnemy> OnEnemyBecomeActiveThreat { get; set; }
        public Action<SAINEnemy> OnEnemyNotActiveThreat { get; set; }

        public void Update()
        {
            IsCurrentEnemy = Bot.Enemy?.EnemyProfileId == EnemyProfileId;
            checkShallKnowEnemy();
            updateRealDistance();
            updateActiveState();
            EnemyVision.Update();
            KnownPlaces.Update();
            EnemyPath.Update();
            EnemyStatus.Update();
        }

        private void checkShallKnowEnemy()
        {
            bool enemyKnown = shallKnowEnemy();

            if (!EnemyKnown && enemyKnown)
            {
                EnemyKnown = true;
                TimeEnemyKnown = Time.time;
                OnEnemyKnown?.Invoke(this);
                return;
            }

            if (EnemyKnown && !enemyKnown)
            {
                EnemyKnown = false;
                TimeEnemyForgotten = Time.time;
                OnEnemyForgotten?.Invoke(this);
                return;
            }

            ActiveThreat = enemyKnown ? checkActiveThreat() : false;
        }

        public float TimeEnemyKnown { get; private set; }
        public float TimeEnemyForgotten { get; private set; }
        public bool EnemyKnown { get; private set; } = false;

        private bool shallKnowEnemy()
        {
            if (EnemyPlayerComponent.IsActive)
            {
                if (BotIsSearchingForMe())
                {
                    return true;
                }
                if (TimeSinceLastKnownUpdated < Bot.Info.ForgetEnemyTime)
                {
                    return true;
                }
            }
            return false;
        }

        public bool BotIsSearchingForMe()
        {
            if (!isBotSearching())
            {
                return false;
            }
            SAINEnemy searchTarget = Bot.Search.SearchTarget;
            if (searchTarget != null && searchTarget == this)
            {
                return !KnownPlaces.SearchedAllKnownLocations;
            }
            return false;
        }

        private bool isBotSearching()
        {
            if (Bot.Decision.CurrentSoloDecision == SoloDecision.Search)
            {
                return true;
            }
            var squadDecision = Bot.Decision.CurrentSquadDecision;
            if (squadDecision == SquadDecision.Search ||
                squadDecision == SquadDecision.GroupSearch)
            {
                return true;
            }
            return false;
        }

        public void Init()
        {
            KnownPlaces.Init();
            EnemyVision.Init();
            EnemyPath.Init();
            EnemyHearing.Init();
            EnemyStatus.Init();
        }

        public readonly string EnemyName;
        public readonly string EnemyProfileId;
        public readonly bool IsAI;

        public PlayerComponent EnemyPlayerComponent { get; private set; }
        public SAINEnemyStatus EnemyStatus { get; private set; }
        public SAINEnemyVision EnemyVision { get; private set; }
        public SAINEnemyPath EnemyPath { get; private set; }
        public EnemyInfo EnemyInfo { get; private set; }
        public EnemyAim EnemyAim { get; private set; }
        public EnemyHearing EnemyHearing { get; private set; }

        public PersonClass EnemyPerson => EnemyPlayerComponent.Person;
        public PersonTransformClass EnemyTransform => EnemyPlayerComponent.Transform;

        public SAINEnemy(BotComponent bot, PlayerComponent playerComponent, EnemyInfo enemyInfo) : base(bot)
        {
            TimeEnemyCreated = Time.time;
            EnemyPlayerComponent = playerComponent;
            EnemyName = $"{playerComponent.Name} ({playerComponent.Person.Nickname})";
            EnemyInfo = enemyInfo;
            IsAI = playerComponent.IsAI;
            EnemyProfileId = playerComponent.ProfileId;

            EnemyStatus = new SAINEnemyStatus(this);
            EnemyVision = new SAINEnemyVision(this);
            EnemyPath = new SAINEnemyPath(this);
            KnownPlaces = new EnemyKnownPlaces(this);
            EnemyAim = new EnemyAim(this);
            EnemyHearing = new EnemyHearing(this);
        }

        public float LastCheckLookTime { get; set; }

        public bool EnemyNotLooking => IsVisible && !EnemyStatus.EnemyLookingAtMe && !EnemyStatus.ShotAtMeRecently;

        public bool IsCurrentEnemy { get; private set; }

        public bool IsValid
        {
            get
            {
                var component = EnemyPlayerComponent;
                if (component == null)
                {
                    Logger.LogError($"Enemy {EnemyName} PlayerComponent is Null");
                    return false;
                }
                var person = component.Person;
                if (person == null)
                {
                    Logger.LogDebug("Enemy Person is Null");
                    return false;
                }
                if (!person.PlayerExists)
                {
                    Logger.LogDebug("Enemy Player does not exist");
                    return false;
                }
                if (EnemyPlayer?.HealthController?.IsAlive == false)
                {
                    //Logger.LogDebug("Enemy is Dead. Removing...");
                    return false;
                }
                // Checks specific to bots
                BotOwner botOwner = EnemyPlayer?.AIData?.BotOwner;
                if (EnemyPerson?.IsAI == true && botOwner == null)
                {
                    Logger.LogDebug("Enemy is AI, but BotOwner is null. Removing...");
                    return false;
                }
                if (botOwner != null && botOwner.ProfileId == BotOwner.ProfileId)
                {
                    Logger.LogWarning("Enemy has same profile id as Bot? Removing...");
                    return false;
                }
                return true;
            }
        }

        public float NextCheckFlashLightTime;

        public bool ActiveThreat { get; private set; }

        private bool checkActiveThreat()
        {
            // If the enemy is an in-active bot or haven't sensed them in a very long time, just set them as inactive.
            if (!EnemyKnown)
            {
                return false;
            }

            if (IsCurrentEnemy)
            {
                return true;
            }

            // have we seen them very recently?
            if (IsVisible || (Seen && TimeSinceSeen < 30f))
            {
                return true;
            }
            // have we heard them very recently?
            if (EnemyStatus.HeardRecently || (Heard && TimeSinceHeard < 10f))
            {
                return true;
            }
            Vector3? lastKnown = LastKnownPosition;
            // do we have no position where we sensed an enemy?
            if (lastKnown == null)
            {
                return false;
            }
            float timeSinceActive = TimeSinceActive;
            float sqrMagnitude = (lastKnown.Value - EnemyPosition).sqrMagnitude;
            if (IsAI)
            {
                // Is the AI Enemys current position greater than x meters away from our last known position?
                if (sqrMagnitude > _activeDistanceThresholdAI * _activeDistanceThresholdAI)
                {
                    // Set them as inactive after a certain x seconds
                    return timeSinceActive < _activeForPeriodAI;
                }
                else
                {
                    // Enemy is close to where we last saw them, keep considering them as active.
                    return true;
                }
            }
            else
            {
                // Is the Human Enemys current position greater than x meters away from our last known position?
                if (sqrMagnitude > _activeDistanceThreshold * _activeDistanceThreshold)
                {
                    // Set them as inactive after a certain x seconds
                    return timeSinceActive < _activeForPeriod;
                }
                else
                {
                    // Enemy is close to where we last saw them, keep considering them as active.
                    return true;
                }
            }
        }

        public float TimeLastActive { get; private set; } = 0f;
        public float TimeSinceActive => _hasBeenActive ? Time.time - TimeLastActive : float.MaxValue;

        public Collider HidingBehindObject
        {
            get
            {
                float time = Time.time;
                if (_nextCheckHidingTime < time)
                {
                    _nextCheckHidingTime = time + _checkHidingFreq;
                    _hidingBehindObject = null;
                    Vector3? lastKnown = LastKnownPosition;
                    if (lastKnown != null
                        && Physics.Raycast(lastKnown.Value + Vector3.up, Bot.Position + Vector3.up, out RaycastHit hit, _checkHidingRayDist, LayerMaskClass.HighPolyCollider))
                    {
                        _hidingBehindObject = hit.collider;
                    }
                }
                return _hidingBehindObject;
            }
        }

        public Vector3? SuppressionTarget
        {
            get
            {
                if (CanSeeLastCornerToEnemy && LastCornerToEnemy != null)
                {
                    return LastCornerToEnemy.Value;
                }
                if (HidingBehindObject != null)
                {
                    Vector3 pos = HidingBehindObject.transform.position + HidingBehindObject.bounds.size.z * Vector3.up;
                    if ((pos - EnemyPosition).sqrMagnitude < 3f * 3f)
                    {
                        return pos;
                    }
                }
                return null;
            }
        }

        public Vector3? CenterMass
        {
            get
            {
                if (EnemyIPlayer == null)
                {
                    return null;
                }
                if (_nextGetCenterTime < Time.time)
                {
                    _nextGetCenterTime = Time.time + 0.05f;
                    _centerMass = new Vector3?(findCenterMass());
                }
                return _centerMass;
            }
        }

        public bool FirstContactOccured => EnemyVision.FirstContactOccured;

        public bool FirstContactReported = false;
        public bool ShallReportRepeatContact => EnemyVision.ShallReportRepeatContact;
        public Vector3 EnemyMoveDirection { get; set; }

        public bool EnemyHeardFromPeace = false;
        public bool Heard { get; set; }
        public float TimeSinceHeard => Time.time - TimeLastHeard;
        public float TimeLastHeard { get; private set; }
        public Vector3? LastHeardPosition => KnownPlaces.LastHeardPlace?.Position;

        public EPathDistance CheckPathDistance() => EnemyPath.EPathDistance;

        public IPlayer EnemyIPlayer => EnemyPlayerComponent.IPlayer;

        public Player EnemyPlayer => EnemyPlayerComponent.Player;

        public float TimeEnemyCreated { get; private set; }

        public float TimeSinceEnemyCreated => Time.time - TimeEnemyCreated;

        public Vector3? LastKnownPosition => KnownPlaces.LastKnownPlace?.Position;

        public float? LastKnownDistance => KnownPlaces.LastKnownPlace?.Distance(Bot.Position);

        public float? LastKnownDistanceSqr => KnownPlaces.LastKnownPlace?.DistanceSqr(Bot.Position);

        public EnemyKnownPlaces KnownPlaces { get; private set; }

        public float TimeLastKnownUpdated => KnownPlaces.TimeSinceLastKnownUpdated;

        public float TimeSinceLastKnownUpdated => Time.time - TimeLastKnownUpdated;

        public Vector3 EnemyPosition => EnemyTransform.Position;

        public Vector3 EnemyDirection => EnemyTransform.DirectionTo(Bot.Transform.Position);

        public Vector3 EnemyHeadPosition => EnemyTransform.HeadPosition;

        public Vector3 EnemyChestPosition => EnemyTransform.BodyPosition;

        public bool InLineOfSight => EnemyVision.InLineOfSight;

        public bool IsVisible => EnemyVision.IsVisible;

        public bool CanShoot => EnemyVision.CanShoot;

        public bool Seen => EnemyVision.Seen;

        public Vector3? LastCornerToEnemy => EnemyPath.LastCornerToEnemy;

        public bool EnemyLookingAtMe => EnemyStatus.EnemyLookingAtMe;

        public Vector3? LastSeenPosition => KnownPlaces.LastSeenPlace?.Position;

        public float VisibleStartTime => EnemyVision.VisibleStartTime;

        public float TimeSinceSeen => EnemyVision.TimeSinceSeen;

        public float RealDistance { get; private set; }

        private void updateRealDistance()
        {
            float timeAdd;
            if (IsCurrentEnemy)
            {
                timeAdd = 0.05f;
            }
            else if (ActiveThreat)
            {
                timeAdd = IsAI ? 0.5f : 0.2f;
            }
            else
            {
                timeAdd = IsAI ? 1f : 0.33f;
            }
            if (_lastUpdateDistanceTime + timeAdd < Time.time)
            {
                _lastUpdateDistanceTime = Time.time;
                RealDistance = (EnemyPlayerComponent.Position - Bot.Position).magnitude;
            }
        }

        public bool CanSeeLastCornerToEnemy => EnemyPath.CanSeeLastCornerToEnemy;

        public bool IsSniper { get; private set; }

        private void updateActiveState()
        {
            if (IsCurrentEnemy &&
                !_hasBeenActive)
            {
                _hasBeenActive = true;
            }

            if (IsCurrentEnemy || IsVisible || EnemyStatus.HeardRecently)
            {
                TimeLastActive = Time.time;
            }
        }

        private Vector3 findCenterMass()
        {
            PlayerComponent enemy = EnemyPlayerComponent;
            Vector3 headPos = enemy.Player.MainParts[BodyPartType.head].Position;
            Vector3 floorPos = enemy.Position;
            Vector3 centerMass = Vector3.Lerp(headPos, floorPos, SAINPlugin.LoadedPreset.GlobalSettings.Aiming.CenterMassVal);

            if (enemy.Player.IsYourPlayer && SAINPlugin.DebugMode && _debugCenterMassTime < Time.time)
            {
                _debugCenterMassTime = Time.time + 1f;
                DebugGizmos.Sphere(centerMass, 0.1f, 5f);
            }

            return centerMass;
        }

        public void UpdateHeardPosition(Vector3 position, bool wasGunfire, bool shallReport, bool arrived = false)
        {
            EnemyMoveDirection = EnemyPlayer.MovementContext.MovementDirection;

            EnemyPlace place = KnownPlaces.UpdatePersonalHeardPosition(position, arrived, wasGunfire);
            if (shallReport &&
                place != null &&
                _nextReportHeardTime < Time.time)
            {
                _nextReportHeardTime = Time.time + _reportHeardFreq;
                Bot.Squad?.SquadInfo?.ReportEnemyPosition(this, place, false);
            }
        }

        public void UpdateSeenPosition(Vector3 position)
        {
            EnemyMoveDirection = EnemyPlayer.MovementContext.MovementDirection;

            var place = KnownPlaces.UpdateSeenPlace(position);
            if (_nextReportSightTime < Time.time)
            {
                _nextReportSightTime = Time.time + _reportSightFreq;
                Bot.Squad.SquadInfo?.ReportEnemyPosition(this, place, true);
            }
        }

        public void EnemyPositionReported(EnemyPlace place, bool seen)
        {
            if (seen)
            {
                KnownPlaces.UpdateSquadSeenPlace(place);
            }
            else
            {
                KnownPlaces.UpdateSquadHeardPlace(place);
            }
        }

        public void SetHeardStatus(bool value, Vector3 pos, SAINSoundType soundType, bool shallReport)
        {
            if (value)
            {
                EnemyStatus.HeardRecently = true;
                TimeLastHeard = Time.time;
                if (!IsVisible)
                {
                    bool wasGunfire = soundType == SAINSoundType.SuppressedGunShot || soundType == SAINSoundType.Gunshot;
                    UpdateHeardPosition(pos, wasGunfire, shallReport);
                    OnEnemyHeard?.Invoke(this);
                    if (!wasGunfire &&
                        shallReport)
                    {
                        updateEnemyAction(soundType, pos);
                        talkNoise(soundType == SAINSoundType.Conversation);
                        if (!Bot.HasEnemy)
                        {
                            EnemyHeardFromPeace = true;
                        }
                    }
                }
            }
        }

        private void updateEnemyAction(SAINSoundType soundType, Vector3 soundPosition)
        {
            bool shallUpdateSquad = true;
            switch (soundType)
            {
                case SAINSoundType.GrenadeDraw:
                case SAINSoundType.GrenadePin:
                    EnemyStatus.EnemyHasGrenadeOut = true;
                    break;

                case SAINSoundType.Reload:
                case SAINSoundType.DryFire:
                    EnemyStatus.EnemyIsReloading = true;
                    break;

                case SAINSoundType.Looting:
                    EnemyStatus.EnemyIsLooting = true;
                    break;

                case SAINSoundType.Heal:
                    EnemyStatus.EnemyIsHealing = true;
                    break;

                case SAINSoundType.Surgery:
                    EnemyStatus.VulnerableAction = SAINComponent.Classes.Enemy.EEnemyAction.UsingSurgery;
                    break;

                default:
                    shallUpdateSquad = false;
                    break;
            }

            if (shallUpdateSquad)
            {
                Bot.Squad.SquadInfo.UpdateSharedEnemyStatus(EnemyIPlayer, EnemyStatus.VulnerableAction, Bot, soundType, soundPosition);
            }
        }

        private void talkNoise(bool conversation)
        {
            if (_nextSayNoise < Time.time
            && Bot.Talk.GroupTalk.FriendIsClose
            && Bot.Squad.BotInGroup
                && (Bot.Enemy == null || Bot.Enemy.TimeSinceSeen > 20f))
            {
                _nextSayNoise = Time.time + 12f;
                if (EFTMath.RandomBool(35))
                {
                    Bot.Talk.TalkAfterDelay(conversation ? EPhraseTrigger.OnEnemyConversation : EPhraseTrigger.NoisePhrase);
                }
            }
        }

        public void SetEnemyAsSniper(bool isSniper)
        {
            IsSniper = isSniper;
            if (isSniper && Bot.Squad.BotInGroup && Bot.Talk.GroupTalk.FriendIsClose)
            {
                Bot.Talk.TalkAfterDelay(EPhraseTrigger.SniperPhrase, ETagStatus.Combat, UnityEngine.Random.Range(0.33f, 0.66f));
            }
        }


        public void Dispose()
        {
            KnownPlaces?.Dispose();
            EnemyVision?.Dispose();
            EnemyPath?.Dispose();
            EnemyHearing?.Dispose();
            EnemyStatus?.Dispose();
        }

        private float _activeForPeriod = 180f;
        private float _activeForPeriodAI = 90f;
        private float _activeDistanceThreshold = 150f;
        private float _activeDistanceThresholdAI = 75f;
        private bool _hasBeenActive;
        private Vector3? _centerMass;
        private float _nextGetCenterTime;
        private static float _debugCenterMassTime;
        private Collider _hidingBehindObject;
        private const float _checkHidingRayDist = 3f;
        private const float _checkHidingFreq = 1f;
        private float _nextCheckHidingTime;
        private float _lastUpdateDistanceTime;
        private float _nextReportSightTime;
        private float _nextReportHeardTime;
        private const float _reportSightFreq = 0.25f;
        private const float _reportHeardFreq = 1f;
        private float _nextSayNoise;
    }
}