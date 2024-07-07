using SAIN.Helpers;
using SAIN.SAINComponent.Classes;
using System.Drawing.Drawing2D;
using UnityEngine;
using UnityEngine.AI;
using static SAIN.SAINComponent.SubComponents.CoverFinder.CoverAnalyzer;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverAnalyzer : BotBase
    {
        public CoverAnalyzer(BotComponent botOwner, CoverFinderComponent coverFinder) : base(botOwner)
        {
            CoverFinder = coverFinder;
        }

        private readonly CoverFinderComponent CoverFinder;

        public bool CheckCollider(Collider collider, TargetData targetData, out CoverPoint coverPoint, out string reason)
        {
            coverPoint = null;
            HardColliderData hardData = new HardColliderData(collider);
            ColliderData colliderData = new ColliderData(hardData, targetData);

            if (!GetPlaceToMove(colliderData, hardData, targetData, out Vector3 coverPosition))
            {
                reason = "noPlaceToMove";
                return false;
            }

            if (!CheckPosition(coverPosition, targetData, colliderData, hardData))
            {
                reason = "badPosition";
                return false;
            }

            PathData pathData = new PathData(new NavMeshPath());
            if (!CheckPath(coverPosition, pathData, targetData))
            {
                reason = "badPath";
                return false;
            }

            reason = string.Empty;
            coverPoint = new CoverPoint(Bot, hardData, pathData, coverPosition);
            return true;
        }

        public bool RecheckCoverPoint(CoverPoint coverPoint, TargetData targetData, out string reason)
        {
            var hardData = coverPoint.HardColliderData;
            ColliderData colliderData = new ColliderData(hardData, targetData);

            if (!GetPlaceToMove(colliderData, hardData, targetData, out Vector3 coverPosition))
            {
                reason = "noPlaceToMove";
                return false;
            }

            coverPoint.Position = coverPosition;

            if (coverPoint.StraightDistanceStatus == CoverStatus.InCover)
            {
                reason = "inCover";
                return true;
            }

            if (!CheckPosition(coverPosition, targetData, colliderData, hardData))
            {
                reason = "badPosition";
                return false;
            }

            if (!CheckPath(coverPosition, coverPoint.PathData, targetData))
            {
                reason = "badPath";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool GetPlaceToMove(ColliderData colliderData, HardColliderData hardData, TargetData targetDirections, out Vector3 place)
        {
            if (!checkColliderDirectionvsTargetDirection(colliderData, targetDirections))
            {
                place = Vector3.zero;
                return false;
            }
            if (!findSampledPosition(colliderData, hardData, POSITION_SAMPLE_RANGE, out place))
            {
                place = Vector3.zero;
                return false;
            }

            return true;
        }

        private const float POSITION_FINAL_MIN_DOT = 0.5f;
        private const float POSITION_EDGE_MIN_DOT = 0.5f;
        private const float POSTIION_EDGE_SAMPLE_RANGE = 0.5f;
        private const float POSITION_SAMPLE_RANGE = 1f;

        private bool checkFinalPositionDirection(ColliderData colliderDirs, HardColliderData hardData, TargetData targetDirs, Vector3 place)
        {
            Vector3 dirToPlace = place - hardData.Position;
            Vector3 dirToPlaceNormal = dirToPlace.normalized;
            Vector3 dirToColliderNormal = colliderDirs.dirTargetToColliderNormal;
            float dot = Vector3.Dot(dirToPlaceNormal, dirToColliderNormal);
            return dot > POSITION_FINAL_MIN_DOT;
        }

        private bool findSampledPosition(ColliderData colliderDirs, HardColliderData hardData, float navSampleRange, out Vector3 coverPosition)
        {
            Vector3 samplePos = hardData.Position + colliderDirs.dirTargetToColliderNormal;
            if (!NavMesh.SamplePosition(samplePos, out var hit, navSampleRange, -1))
            {
                coverPosition = Vector3.zero;
                return false;
            }
            coverPosition = findEdge(hit.position, colliderDirs);
            return true;
        }

        private Vector3 findEdge(Vector3 navMeshHit, ColliderData colliderDirs)
        {
            if (NavMesh.FindClosestEdge(navMeshHit, out var edge, -1))
            {
                Vector3 edgeNormal = edge.normal;
                Vector3 targetNormal = colliderDirs.dirTargetToColliderNormal;
                if (Vector3.Dot(edgeNormal, targetNormal) > POSITION_EDGE_MIN_DOT)
                {
                    Vector3 edgeCover = edge.position + colliderDirs.dirTargetToColliderNormal;
                    if (NavMesh.SamplePosition(edgeCover, out var edgeHit, POSTIION_EDGE_SAMPLE_RANGE, -1))
                    {
                        return edgeHit.position;
                    }
                }
            }
            return navMeshHit;
        }

        private bool checkColliderDirectionvsTargetDirection(ColliderData colliderDirs, TargetData targetDirs)
        {
            float dot = Vector3.Dot(targetDirs.DirBotToTargetNormal, colliderDirs.dirBotToColliderNormal);

            if (dot <= 0.33f)
            {
                return true;
            }
            float colliderDist = colliderDirs.ColliderDistanceToBot;
            float targetDist = targetDirs.TargetDistance;
            if (dot <= 0.5f)
            {
                return colliderDist < targetDist * 0.75f;
            }
            if (dot <= 0.66f)
            {
                return colliderDist < targetDist * 0.66f;
            }
            return colliderDist < targetDist * 0.5f;
        }

        private bool CheckPosition(Vector3 coverPosition, TargetData targetData, ColliderData colliderData, HardColliderData hardData)
        {
            return (coverPosition - targetData.TargetPosition).sqrMagnitude > CoverMinEnemyDistSqr &&
                !isPositionSpotted(coverPosition) &&
                checkPositionVsOtherBots(coverPosition) &&
                visibilityCheck(coverPosition, targetData, colliderData, hardData);
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

        private bool CheckPath(Vector3 position, PathData pathData, TargetData targetData)
        {
            var path = pathData.Path;
            path.ClearCorners();
            NavMesh.CalculatePath(OriginPoint, position, -1, path);

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            float pathLength = path.CalculatePathLength();
            if (pathLength > SAINPlugin.LoadedPreset.GlobalSettings.Cover.MaxCoverPathLength)
            {
                return false;
            }
            pathData.PathLength = pathLength;

            if (!checkPathToEnemy(path, targetData))
            {
                return false;
            }
            return true;
        }

        private const float PATH_SAME_DIST_MIN_RATIO = 0.66f;
        private const float PATH_SAME_CHECK_DIST = 0.1f;
        private const float PATH_NODE_MIN_DIST_SQR = 0.25f;
        private const float PATH_NODE_FIRST_DOT_MAX = 0.5f;

        private bool checkPathToEnemy(NavMeshPath path, TargetData targetData)
        {
            if (!SAINBotSpaceAwareness.ArePathsDifferent(path, targetData.TargetEnemy.Path.PathToEnemy, PATH_SAME_DIST_MIN_RATIO, PATH_SAME_CHECK_DIST)) {
                return false;
            }

            Vector3 botToTargetNormal = targetData.DirBotToTargetNormal;

            for (int i = 1; i < path.corners.Length - 1; i++)
            {
                var corner = path.corners[i];
                Vector3 cornerToTarget = TargetPoint - corner;
                Vector3 botToCorner = corner - OriginPoint;

                if (cornerToTarget.sqrMagnitude < PATH_NODE_MIN_DIST_SQR) {
                    if (DebugCoverFinder) {
                        //DrawDebugGizmos.Ray(OriginPoint, corner - OriginPoint, Color.red, (corner - OriginPoint).magnitude, 0.05f, true, 30f);
                    }
                    return false;
                }

                if (i == 1) {
                    if (Vector3.Dot(botToCorner.normalized, botToTargetNormal) > PATH_NODE_FIRST_DOT_MAX) {
                        if (DebugCoverFinder) {
                            //DrawDebugGizmos.Ray(corner, cornerToTarget, Color.red, cornerToTarget.magnitude, 0.05f, true, 30f);
                        }
                        return false;
                    }
                }
                else if (i < path.corners.Length - 2) {
                    Vector3 cornerB = path.corners[i + 1];
                    Vector3 directionToNextCorner = cornerB - corner;

                    if (Vector3.Dot(cornerToTarget.normalized, directionToNextCorner.normalized) > 0.5f) {
                        if (directionToNextCorner.sqrMagnitude > cornerToTarget.sqrMagnitude) {
                            if (DebugCoverFinder) {
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

            string profileID = Bot.ProfileId;
            foreach (var member in Bot.Squad.Members.Values)
            {
                if (member == null || member.ProfileId == profileID)
                    continue;

                if (isDistanceToClose(member.Cover.CoverInUse, position))
                    return false;
            }

            return true;
        }

        private bool isDistanceToClose(CoverPoint point, Vector3 position)
        {
            const float DistanceToBotCoverThresh = 1f;
            return point != null && (position - point.Position).sqrMagnitude < DistanceToBotCoverThresh;
        }

        private static bool visibilityCheck(Vector3 position, TargetData targetData, ColliderData colliderData, HardColliderData hardData)
        {
            const float offset = 0.1f;

            float distanceToCollider = (hardData.Position - position).magnitude * 1.25f;
            //Logger.LogDebug($"visCheck: Dist To Collider: {distanceToCollider}");

            Vector3 target = targetData.TargetPosition;
            if (!checkRaycastToCoverCollider(position, target, out RaycastHit hit, distanceToCollider))
            {
                return false;
            }

            Vector3 enemyDirection = targetData.DirBotToTargetNormal * offset;
            Quaternion right = Quaternion.Euler(0f, 90f, 0f);
            Vector3 rightPoint = right * enemyDirection;

            if (!checkRaycastToCoverCollider(position + rightPoint, target, out hit, distanceToCollider))
            {
                return false;
            }

            if (!checkRaycastToCoverCollider(position - rightPoint, target, out hit, distanceToCollider))
            {
                return false;
            }

            return true;
        }

        private static bool checkRaycastToCoverCollider(Vector3 point, Vector3 target, out RaycastHit hit, float distance)
        {
            point.y += 0.5f;
            target.y += 1.25f;
            Vector3 direction = target - point;
            bool hitObject = Physics.Raycast(point, direction, out hit, distance, LayerMaskClass.HighPolyWithTerrainMask);

            if (DebugCoverFinder)
            {
                if (hitObject)
                {
                    DebugGizmos.Line(point, hit.point, Color.white, 0.1f, true, 10f);
                }
                else
                {
                    Vector3 testPoint = direction.normalized * distance + point;
                    DebugGizmos.Line(point, testPoint, Color.red, 0.1f, true, 10f);
                }
            }
            return hitObject;
        }

        private Vector3 OriginPoint => CoverFinder.OriginPoint;
        private Vector3 TargetPoint => CoverFinder.TargetPoint;
        private float CoverMinEnemyDistSqr => CoverFinderComponent.CoverMinEnemyDistSqr;
        private static bool DebugCoverFinder => CoverFinderComponent.DebugCoverFinder;
        private static float CoverMinHeight => CoverFinderComponent.CoverMinHeight;
    }
}