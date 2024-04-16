using EFT;
using SAIN.SAINComponent.BaseClasses;
using UnityEngine;
using UnityEngine.AI;
using static RootMotion.FinalIK.AimPoser;

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
        }

        public EnemyInfo EnemyInfo { get; private set; }
        public SAINPersonClass EnemyPerson { get; private set; }
        public SAINPersonTransformClass EnemyTransform => EnemyPerson.Transform;
        public bool IsCurrentEnemy => SAIN.HasEnemy && SAIN.EnemyController.ActiveEnemy == this;

        public void Init()
        {
        }

        public void Update()
        {
            if (!SAIN.HasEnemy)
            {
                SAIN.EnemyController.ClearEnemy();
                return;
            }

            bool isCurrent = IsCurrentEnemy;
            Vision.Update(isCurrent);
            Path.Update(isCurrent);
        }

        public readonly bool IsAI;

        public float LastActiveTime;

        private readonly float TimeSinceHeardTimeAdd = 8f;

        public bool Heard { get; private set; }

        public void SetHeardStatus(bool canHear, Vector3 pos)
        {
            HeardRecently = canHear;
            if (HeardRecently)
            {
                LastHeardPosition = pos;
                HasSeenLastKnownLocation = false;
            }
        }

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
                if (Heard && TimeSinceHeard + TimeSinceHeardTimeAdd < Time.time)
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
        }

        public bool HasSeenLastKnownLocation { get; private set; }

        public bool CheckIfSeenLastKnown()
        {
            if (!HasSeenLastKnownLocation && CheckLastSeenTimer < Time.time)
            {
                CheckLastSeenTimer = Time.time + 0.2f;
                if (LastKnownLocation != null)
                {
                    Vector3 lastknown = LastKnownLocation.Value;
                    Vector3 botPos = SAIN.Person.Transform.Head;
                    Vector3 direction = lastknown - botPos;
                    HasSeenLastKnownLocation = !Physics.Raycast(botPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI);
                }
            }
            return HasSeenLastKnownLocation;
        }

        private float CheckLastSeenTimer;

        public EnemyPathDistance CheckPathDistance() => Path.CheckPathDistance();

        // ActiveEnemy Properties
        public IPlayer EnemyIPlayer => EnemyPerson.IPlayer;

        public Player EnemyPlayer => EnemyPerson.Player;

        public float TimeEnemyCreated { get; private set; }

        public float TimeSinceEnemyCreated => Time.time - TimeEnemyCreated;

        public Vector3? LastKnownLocation
        {
            get
            {
                Vector3? result = null;
                if (IsVisible)
                {
                    result = EnemyPosition;
                }
                else if (Seen && !Heard)
                {
                    result = LastSeenPosition;
                }
                else if (!Seen && Heard)
                {
                    result = LastHeardPosition;
                }
                else if (Seen && Heard)
                {
                    if (TimeSinceSeen > TimeSinceHeard)
                    {
                        result = LastSeenPosition;
                    }
                    else
                    {
                        result = LastHeardPosition;
                    }
                }
                return result;
            }
        }

        public float TimeSinceLastKnownUpdated
        {
            get
            {
                if (Seen || Heard)
                {
                    return Mathf.Max(TimeSinceHeard, TimeSinceSeen);
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

        // PathToEnemy Properties
        public bool ArrivedAtLastSeenPosition => Path.HasArrivedAtLastSeen;
        public float RealDistance => Path.EnemyDistance;
        public bool CanSeeLastCornerToEnemy => Path.CanSeeLastCornerToEnemy;
        public float PathDistance => Path.PathDistance;
        public NavMeshPath NavMeshPath => Path.PathToEnemy;

        public SAINEnemyStatus EnemyStatus { get; private set; }
        public SAINEnemyVision Vision { get; private set; }
        public SAINEnemyPath Path { get; private set; }
    }
}