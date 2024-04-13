using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyPath : EnemyBase
    {
        public SAINEnemyPath(SAINEnemy enemy) : base(enemy)
        {
        }

        public void Update(bool isCurrentEnemy)
        {
            if (!isCurrentEnemy)
            {
                LastCornerToEnemy = null;
                CanSeeLastCornerToEnemy = false;
                return;
            }

            if (CheckPathTimer < Time.time)
            {
                CheckPathTimer = Time.time + 0.35f;
                CalcPath();
            }
        }

        public NavMeshPathStatus PathToEnemyStatus { get; private set; }

        private void CalcPath()
        {
            bool hasLastCorner = false;
            float pathDistance = Mathf.Infinity;

            Vector3 enemyPosition;
            if (Enemy.LastSeenPosition != null)
            {
                enemyPosition = Enemy.LastSeenPosition.Value;
            }
            else
            {
                enemyPosition = EnemyPosition;
            }

            PathToEnemy.ClearCorners();
            if (NavMesh.CalculatePath(SAIN.Position, enemyPosition, -1, PathToEnemy))
            {
                pathDistance = PathToEnemy.CalculatePathLength();
                if (PathToEnemy.corners.Length > 2)
                {
                    hasLastCorner = true;
                }
            }

            PathDistance = pathDistance;
            PathToEnemyStatus = PathToEnemy.status;

            if (hasLastCorner && PathToEnemyStatus == NavMeshPathStatus.PathComplete)
            {
                Vector3 lastCorner = PathToEnemy.corners[PathToEnemy.corners.Length - 2];
                LastCornerToEnemy = lastCorner;

                Vector3 cornerRay = lastCorner + Vector3.up * 1f;
                Vector3 headPos = SAIN.Transform.Head;
                Vector3 direction = cornerRay - headPos;

                CanSeeLastCornerToEnemy = !Physics.Raycast(headPos, direction.normalized, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);
            }
            else
            {
                LastCornerToEnemy = null;
            }
        }

        private void UpdateDistance()
        {
        }

        public EnemyPathDistance CheckPathDistance()
        {
            const float VeryCloseDist = 8f;
            const float CloseDist = 35f;
            const float FarDist = 100f;
            const float VeryFarDist = 150f;

            EnemyPathDistance pathDistance;
            float distance = PathDistance;

            if (distance <= VeryCloseDist)
            {
                pathDistance = EnemyPathDistance.VeryClose;
            }
            else if (distance <= CloseDist)
            {
                pathDistance = EnemyPathDistance.Close;
            }
            else if (distance <= FarDist)
            {
                pathDistance = EnemyPathDistance.Mid;
            }
            else if (distance <= VeryFarDist)
            {
                pathDistance = EnemyPathDistance.Far;
            }
            else
            {
                pathDistance = EnemyPathDistance.VeryFar;
            }

            return pathDistance;
        }

        public float DistanceToEnemy(Vector3 point) => Vector.DistanceBetween(Enemy.EnemyPosition, point);

        public float DistanceToMe(Vector3 point) => Vector.DistanceBetween(SAIN.Transform.Position, point);

        public bool HasArrivedAtLastSeen => !Enemy.EnemyPerson.PlayerNull && Enemy.Seen && !Enemy.IsVisible && (MyDistanceFromLastSeen < 2f || VisitedLastSeenPosition);
        private bool VisitedLastSeenPosition => !Enemy.EnemyPerson.PlayerNull && Enemy.Seen && !Enemy.IsVisible && (MyDistanceFromLastSeen < 2f || VisitedLastSeenPosition);
        public float EnemyDistanceFromLastSeen => Enemy.LastSeenPosition != null ? DistanceToEnemy(Enemy.LastSeenPosition.Value) : 0f;
        public float EnemyDistance => DistanceToMe(Enemy.EnemyPosition);
        public float MyDistanceFromLastSeen => Enemy.LastSeenPosition != null ? DistanceToMe(Enemy.LastSeenPosition.Value) : EnemyDistance;
        public float PathDistance { get; private set; }
        public Vector3? LastCornerToEnemy { get; private set; }
        public bool CanSeeLastCornerToEnemy { get; private set; }

        public readonly NavMeshPath PathToEnemy = new NavMeshPath();

        private float CheckPathTimer = 0f;
    }
}