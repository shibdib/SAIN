using EFT;
using SAIN.Components.BotComponentSpace.Classes.EnemyClasses;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.Preset;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class Enemy : BotBaseClass, ISAINClass
    {
        public string EnemyName { get; }
        public string EnemyProfileId { get; }
        public PlayerComponent EnemyPlayerComponent { get; }
        public PersonClass EnemyPerson { get; }
        public PersonTransformClass EnemyTransform { get; }
        public bool IsAI => EnemyPlayer.IsAI;

        public bool IsCurrentEnemy { get; private set; }
        public float LastCheckLookTime { get; set; }
        public float RealDistance { get; private set; }
        public bool IsSniper { get; private set; }

        public EnemyEvents Events { get; }
        public EnemyKnownPlaces KnownPlaces { get; private set; }
        public SAINEnemyStatus Status { get; }
        public SAINEnemyVision Vision { get; }
        public SAINEnemyPath Path { get; }
        public EnemyInfo EnemyInfo { get; }
        public EnemyAim Aim { get; }
        public EnemyHearing Hearing { get; }

        private EnemyKnownChecker _knownChecker { get; }
        private EnemyActiveThreatChecker _activeThreatChecker { get; }
        private EnemyValidChecker _validChecker { get; }

        public Enemy(BotComponent bot, PlayerComponent playerComponent, EnemyInfo enemyInfo) : base(bot)
        {
            EnemyPlayerComponent = playerComponent;
            EnemyPerson = playerComponent.Person;
            EnemyTransform = playerComponent.Transform;
            EnemyName = $"{playerComponent.Name} ({playerComponent.Person.Nickname})";
            EnemyInfo = enemyInfo;
            EnemyProfileId = playerComponent.ProfileId;

            Events = new EnemyEvents(this);
            _activeThreatChecker = new EnemyActiveThreatChecker(this);
            _validChecker = new EnemyValidChecker(this);
            _knownChecker = new EnemyKnownChecker(this);
            Status = new SAINEnemyStatus(this);
            Vision = new SAINEnemyVision(this);
            Path = new SAINEnemyPath(this);
            KnownPlaces = new EnemyKnownPlaces(this);
            Aim = new EnemyAim(this);
            Hearing = new EnemyHearing(this);
        }

        public void Init()
        {
            base.SubscribeToPresetChanges(updatePresetSettings);

            Events.Init();
            _validChecker.Init();
            _knownChecker.Init();
            _activeThreatChecker.Init();
            KnownPlaces.Init();
            Vision.Init();
            Path.Init();
            Hearing.Init();
            Status.Init();
        }

        public void Update()
        {
            IsCurrentEnemy = Bot.Enemy?.EnemyProfileId == EnemyProfileId;

            Events.Update();
            _validChecker.Update();
            _knownChecker.Update();
            _activeThreatChecker.Update();

            updateRealDistance();
            updateActiveState();

            Vision.Update();
            KnownPlaces.Update();
            Path.Update();
            Status.Update();
        }

        public void Dispose()
        {
            base.UnSubscribeToPresetChanges();
            Events?.Dispose();
            _validChecker?.Dispose();
            _knownChecker?.Dispose();
            _activeThreatChecker?.Dispose();
            KnownPlaces?.Dispose();
            Vision?.Dispose();
            Path?.Dispose();
            Hearing?.Dispose();
            Status?.Dispose();
        }

        public bool EnemyKnown => Events.OnEnemyKnownChanged.Value;
        public bool EnemyNotLooking => IsVisible && !Status.EnemyLookingAtMe && !Status.ShotAtMeRecently;
        public bool IsValid => _validChecker.IsValid;
        public bool ActiveThreat => _activeThreatChecker.ActiveThreat;
        public float TimeSinceCurrentEnemy => _hasBeenActive ? Time.time - _timeLastActive : float.MaxValue;

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

                EnemyCornerDictionary corners = Path.EnemyCorners;

                Vector3? blindCorner = corners.EyeLevelPosition(ECornerType.Blind);
                if (blindCorner != null &&
                    isTargetInSuppRange(enemyLastKnown.Value, blindCorner.Value))
                {
                    return blindCorner;
                }

                Vector3? lastCorner = corners.EyeLevelPosition(ECornerType.Last);
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
            if ((target - suppressPoint).sqrMagnitude <= MAX_TARGET_SUPPRESS_DIST)
            {
                return true;
            }
            Vector3 directionToSuppPoint = suppressPoint - Bot.Position;
            Vector3 directionToTarget = target - Bot.Position;
            float angle = Vector3.Angle(directionToSuppPoint.normalized, directionToTarget.normalized);
            if (angle < MAX_TARGET_SUPPRESS_ANGLE)
            {
                return true;
            }
            return false;
        }

        private const float MAX_TARGET_SUPPRESS_DIST = 5f * 5f;
        private const float MAX_TARGET_SUPPRESS_ANGLE = 20f;

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

        public EPathDistance EPathDistance => Path.EPathDistance;
        public IPlayer EnemyIPlayer => EnemyPlayerComponent.IPlayer;
        public Player EnemyPlayer => EnemyPlayerComponent.Player;

        public Vector3? LastKnownPosition => KnownPlaces.LastKnownPosition;
        public float TimeSinceLastKnownUpdated => KnownPlaces.TimeSinceLastKnownUpdated;
        public Vector3 EnemyMoveDirection => EnemyPlayer.MovementContext.MovementDirection;


        public Vector3 EnemyPosition => EnemyTransform.Position;
        public Vector3 EnemyDirection => EnemyTransform.DirectionToMe(Bot.Transform.Position);
        public Vector3 EnemyHeadPosition => EnemyTransform.HeadPosition;

        public bool InLineOfSight => Vision.InLineOfSight;
        public bool IsVisible => Vision.IsVisible;
        public bool CanShoot => Vision.CanShoot;
        public bool Seen => Vision.Seen;
        public bool Heard => Hearing.Heard;

        public bool EnemyLookingAtMe => Status.EnemyLookingAtMe;
        public float TimeSinceSeen => Vision.TimeSinceSeen;
        public float TimeSinceHeard => Hearing.TimeSinceHeard;

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

        private void updateActiveState()
        {
            if (IsCurrentEnemy &&
                !_hasBeenActive)
            {
                _hasBeenActive = true;
            }

            if (IsCurrentEnemy || IsVisible || Status.HeardRecently)
            {
                _timeLastActive = Time.time;
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

        public void SetEnemyAsSniper(bool isSniper)
        {
            IsSniper = isSniper;
            if (isSniper && Bot.Squad.BotInGroup && Bot.Talk.GroupTalk.FriendIsClose)
            {
                Bot.Talk.TalkAfterDelay(EPhraseTrigger.SniperPhrase, ETagStatus.Combat, UnityEngine.Random.Range(0.33f, 0.66f));
            }
        }

        private void updatePresetSettings(SAINPresetClass preset)
        {

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
        private const float _reportSightFreq = 0.5f;
        private float _timeLastActive;
    }
}