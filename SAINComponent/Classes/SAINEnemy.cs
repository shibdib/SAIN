using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.BaseClasses;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static RootMotion.FinalIK.AimPoser;
using static SAIN.SAINComponent.Classes.SAINEnemy;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemy : SAINBase, ISAINClass
    {
        public SAINEnemy(SAINComponentClass bot, SAINPersonClass person, EnemyInfo enemyInfo) : base(bot)
        {
            TimeEnemyCreated = Time.time;
            EnemyPerson = person;
            EnemyInfo = enemyInfo;
            IsAI = enemyInfo.Person?.IsAI == true;

            EnemyStatus = new SAINEnemyStatus(this);
            Vision = new SAINEnemyVision(this);
            Path = new SAINEnemyPath(this);
            KnownPlaces = new EnemyKnownPlaces(this);
        }

        public EnemyInfo EnemyInfo { get; private set; }
        public SAINPersonClass EnemyPerson { get; private set; }
        public SAINPersonTransformClass EnemyTransform => EnemyPerson.Transform;
        public bool IsCurrentEnemy => SAIN.HasEnemy && SAIN.EnemyController.ActiveEnemy == this;

        public void Init()
        {
        }

        public bool FirstContactOccured => Vision.FirstContactOccured;

        public bool FirstContactReported = false;
        public bool ShallReportRepeatContact => Vision.ShallReportRepeatContact;

        public void DeleteInfo(EDamageType _)
        {
            SAIN.EnemyController.RemoveEnemy(EnemyPlayer);
        }

        public bool SearchStarted { get; set; } = false;
        public int TimesSearchedStarted { get; set; } = 0;

        public bool EnemyIsSuppressed
        {
            get
            {
                return _suppressEndTimer < Time.time;
            }
            set
            {
                _suppressEndTimer = value ? Time.time + 2f : 0f;
            }
        }

        private float _suppressEndTimer;

        public void Update()
        {
            if (!SAIN.HasEnemy)
            {
                SAIN.EnemyController.ClearEnemy();
                return;
            }
            if (EnemyPlayer == null)
            {
                DeleteInfo(default);
                return;
            }

            if (EnemyInfo?.ShallKnowEnemy() == true || EnemyPlayer.IsAI == false)
            {
                bool isCurrent = IsCurrentEnemy;
                Vision.Update(isCurrent);
                Path.Update(isCurrent);
                KnownPlaces.Update();

                if (isCurrent)
                {
                    //KnownPlaces.Update();
                }
            }
            else
            {
                Vision.UpdateVisible(false);
                Vision.UpdateCanShoot(false);
                Path.Clear();
            }
        }

        private float _nextUpdateDistTime;

        public void UpdateKnownPosition(Vector3 position, bool arrived = false, bool seen = false)
        {
            KnownPlaces.AddPosition(position, arrived, seen);
            SAIN.Squad?.SquadInfo?.ReportEnemyPosition(this, position);
        }

        public void EnemyPositionReported(Vector3 position, bool arrived = false, bool seen = false)
        {
            KnownPlaces.AddPosition(position, arrived, seen);
        }

        public readonly bool IsAI;

        public float LastActiveTime;

        private readonly float TimeCantHearAnymore = 3f;

        public bool Heard { get; private set; }

        public void SetHeardStatus(bool canHear, Vector3 pos, bool isTalked = false)
        {
            HeardRecently = canHear;
            if (canHear)
            {
                UpdateKnownPosition(pos);
                LastHeardPosition = new Vector3?(pos);
                if (_nextSayNoise < Time.time 
                    && (SAIN.Talk.GroupTalk.FriendIsClose 
                        || SAIN.Equipment.HasEarPiece) 
                    && SAIN.Squad.BotInGroup 
                    && (SAIN.Enemy == null 
                        || SAIN.Enemy.TimeSinceSeen > 10f))
                {
                    _nextSayNoise = Time.time + 8f;
                    if (EFTMath.RandomBool(40))
                    {
                        SAIN.Talk.TalkAfterDelay(isTalked ? EPhraseTrigger.OnEnemyConversation : EPhraseTrigger.NoisePhrase);
                    }
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
                if (Heard && TimeSinceHeard > TimeCantHearAnymore)
                {
                    _heardRecently = false;
                }
                return _heardRecently;
            }
            set
            {
                if (value)
                {
                    Heard = true;
                    TimeLastHeard = Time.time;
                }
                _heardRecently = value;
            }
        }

        private bool _heardRecently;

        public float TimeSinceHeard => Heard ? Time.time - TimeLastHeard : float.MaxValue;
        public float TimeLastHeard { get; private set; }
        public Vector3? LastHeardPosition { get; private set; }

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

        public float TimeSinceLastKnownUpdated
        {
            get
            {
                EnemyPlace lastKnown = KnownPlaces.LastKnownPlace;
                if (lastKnown != null)
                {
                    return Time.time - lastKnown.TimePositionUpdated;
                }
                return float.MaxValue;
            }
        }

        public Vector3 EnemyPosition => EnemyTransform.Position;

        public Vector3 EnemyDirection => EnemyTransform.Direction(SAIN.Transform.Position);

        public Vector3 EnemyHeadPosition => EnemyTransform.Head;

        public Vector3 EnemyChestPosition => EnemyTransform.Chest;

        // Look Properties
        public bool InLineOfSight => Vision.InLineOfSight;

        public bool IsVisible => Vision.IsVisible;
        public bool CanShoot => Vision.CanShoot;
        public bool Seen => Vision.Seen;
        public Vector3? LastCornerToEnemy => Vision.LastSeenPosition;
        public float LastChangeVisionTime => Vision.VisibleStartTime;
        public bool EnemyLookingAtMe => EnemyStatus.EnemyLookingAtMe;
        public Vector3? LastSeenPosition => Vision.LastSeenPosition;
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