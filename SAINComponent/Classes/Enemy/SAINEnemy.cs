using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.BaseClasses;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static SAIN.SAINComponent.Classes.Enemy.SAINEnemy;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class SAINEnemy : SAINBase, ISAINClass
    {
        public SAINEnemy(SAINComponentClass bot, SAINPersonClass person, EnemyInfo enemyInfo) : base(bot)
        {
            TimeEnemyCreated = Time.time;
            EnemyPerson = person;
            EnemyTransform = person.Transform;
            EnemyInfo = enemyInfo;
            IsAI = enemyInfo.Person?.IsAI == true;
            EnemyProfileId = person.ProfileId;

            EnemyStatus = new SAINEnemyStatus(this);
            Vision = new SAINEnemyVision(this);
            Path = new SAINEnemyPath(this);
            KnownPlaces = new EnemyKnownPlaces(this);
        }

        public bool IsValid
        {
            get
            {
                if (EnemyPerson?.PlayerNull == true)
                {
                    //Logger.LogDebug("Enemy Player is Null. Removing...");
                    return false;
                }
                // Redundant Checks
                // Common checks between PMC and bots
                if (EnemyPlayer == null)
                {
                    //Logger.LogDebug("Enemy is Null. Removing...");
                    return false;
                }
                if (EnemyPlayer?.HealthController?.IsAlive == false)
                {
                    //Logger.LogDebug("Enemy is Dead. Removing...");
                    return false;
                }
                // Checks specific to bots
                BotOwner botOwner = EnemyPlayer?.AIData?.BotOwner;
                if (EnemyPlayer?.IsAI == true && botOwner == null)
                {
                    Logger.LogDebug("Enemy is AI, but Bot is null. Removing...");
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

        public readonly string EnemyProfileId;
        public EnemyInfo EnemyInfo { get; private set; }
        public SAINPersonClass EnemyPerson { get; private set; }
        public SAINPersonTransformClass EnemyTransform { get; private set; }
        public bool IsCurrentEnemy => SAIN.HasEnemy && SAIN.Enemy.EnemyProfileId == EnemyProfileId;

        public void Init()
        {
        }

        public bool FirstContactOccured => Vision.FirstContactOccured;

        public bool FirstContactReported = false;
        public bool ShallReportRepeatContact => Vision.ShallReportRepeatContact;

        public void DeleteInfo(EDamageType _)
        {
            SAIN.EnemyController.RemoveEnemy(EnemyProfileId);
        }

        public bool ShallUpdateEnemy => EnemyPerson?.IsActive == true && (EnemyInfo?.ShallKnowEnemy() == true || EnemyPlayer.IsAI == false);

        public bool IsUpdating { get; private set; }

        public void Update()
        {
            if (ShallUpdateEnemy)
            {
                bool isCurrent = IsCurrentEnemy;
                Vision.Update(isCurrent);
                Path.Update(isCurrent);
                KnownPlaces.Update(isCurrent);
            }
            else
            {
                Vision.UpdateVisible(true);
                Vision.UpdateCanShoot(true);
                Path.Clear();
            }
        }

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
                        && Physics.Raycast(lastKnown.Value + Vector3.up, SAIN.Position + Vector3.up, out RaycastHit hit, _checkHidingRayDist, LayerMaskClass.HighPolyCollider))
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
                if (EnemyPerson.IPlayer == null)
                {
                    return null;
                }
                if (_nextGetCenterTime < Time.time)
                {
                    _nextGetCenterTime = Time.time + 0.1f;
                    _centerMass = new Vector3?(findCenterMass(EnemyPerson.IPlayer));
                }
                return _centerMass;
            }
        }

        private static Vector3 findCenterMass(IPlayer person)
        {
            Vector3 headPos = person.MainParts[BodyPartType.head].Position;
            Vector3 floorPos = person.Position;
            Vector3 centerMass = Vector3.Lerp(headPos, floorPos, 0.3125f);

            if (person.IsYourPlayer && SAINPlugin.DebugMode && _debugCenterMassTime < Time.time)
            {
                _debugCenterMassTime = Time.time + 1f;
                DebugGizmos.Sphere(centerMass, 0.1f, 5f);
            }

            return centerMass;
        }

        public void UpdateHeardPosition(Vector3 position, bool wasGunfire, bool arrived = false)
        {
            EnemyPlace place = KnownPlaces.AddPersonalHeardPlace(position, arrived, wasGunfire);
            if (_nextReportHeardTime < Time.time)
            {
                _nextReportHeardTime = Time.time + _reportHeardFreq;
                SAIN.Squad?.SquadInfo?.ReportEnemyPosition(this, place, false);
            }
        }

        public void UpdateSeenPosition(Vector3 position)
        {
            var place = KnownPlaces.UpdateSeenPlace(position);
            if (_nextReportSightTime < Time.time)
            {
                _nextReportSightTime = Time.time + _reportSightFreq;
                SAIN.Squad.SquadInfo?.ReportEnemyPosition(this, place, true);
            }
        }

        private Vector3? _centerMass;
        private float _nextGetCenterTime;
        private static float _debugCenterMassTime;
        private Collider _hidingBehindObject;
        private const float _checkHidingRayDist = 3f;
        private const float _checkHidingFreq = 1f;
        private float _nextCheckHidingTime;
        private float _nextUpdateDistTime;
        private float _nextReportSightTime;
        private float _nextReportHeardTime;
        private const float _reportSightFreq = 0.25f;
        private const float _reportHeardFreq = 1f;

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

        public readonly bool IsAI;

        public float LastActiveTime;

        public bool Heard { get; private set; }

        public Action<SAINEnemy> OnEnemyHeard;

        public void SetHeardStatus(bool value, Vector3 pos, SAINSoundType soundType)
        {
            if (value)
            {
                if (!Heard)
                {
                    Heard = true;
                }
                HeardRecently = true;
                TimeLastHeard = Time.time;
                bool wasGunfire = soundType == SAINSoundType.SuppressedGunShot || soundType == SAINSoundType.Gunshot;
                UpdateHeardPosition(pos, wasGunfire);
                OnEnemyHeard?.Invoke(this);
                if (!wasGunfire)
                {
                    talkNoise(soundType == SAINSoundType.Conversation);
                }
            }
        }

        private void talkNoise(bool conversation)
        {
            if (_nextSayNoise < Time.time
                && (SAIN.Talk.GroupTalk.FriendIsClose || SAIN.Equipment.HasEarPiece)
                && SAIN.Squad.BotInGroup
                && (SAIN.Enemy == null || SAIN.Enemy.TimeSinceSeen > 15f))
            {
                _nextSayNoise = Time.time + 8f;
                if (EFTMath.RandomBool(40))
                {
                    SAIN.Talk.TalkAfterDelay(conversation ? EPhraseTrigger.OnEnemyConversation : EPhraseTrigger.NoisePhrase);
                }
            }
        }

        private float _nextSayNoise;

        public bool IsSniper { get; private set; }

        public void SetEnemyAsSniper(bool isSniper)
        {
            IsSniper = isSniper;
            if (isSniper && SAIN.Squad.BotInGroup && SAIN.Talk.GroupTalk.FriendIsClose)
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.SniperPhrase, ETagStatus.Combat, UnityEngine.Random.Range(0.5f, 1f));
            }
        }

        public bool HeardRecently
        {
            get
            {
                return _heardRecently.Value;
            }
            set
            {
                _heardRecently.Value = value;
            }
        }

        private readonly ExpirableBool _heardRecently = new ExpirableBool(2f, 0.85f, 1.15f);

        public float TimeSinceHeard => Time.time - TimeLastHeard;
        public float TimeLastHeard { get; private set; }

        public Vector3? LastHeardPosition
        {
            get
            {
                var lastHeardPlace = KnownPlaces.LastHeardPlace;
                if (lastHeardPlace != null)
                {
                    return lastHeardPlace.Position;
                }
                return null;
            }
        }

        public void Dispose()
        {
            KnownPlaces?.Dispose();
        }

        public EnemyPathDistance CheckPathDistance() => Path.CheckPathDistance();

        // ActiveEnemy Properties
        public IPlayer EnemyIPlayer => EnemyPerson.IPlayer;

        public Player EnemyPlayer => EnemyPerson.Player;
        public float TimeEnemyCreated { get; private set; }
        public float TimeSinceEnemyCreated => Time.time - TimeEnemyCreated;
        public Vector3? LastKnownPosition => KnownPlaces.LastKnownPlace?.Position;

        public EnemyKnownPlaces KnownPlaces { get; private set; }

        public float TimeLastKnownUpdated
        {
            get
            {
                EnemyPlace lastKnown = KnownPlaces.LastKnownPlace;
                if (lastKnown != null)
                {
                    return lastKnown.TimePositionUpdated;
                }
                return float.MaxValue;
            }
        }

        public float TimeSinceLastKnownUpdated => Time.time - TimeLastKnownUpdated;

        public Vector3 EnemyPosition => EnemyTransform.Position;
        public Vector3 EnemyDirection => EnemyTransform.Direction(SAIN.Transform.Position);
        public Vector3 EnemyHeadPosition => EnemyTransform.HeadPosition;
        public Vector3 EnemyChestPosition => EnemyTransform.CenterPosition;

        // Look Properties
        public bool InLineOfSight => Vision.InLineOfSight;

        public bool IsVisible => Vision.IsVisible;
        public bool CanShoot => Vision.CanShoot;
        public bool Seen => Vision.Seen;
        public Vector3? LastCornerToEnemy => Path.LastCornerToEnemy;
        public float LastChangeVisionTime => Vision.LastChangeVisionTime;
        public bool EnemyLookingAtMe => EnemyStatus.EnemyLookingAtMe;
        public Vector3? LastSeenPosition => KnownPlaces.LastSeenPlace?.Position;
        public float VisibleStartTime => Vision.VisibleStartTime;
        public float TimeSinceSeen => Vision.TimeSinceSeen;

        public float RealDistance
        {
            get
            {
                if (_nextUpdateDistTime < Time.time)
                {
                    _nextUpdateDistTime = Time.time + 0.1f;
                    _realDistance = (EnemyPerson.Transform.Position - SAIN.Position).magnitude;
                }
                return _realDistance;
            }
        }

        private float _realDistance;
        public bool CanSeeLastCornerToEnemy => Path.CanSeeLastCornerToEnemy;
        public NavMeshPath PathToEnemy => Path.PathToEnemy;
        public SAINEnemyStatus EnemyStatus { get; private set; }
        public SAINEnemyVision Vision { get; private set; }
        public SAINEnemyPath Path { get; private set; }
    }
}