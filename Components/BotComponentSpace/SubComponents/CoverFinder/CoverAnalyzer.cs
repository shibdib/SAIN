using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Enemy;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverAnalyzer : SAINBase
    {
        public CoverAnalyzer(BotComponent botOwner, CoverFinderComponent coverFinder) : base(botOwner)
        {
            CoverFinder = coverFinder;
        }

        private readonly CoverFinderComponent CoverFinder;

        public bool CheckCollider(Collider collider, out CoverPoint newPoint, out ECoverFailReason failReason)
        {
            NavMeshPath path = new NavMeshPath();
            if (CheckColliderForCover(collider, out Vector3 place, path, out failReason))
            {
                newPoint = new CoverPoint(Bot, place, collider, path);
                newPoint.PathLength = path.CalculatePathLength();
                return true;
            }
            newPoint = null;
            return false;
        }

        public bool CheckCollider(CoverPoint coverPoint, out ECoverFailReason failReason)
        {
            // the closest edge to that farPoint
            if (CheckColliderForCover(coverPoint.Collider, out Vector3 place, coverPoint.PathToPoint, out failReason))
            {
                coverPoint.PathLength = coverPoint.PathToPoint.CalculatePathLength();
                coverPoint.Position = place;
            }

            return false;
        }

        private bool CheckColliderForCover(Collider collider, out Vector3 place, NavMeshPath pathToPoint, out ECoverFailReason failReason)
        {
            if (!GetPlaceToMove(collider, TargetPoint, OriginPoint, out place))
            {
                failReason = ECoverFailReason.NoPlaceToMove;
                return false;
            }

            if (!CheckPosition(place))
            {
                failReason = ECoverFailReason.BadPosition;
                return false;
            }

            if (!CheckPath(place, pathToPoint))
            {
                failReason = ECoverFailReason.BadPath;
                return false;
            }

            failReason = ECoverFailReason.None;
            return true;
        }

        public static bool GetPlaceToMove(Collider collider, Vector3 targetPosition, Vector3 botPosition, out Vector3 place, float navSampleRange = 1f)
        {
            if (collider == null)
            {
                place = Vector3.zero;
                return false;
            }

            if (!CheckColliderDirection(collider, targetPosition, botPosition))
            {
                place = Vector3.zero;
                return false;
            }

            Vector3 colliderPos = collider.transform.position;

            // The direction from the target to the collider
            Vector3 colliderDir = (colliderPos - targetPosition).normalized;
            colliderDir.y = 0f;

            // a farPoint on opposite side of the target
            if (!NavMesh.SamplePosition(colliderPos + colliderDir, out var hit, navSampleRange, -1))
            {
                place = Vector3.zero;
                return false;
            }

            place = hit.position;

            // the closest edge to that farPoint
            if (NavMesh.FindClosestEdge(place, out var edge, -1)
                && NavMesh.SamplePosition(edge.position + colliderDir, out var hit2, navSampleRange, -1))
            {
                //Logger.LogDebug("Found Edge");
                place = hit2.position;
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
            return (position - TargetPoint).sqrMagnitude > CoverMinEnemyDistSqr &&
                !isPositionSpotted(position) &&
                checkPositionVsOtherBots(position) &&
                visibilityCheck(position, TargetPoint);
        }

        private bool isPositionSpotted(Vector3 position)
        {
            foreach (var point in CoverFinder.SpottedCoverPoints)
            {
                Vector3 coverPos = point.CoverPoint.Position;
                if (!point.IsValidAgain &&
                    point.TooClose(coverPos, position))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckPath(Vector3 position, NavMeshPath pathToPoint)
        {
            if (pathToPoint == null)
            {
                pathToPoint = new NavMeshPath();
            }
            else
            {
                pathToPoint.ClearCorners();
            }

            Vector3 origin = Vector3.zero;

            if (!NavMesh.CalculatePath(OriginPoint, position, -1, pathToPoint))
            {
                return false;
            }

            if (pathToPoint.status == NavMeshPathStatus.PathPartial &&
                (position - pathToPoint.corners[pathToPoint.corners.Length - 1]).sqrMagnitude > 1f)
            {
                return false;
            }

            if (!checkPathToEnemy(pathToPoint))
            {
                return false;
            }

            return true;
        }

        private bool checkPathToEnemy(NavMeshPath path)
        {
            SAINEnemy enemy = Bot.Enemy;
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

        private bool checkPositionVsOtherBots(Vector3 position)
        {
            if (Bot.Squad.Members == null || Bot.Squad.Members.Count < 2)
            {
                return true;
            }

            const float DistanceToBotCoverThresh = 1f;

            foreach (var member in Bot.Squad.Members.Values)
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

        private static bool visibilityCheck(Vector3 position, Vector3 target)
        {
            const float offset = 0.1f;

            if (checkRayCast(position, target, 3f))
            {
                Vector3 enemyDirection = target - position;
                enemyDirection = enemyDirection.normalized * offset;

                Quaternion right = Quaternion.Euler(0f, 90f, 0f);
                Vector3 rightPoint = right * enemyDirection;
                rightPoint += position;

                if (checkRayCast(rightPoint, target, 3f))
                {
                    Quaternion left = Quaternion.Euler(0f, -90f, 0f);
                    Vector3 leftPoint = left * enemyDirection;
                    leftPoint += position;

                    if (checkRayCast(leftPoint, target, 3f))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool checkRayCast(Vector3 point, Vector3 target, float distance = 3f)
        {
            point.y += 0.5f;
            target.y += 1.25f;
            Vector3 direction = target - point;
            return Physics.Raycast(point, direction, distance, LayerMaskClass.HighPolyWithTerrainMask);
        }

        private Vector3 OriginPoint => CoverFinder.OriginPoint;
        private Vector3 TargetPoint => CoverFinder.TargetPoint;
        private float CoverMinEnemyDistSqr => CoverFinderComponent.CoverMinEnemyDistSqr;
        private static bool DebugCoverFinder => CoverFinderComponent.DebugCoverFinder;
        private static float CoverMinHeight => CoverFinderComponent.CoverMinHeight;
    }
}