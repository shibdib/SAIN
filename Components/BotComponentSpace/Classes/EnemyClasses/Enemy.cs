using EFT;
using SAIN.Components.BotComponentSpace.Classes.EnemyClasses;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class Enemy : SAINBase, ISAINClass
    {
        public event Action<Enemy, SAINSoundType, bool> OnEnemyHeard;

        public void Init()
        {
            ValidChecker.Init();
            EnemyKnownChecker.Init();
            ActiveThreatChecker.Init();

            KnownPlaces.Init();
            Vision.Init();
            Path.Init();
            Hearing.Init();
            Status.Init();
        }

        public void Update()
        {
            IsCurrentEnemy = Bot.Enemy?.EnemyProfileId == EnemyProfileId;

            ValidChecker.Update();
            EnemyKnownChecker.Update();
            ActiveThreatChecker.Update();

            updateRealDistance();
            updateActiveState();

            Vision.Update();
            KnownPlaces.Update();
            Path.Update();
            Status.Update();
        }

        public void Dispose()
        {
            ValidChecker?.Dispose();
            EnemyKnownChecker?.Dispose();
            ActiveThreatChecker?.Dispose();
            KnownPlaces?.Dispose();
            Vision?.Dispose();
            Path?.Dispose();
            Hearing?.Dispose();
            Status?.Dispose();
        }

        public bool EnemyKnown => EnemyKnownChecker.EnemyKnown;

        public readonly string EnemyName;
        public readonly string EnemyProfileId;
        public readonly bool IsAI;

        public PlayerComponent EnemyPlayerComponent { get; private set; }
        public SAINEnemyStatus Status { get; private set; }
        public SAINEnemyVision Vision { get; private set; }
        public SAINEnemyPath Path { get; private set; }
        public EnemyInfo EnemyInfo { get; private set; }
        public EnemyAim Aim { get; private set; }
        public EnemyHearing Hearing { get; private set; }

        public EnemyKnownChecker EnemyKnownChecker { get; private set; }
        public EnemyActiveThreatChecker ActiveThreatChecker { get; private set; }
        public EnemyValidChecker ValidChecker { get; private set; }

        public PersonClass EnemyPerson => EnemyPlayerComponent.Person;
        public PersonTransformClass EnemyTransform => EnemyPlayerComponent.Transform;

        public Enemy(BotComponent bot, PlayerComponent playerComponent, EnemyInfo enemyInfo) : base(bot)
        {
            EnemyPlayerComponent = playerComponent;
            EnemyName = $"{playerComponent.Name} ({playerComponent.Person.Nickname})";
            EnemyInfo = enemyInfo;
            IsAI = playerComponent.IsAI;
            EnemyProfileId = playerComponent.ProfileId;
            ActiveThreatChecker = new EnemyActiveThreatChecker(this);
            ValidChecker = new EnemyValidChecker(this);
            EnemyKnownChecker = new EnemyKnownChecker(this);
            Status = new SAINEnemyStatus(this);
            Vision = new SAINEnemyVision(this);
            Path = new SAINEnemyPath(this);
            KnownPlaces = new EnemyKnownPlaces(this);
            Aim = new EnemyAim(this);
            Hearing = new EnemyHearing(this);
        }

        public float LastCheckLookTime { get; set; }

        public bool EnemyNotLooking => IsVisible && !Status.EnemyLookingAtMe && !Status.ShotAtMeRecently;

        public bool IsCurrentEnemy { get; private set; }

        public bool IsValid => ValidChecker.IsValid;
        public bool ActiveThreat => ActiveThreatChecker.ActiveThreat;

        public float TimeLastActive { get; private set; } = 0f;
        public float TimeSinceCurrentEnemy => _hasBeenActive ? Time.time - TimeLastActive : float.MaxValue;

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
                Vector3? enemyLastKnown = KnownPlaces.LastKnownPosition;
                if (enemyLastKnown == null)
                {
                    return null;
                }

                Vector3? blindCorner = Path.BlindCornerToEnemy;
                if (blindCorner != null &&
                    isTargetInSuppRange(enemyLastKnown.Value, blindCorner.Value))
                {
                    return blindCorner;
                }

                Vector3? lastCorner = Path.LastCornerToEnemyEyeLevel;
                if (lastCorner != null &&
                    Path.CanSeeLastCornerToEnemy &&
                    isTargetInSuppRange(enemyLastKnown.Value, lastCorner.Value))
                {
                    return lastCorner;
                }

                if (HidingBehindObject != null)
                {
                    Vector3 pos = HidingBehindObject.transform.position + HidingBehindObject.bounds.size.z * Vector3.up;
                    if (isTargetInSuppRange(enemyLastKnown.Value, pos))
                    {
                        return pos;
                    }
                }
                return null;
            }
        }

        private bool isTargetInSuppRange(Vector3 target, Vector3 suppressPoint)
        {
            return (target - suppressPoint).sqrMagnitude <= MAX_TARGET_SUPPRESS_DIST;
        }

        private const float MAX_TARGET_SUPPRESS_DIST = 5f * 5f;

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

        public bool FirstContactOccured => Vision.FirstContactOccured;

        public bool FirstContactReported = false;
        public bool ShallReportRepeatContact => Vision.ShallReportRepeatContact;

        public bool EnemyHeardFromPeace = false;
        public bool Heard { get; set; }
        public float TimeSinceHeard => Time.time - TimeLastHeard;
        public float TimeLastHeard { get; private set; }

        public EPathDistance EPathDistance => Path.EPathDistance;
        public IPlayer EnemyIPlayer => EnemyPlayerComponent.IPlayer;
        public Player EnemyPlayer => EnemyPlayerComponent.Player;

        public Vector3? LastKnownPosition => KnownPlaces.LastKnownPosition;
        public float TimeSinceLastKnownUpdated => KnownPlaces.TimeSinceLastKnownUpdated;
        public Vector3 EnemyMoveDirection => EnemyPlayer.MovementContext.MovementDirection;

        public EnemyKnownPlaces KnownPlaces { get; private set; }

        public Vector3 EnemyPosition => EnemyTransform.Position;
        public Vector3 EnemyDirection => EnemyTransform.DirectionToMe(Bot.Transform.Position);
        public Vector3 EnemyHeadPosition => EnemyTransform.HeadPosition;

        public bool InLineOfSight => Vision.InLineOfSight;
        public bool IsVisible => Vision.IsVisible;
        public bool CanShoot => Vision.CanShoot;
        public bool Seen => Vision.Seen;

        public bool EnemyLookingAtMe => Status.EnemyLookingAtMe;
        public float TimeSinceSeen => Vision.TimeSinceSeen;
        public float RealDistance { get; private set; }

        private void updateRealDistance()
        {
            float timeAdd;
            if (IsCurrentEnemy)
            {
                timeAdd = 0.05f;
            }
            else if (EnemyKnown)
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

        public bool IsSniper { get; private set; }

        private void updateActiveState()
        {
            if (IsCurrentEnemy &&
                !_hasBeenActive)
            {
                _hasBeenActive = true;
            }

            if (IsCurrentEnemy || IsVisible || Status.HeardRecently)
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

        public void UpdateHeardPosition(Vector3 position, bool wasGunfire, bool shallReport)
        {
            EnemyPlace place = KnownPlaces.UpdatePersonalHeardPosition(position, wasGunfire);
            if (shallReport &&
                place != null &&
                _nextReportHeardTime < Time.time)
            {
                _nextReportHeardTime = Time.time + _reportHeardFreq;
                Bot.Squad?.SquadInfo?.ReportEnemyPosition(this, place, false);
            }
        }

        public void UpdateLastSeenPosition(Vector3 position)
        {
            var place = KnownPlaces.UpdateSeenPlace(position);
            Bot.Squad.SquadInfo?.ReportEnemyPosition(this, place, true);
        }

        public void UpdateCurrentEnemyPos(Vector3 position)
        {
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
            if (IsVisible)
            {
                return;
            }
            if (!value)
            {
                return;
            }

            Status.HeardRecently = true;
            TimeLastHeard = Time.time;

            bool wasGunfire = soundType.IsGunShot();
            UpdateHeardPosition(pos, wasGunfire, shallReport);
            OnEnemyHeard?.Invoke(this, soundType, wasGunfire);

            if (!Bot.HasEnemy)
                EnemyHeardFromPeace = true;

            if (wasGunfire || !shallReport)
            {
                return;
            }
            updateEnemyAction(soundType, pos);
            talkNoise(soundType == SAINSoundType.Conversation);
        }

        private void updateEnemyAction(SAINSoundType soundType, Vector3 soundPosition)
        {
            EEnemyAction action = EEnemyAction.None;
            switch (soundType)
            {
                case SAINSoundType.GrenadeDraw:
                case SAINSoundType.GrenadePin:
                    action = EEnemyAction.HasGrenade;
                    break;

                case SAINSoundType.Reload:
                case SAINSoundType.DryFire:
                    action = EEnemyAction.Reloading;
                    break;

                case SAINSoundType.Looting:
                    action = EEnemyAction.Looting;
                    break;

                case SAINSoundType.Heal:
                    action = EEnemyAction.Healing;
                    break;

                case SAINSoundType.Surgery:
                    action = EEnemyAction.UsingSurgery;
                    break;

                default:
                    break;
            }

            if (action != EEnemyAction.None)
            {
                Status.SetVulnerableAction(action);
                Bot.Squad.SquadInfo.UpdateSharedEnemyStatus(EnemyIPlayer, action, Bot, soundType, soundPosition);
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

        public float NextCheckFlashLightTime;

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
        private const float _reportSightFreq = 0.5f;
        private const float _reportHeardFreq = 1f;
        private float _nextSayNoise;
    }
}