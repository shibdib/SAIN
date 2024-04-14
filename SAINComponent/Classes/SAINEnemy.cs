using EFT;
using SAIN.SAINComponent.BaseClasses;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemy : SAINBase, ISAINClass
    {
        public SAINEnemy(SAINComponentClass bot, SAINPersonClass person, EnemyInfo enemyInfo) : base(bot)
        {
            TimeEnemyCreated = Time.time;
            EnemyPerson = person;
            EnemyInfo = enemyInfo;

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

            UpdateHearStatus();
        }

        public void UpdateHearStatus()
        {
            if (CanBeHeard && TimeSinceHeard + TimeSinceHeardTimeAdd < Time.time)
            {
                SetHeardStatus(false, Vector3.zero);
            }
        }

        public float LastActiveTime;

        private readonly float TimeSinceHeardTimeAdd = 10f;

        public void SetHeardStatus(bool canHear, Vector3 pos)
        {
            CouldBeHeard = CanBeHeard;
            CanBeHeard = canHear;
            if (canHear)
            {
                LastHeardPosition = pos;
            }
            if (canHear == false && CouldBeHeard == true)
            {
                TimeLastHeard = Time.time;
            }
        }

        private bool CouldBeHeard;
        public bool CanBeHeard { get; set; }
        public float TimeSinceHeard { get; set; }
        public float TimeLastHeard { get; private set; }
        public Vector3 LastHeardPosition { get; private set; }

        public void Dispose()
        {
        }

        public EnemyPathDistance CheckPathDistance() => Path.CheckPathDistance();

        // ActiveEnemy Properties
        public IPlayer EnemyIPlayer => EnemyPerson.IPlayer;

        public Player EnemyPlayer => EnemyPerson.Player;

        public float TimeEnemyCreated { get; private set; }

        public float TimeSinceEnemyCreated => Time.time - TimeEnemyCreated;

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