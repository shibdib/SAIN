using Comfort.Common;
using EFT;
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

            if (isCurrent)
            {
                KnownPlaces.Update();
            }
        }

        public void UpdateKnownPosition(Vector3 position, bool arrived = false, bool seen = false)
        {
            KnownPlaces.AddPosition(position, arrived, seen);
        }

        public readonly bool IsAI;

        public float LastActiveTime;

        private readonly float TimeCantHearAnymore = 3f;

        public bool Heard { get; private set; }

        public void SetHeardStatus(bool canHear, Vector3 pos)
        {
            HeardRecently = canHear;
            if (canHear)
            {
                UpdateKnownPosition(pos);
                LastHeardPosition = new Vector3?(pos);
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
        }

        public EnemyPathDistance CheckPathDistance() => Path.CheckPathDistance();

        // ActiveEnemy Properties
        public IPlayer EnemyIPlayer => EnemyPerson.IPlayer;

        public Player EnemyPlayer => EnemyPerson.Player;

        public float TimeEnemyCreated { get; private set; }

        public float TimeSinceEnemyCreated => Time.time - TimeEnemyCreated;

        public Vector3? LastKnownPosition => KnownPlaces.LastKnownPlace?.Position;

        public EnemyKnownPlaces KnownPlaces { get; private set; }

        public float TimeSinceLastKnownUpdated
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