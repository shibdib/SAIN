using EFT;
using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyPath : EnemyBase
    {
        public SAINEnemyPath(SAINEnemy enemy) : base(enemy)
        {
        }

        public void Update(bool isCurrentEnemy)
        {
            float timeAdd = 0.5f;
            if (!isCurrentEnemy && Enemy.IsAI)
            {
                timeAdd = 4f;
            }
            if (!isCurrentEnemy && !Enemy.IsAI)
            {
                timeAdd = 0.5f;
            }
            if (isCurrentEnemy && !Enemy.IsAI)
            {
                timeAdd = 0.2f;
            }

            if (CheckPathTimer + timeAdd < Time.time)
            {
                CheckPathTimer = Time.time;
                if (!isCurrentEnemy)
                {
                    bool clearPath = false;
                    if (Enemy.Seen == false && Enemy.Heard == false)
                    {
                        clearPath = true;
                    }
                    else if (Enemy.TimeSinceLastKnownUpdated > BotOwner.Settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC)
                    {
                        clearPath = true;
                    }
                    else if (Enemy.IsAI && Enemy.RealDistance > 300 || !Enemy.IsAI && Enemy.RealDistance > 500)
                    {
                        clearPath = true;
                    }
                    if (clearPath)
                    {
                        Clear();
                        return;
                    }
                }
                CalcPath(isCurrentEnemy);
            }
        }

        public NavMeshPathStatus PathToEnemyStatus { get; private set; }

        public void Clear()
        {
            if (PathToEnemy != null && PathToEnemy.corners.Length > 0)
            {
                PathToEnemy.ClearCorners();
            }
            if (PathToEnemyStatus != NavMeshPathStatus.PathInvalid)
            {
                PathToEnemyStatus = NavMeshPathStatus.PathInvalid;
            }
            if (LastCornerToEnemy != null)
            {
                LastCornerToEnemy = null;
            }
            if (CanSeeLastCornerToEnemy)
            {
                CanSeeLastCornerToEnemy = false;
            }
            if (_blindCorner != null)
            {
                _blindCorner = null;
            }
        }

        private void CalcPath(bool isCurrentEnemy)
        {
            // We should always have a not null LastKnownPosition here, but have the real position as a fallback just in case
            Vector3 enemyPosition;
            EnemyPlace lastKnown = Enemy.KnownPlaces.LastKnownPlace;
            if (lastKnown !=  null)
            {
                enemyPosition = lastKnown.Position;
            }
            else
            {
                enemyPosition = EnemyPosition;
            }

            Vector3 botPosition = SAIN.Position;

            // Did we already check the current enemy position and has the bot not moved? dont recalc path then
            if (_enemyLastPosChecked != null 
                && (_enemyLastPosChecked.Value - enemyPosition).sqrMagnitude < 0.1f
                && (_botLastPosChecked - botPosition).sqrMagnitude < 0.1f)
            {
                return;
            }

            // cache the positions we are currently checking
            _enemyLastPosChecked = enemyPosition;
            _botLastPosChecked = botPosition;

            // calculate a path to the enemys position
            ClearCorners();
            if (NavMesh.CalculatePath(botPosition, enemyPosition, -1, PathToEnemy))
            {
                PathDistance = PathToEnemy.CalculatePathLength();
            }

            PathToEnemyStatus = PathToEnemy.status;
            FindLastCornerToEnemy();
        }

        private void FindLastCornerToEnemy()
        {
            // find the last corner before arriving at an enemy position, and then check if we can see it.
            if (PathToEnemyStatus == NavMeshPathStatus.PathComplete && PathToEnemy.corners.Length > 2)
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
                CanSeeLastCornerToEnemy = false;
            }
        }

        private Vector3? _enemyLastPosChecked;
        private Vector3 _botLastPosChecked;

        private void ClearCorners()
        {
            if (PathToEnemy == null)
            {
                PathToEnemy = new NavMeshPath();
            }
            else
            {
                PathToEnemy.ClearCorners();
            }
        }

        public EnemyPathDistance CheckPathDistance()
        {
            const float VeryCloseDist = 5f;
            const float CloseDist = 40f;
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

        public float PathDistance { get; private set; } = float.MaxValue;
        public Vector3? LastCornerToEnemy { get; private set; }
        public bool CanSeeLastCornerToEnemy { get; private set; }

        public Vector3? BlindCornerToEnemy 
        { 
            get 
            { 
                if (_nextCheckBlindCornerTime < Time.time)
                {
                    _nextCheckBlindCornerTime = Time.time + 0.5f;

                    _blindCorner = Vector.FindFirstBlindCorner(BotOwner, PathToEnemy);

                    if (_blindCorner != null && SAINPlugin.DebugMode)
                    {
                        if (blindcornerGUIObject == null)
                        {
                            blindcornerGUIObject = DebugGizmos.CreateLabel(_blindCorner.Value, $"Blind Corner for {BotOwner.name} => {Enemy.EnemyPlayer?.name}");
                        }
                        else
                        {
                            blindcornerGUIObject.WorldPos = _blindCorner.Value;
                        }

                        if (blindcornerGammeObject != null)
                        {
                            blindcornerGammeObject.transform.position = _blindCorner.Value;
                        }
                        else
                        {
                            blindcornerGammeObject = DebugGizmos.Sphere(_blindCorner.Value, 0.1f, Color.red, false);
                        }
                    }

                    if (!SAINPlugin.DebugMode && blindcornerGUIObject != null)
                    {
                        DebugGizmos.DestroyLabel(blindcornerGUIObject);
                        GameObject.Destroy(blindcornerGammeObject);
                        blindcornerGUIObject = null;
                    }
                }
                return _blindCorner;
            }
        }

        private GUIObject blindcornerGUIObject;
        private GameObject blindcornerGammeObject;

        private Vector3? _blindCorner;
        private float _nextCheckBlindCornerTime;

        public NavMeshPath PathToEnemy { get; private set; }

        private float CheckPathTimer = 0f;
    }
}