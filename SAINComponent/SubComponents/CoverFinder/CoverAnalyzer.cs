using BepInEx.Logging;
using EFT;
using EFT.Interactive;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Enemy;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public enum ECoverFailReason
    {
        None = 0,
        ColliderUsed = 1,
        ExcludedName = 2,
        NoPlaceToMove = 3,
        BadPosition = 4,
        BadPath = 5,
        NullOrBad = 6,
        Spotted = 7,
    }

    public class CoverAnalyzer : SAINBase, ISAINClass
    {
        public CoverAnalyzer(Bot botOwner, CoverFinderComponent coverFinder) : base(botOwner)
        {
            CoverFinder = coverFinder;
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        private readonly CoverFinderComponent CoverFinder; 

        public bool CheckCollider(Collider collider, out CoverPoint newPoint, out ECoverFailReason failReason)
        {
            NavMeshPath path = new NavMeshPath();
            if (CheckColliderForCover(collider, out Vector3 place, false, out _, path, out failReason))
            {
                newPoint = new CoverPoint(SAINBot, place, collider, path);
                return true;
            }
            newPoint = null;
            return false;
        }

        public bool CheckCollider(CoverPoint coverPoint, out ECoverFailReason failReason)
        {
            // the closest edge to that farPoint
            NavMeshPath path = new NavMeshPath();
            if (CheckColliderForCover(coverPoint.Collider, out Vector3 place, false, out _, coverPoint.PathToPoint, out failReason))
            {
                coverPoint.Position = place;
            }

            return false;
        }

        private bool CheckColliderForCover(Collider collider, out Vector3 place, bool checkSafety, out bool isSafe, NavMeshPath pathToPoint, out ECoverFailReason failReason)
        {
            isSafe = false;
            if (GetPlaceToMove(collider, TargetPoint, OriginPoint, out place))
            {
                if (CheckPosition(place) && CheckHumanPlayerVisibility(place))
                {
                    if (CheckPath(place, checkSafety, out isSafe, pathToPoint))
                    {
                        failReason = ECoverFailReason.None;
                        return true;
                    }
                    else
                    {
                        failReason = ECoverFailReason.BadPath;
                    }
                }
                else
                {
                    failReason = ECoverFailReason.BadPosition;
                }
            }
            else
            {
                failReason = ECoverFailReason.NoPlaceToMove;
            }
            return false;
        }

        public static bool GetPlaceToMove(Collider collider, Vector3 targetPosition, Vector3 botPosition, out Vector3 place, float navSampleRange = 1f)
        {
            const float ExtendLengthThresh = 2f;

            place = Vector3.zero;
            if (collider == null 
                || collider.bounds.size.y < CoverMinHeight 
                || !CheckColliderDirection(collider, targetPosition, botPosition))
            {
                return false;
            }

            Vector3 colliderPos = collider.transform.position;

            // The direction from the target to the collider
            Vector3 colliderDir = (colliderPos - targetPosition).normalized;
            colliderDir.y = 0f;

            if (collider.bounds.size.z > ExtendLengthThresh && collider.bounds.size.x > ExtendLengthThresh)
            {
                //float min = Mathf.Min(collider.bounds.size.z, collider.bounds.size.x);
                //float multiplier = Mathf.Clamp(1f + (min - ExtendLengthThresh), 1f, 3f);
                //colliderDir *= multiplier;
            }

            // a farPoint on opposite side of the target
            Vector3 farPoint = colliderPos + colliderDir;

            // the closest edge to that farPoint
            if (NavMesh.SamplePosition(farPoint, out var hit, navSampleRange, -1))
            {
                if (NavMesh.FindClosestEdge(hit.position, out var edge, -1) 
                    && NavMesh.SamplePosition(edge.position + colliderDir, out var hit2, navSampleRange, -1))
                {
                    //Logger.LogDebug("Found Edge");
                    place = hit2.position;
                }
                else
                {
                    place = hit.position;
                }
                return true;
            }
            return false;
        }

        private bool CheckHumanPlayerVisibility(Vector3 point)
        {
            // this function is all fucked up
            return true;
            //Player closestPlayer = GameWorldHandler.SAINGameWorld?.FindClosestPlayer(out float sqrDist, point);
            if (SAINBot.EnemyController.IsHumanPlayerActiveEnemy == false 
                && SAINBot.EnemyController.IsHumanPlayerLookAtMe(out Player lookingPlayer)
                && lookingPlayer != null)
            {
                SAINEnemy enemy = SAINBot.EnemyController.GetEnemy(lookingPlayer.ProfileId);
                var lastKnown = enemy?.LastKnownPosition;
                if (lastKnown != null)
                {
                    bool VisibleCheckPass = (VisibilityCheck(point, lastKnown.Value));
                    if (SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder)
                    {
                        if (VisibleCheckPass)
                        {
                            // Main Player does not have vision on coverpoint position
                            Logger.LogWarning("PASS");
                        }
                        else
                        {
                            // Main Player has vision
                            Logger.LogWarning("FAIL");
                        }
                    }

                    return VisibleCheckPass;
                }
            }
            return true;
        }

        private static bool CheckColliderDirection(Collider collider, Vector3 targetPosition, Vector3 botPosition)
        {
            Vector3 pos = collider.transform.position;

            Vector3 directionToTarget = targetPosition - botPosition;
            float targetDist = directionToTarget.magnitude;

            Vector3 directionToCollider = pos - botPosition;
            float colliderDist = directionToCollider.magnitude;

            float dot = Vector3.Dot(directionToTarget.normalized, directionToCollider.normalized);

            if (dot <= 0.33f)
            {
                return true;
            }
            if (dot <= 0.6f)
            {
                return colliderDist < targetDist * 0.75f;
            }
            if (dot <= 0.8f)
            {
                return colliderDist < targetDist * 0.5f;
            }
            return colliderDist < targetDist * 0.25f;
        }

        private bool CheckPosition(Vector3 position)
        {
            return (position - TargetPoint).sqrMagnitude > CoverMinEnemyDist * CoverMinEnemyDist
                && !isPositionSpotted(position)
                && CheckPositionVsOtherBots(position)
                && VisibilityCheck(position, TargetPoint);
        }

        private bool isPositionSpotted(Vector3 position)
        {
            foreach (var point in CoverFinder.SpottedCoverPoints)
            {
                Vector3 coverPos = point.CoverPoint.Position;
                if (!point.IsValidAgain && point.TooClose(coverPos, position))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckPath(Vector3 position, bool checkSafety, out bool isSafe, NavMeshPath pathToPoint)
        {
            if (pathToPoint == null)
            {
                pathToPoint = new NavMeshPath();
            }
            else
            {
                pathToPoint.ClearCorners();
            }
            if (NavMesh.CalculatePath(OriginPoint, position, -1, pathToPoint) && pathToPoint.status == NavMeshPathStatus.PathComplete)
            {
                if (checkPathToEnemy(pathToPoint))
                {
                    isSafe = checkSafety ? CheckPathSafety(pathToPoint) : false;
                    return true;
                }
            }

            isSafe = false;
            return false;
        }

        private bool CheckPathSafety(NavMeshPath path)
        {
            Vector3 target = TargetPoint + Vector3.up;
            return SAINBotSpaceAwareness.CheckPathSafety(path, target);
        }

        static bool DebugCoverFinder => SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder;

        private bool checkPathToEnemy(NavMeshPath path)
        {
            SAINEnemy enemy = SAINBot.Enemy;
            if (enemy != null 
                && enemy.Path.PathToEnemy != null 
                && !SAINBotSpaceAwareness.ArePathsDifferent(path, enemy.Path.PathToEnemy, 0.5f, 0.1f))
            {
                return false;
            }
            for (int i = 1; i < path.corners.Length - 1; i++)
            {
                var corner = path.corners[i];
                Vector3 cornerToTarget = TargetPoint - corner;
                Vector3 botToTarget = TargetPoint - OriginPoint;
                Vector3 botToCorner = corner - OriginPoint;

                if (cornerToTarget.magnitude < 0.5f)
                {
                    if (DebugCoverFinder)
                    {
                        //DrawDebugGizmos.Ray(OriginPoint, corner - OriginPoint, Color.red, (corner - OriginPoint).magnitude, 0.05f, true, 30f);
                    }

                    return false;
                }

                if (i == 1)
                {
                    if (Vector3.Dot(botToCorner.normalized, botToTarget.normalized) > 0.5f)
                    {
                        if (DebugCoverFinder)
                        {
                            //DrawDebugGizmos.Ray(corner, cornerToTarget, Color.red, cornerToTarget.magnitude, 0.05f, true, 30f);
                        }
                        return false;
                    }
                }
                else if (i < path.corners.Length - 2)
                {
                    Vector3 cornerB = path.corners[i + 1];
                    Vector3 directionToNextCorner = cornerB - corner;

                    if (Vector3.Dot(cornerToTarget.normalized, directionToNextCorner.normalized) > 0.5f)
                    {
                        if (directionToNextCorner.magnitude > cornerToTarget.magnitude)
                        {
                            if (DebugCoverFinder)
                            {
                                //DrawDebugGizmos.Ray(corner, cornerToTarget, Color.red, cornerToTarget.magnitude, 0.05f, true, 30f);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool CheckPositionVsOtherBots(Vector3 position)
        {
            if (SAINBot.Squad.Members == null || SAINBot.Squad.Members.Count < 2)
            {
                return true;
            }

            const float DistanceToBotCoverThresh = 1f;

            foreach (var member in SAINBot.Squad.Members.Values)
            {
                if (member != null && member.BotOwner != BotOwner)
                {
                    CoverPoint currentCover = member.Cover.CoverInUse;
                    if (currentCover != null)
                    {
                        Vector3 coverPos = currentCover.Position;
                        if ((position - coverPos).sqrMagnitude < DistanceToBotCoverThresh * DistanceToBotCoverThresh)
                        {
                            return false;
                        }
                    }
                    else if (member.Cover.FallBackPoint != null)
                    {
                        Vector3 coverPos = member.Cover.FallBackPoint.Position;
                        if ((position - coverPos).sqrMagnitude < DistanceToBotCoverThresh * DistanceToBotCoverThresh)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool VisibilityCheck(Vector3 position, Vector3 target)
        {
            const float offset = 0.1f;

            if (CheckRayCast(position, target, 3f))
            {
                Vector3 enemyDirection = target - position;
                enemyDirection = enemyDirection.normalized * offset;

                Quaternion right = Quaternion.Euler(0f, 90f, 0f);
                Vector3 rightPoint = right * enemyDirection;
                rightPoint += position;

                if (CheckRayCast(rightPoint, target, 3f))
                {
                    Quaternion left = Quaternion.Euler(0f, -90f, 0f);
                    Vector3 leftPoint = left * enemyDirection;
                    leftPoint += position;

                    if (CheckRayCast(leftPoint, target, 3f))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
         
        private static bool CheckRayCast(Vector3 point, Vector3 target, float distance = 3f)
        {
            point.y += 0.5f;
            target.y += 1.25f;
            Vector3 direction = target - point;
            return Physics.Raycast(point, direction, distance, LayerMaskClass.HighPolyWithTerrainMask);
        }

        private static float CoverMinHeight => CoverFinderComponent.CoverMinHeight;
        private Vector3 OriginPoint => CoverFinder.OriginPoint;
        private Vector3 TargetPoint => CoverFinder.TargetPoint;
        private float CoverMinEnemyDist => CoverFinderComponent.CoverMinEnemyDist;
    }
}