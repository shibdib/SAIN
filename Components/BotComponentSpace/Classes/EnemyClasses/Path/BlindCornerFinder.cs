using SAIN.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class BlindCornerFinder : EnemyBase
    {
        public BlindCornerFinder(Enemy enemy) : base(enemy)
        {
        }

        public void ClearBlindCorner()
        {
            Enemy.Path.EnemyCorners.Remove(ECornerType.Blind);
        }

        public IEnumerator FindBlindCorner2(Vector3[] corners, Vector3 enemyPosition)
        {
            _corners.Clear();
            _corners.AddRange(corners);
            int count = _corners.Count;

            if (count <= 2) {
                ClearBlindCorner();
                yield break;
            }

            int blindCornerIndex = count - 1;
            Vector3? blindCorner = null;

            for (int i = count - 1; i > 0; i--) {

                Vector3 corner = _corners[i];
                blindCornerIndex = i - 1;
                Vector3 nextCorner = _corners[i - 1];

                _segments.Clear();
                findSegmentsBetweenCorner(corner, nextCorner, _segments);

                for (int j = 0; j < _segments.Count; j++) {

                    Vector3 segment = _segments[j];
                    if (CheckSightAtSegment(segment, Bot.Transform.EyePosition, out Vector3 sightPoint)) {
                        blindCorner = segment;
                        break;
                    }

                    yield return null;
                }
                if (blindCorner != null) {
                    break;
                }
            }
            
            if (blindCorner != null) {
                Vector3 blindCornerDir = (blindCorner.Value - Bot.Transform.EyePosition).normalized;
                blindCornerDir.y = 0;
                Vector3 enemyPosDir = (enemyPosition - Bot.Transform.EyePosition).normalized;
                enemyPosDir.y = 0;

                float signedAngle = Vector3.SignedAngle(blindCornerDir, enemyPosDir, Vector3.up);
                Enemy.Path.EnemyCorners.AddOrReplace(ECornerType.Blind, new EnemyCorner(blindCorner.Value, signedAngle, blindCornerIndex));
                yield break;
            }
            ClearBlindCorner();
        }

        private bool CheckSightAtSegment(Vector3 segment, Vector3 origin, out Vector3 sightPoint)
        {
            Vector3 first = segment + (Vector3.up * 0.1f);
            Vector3 firstDir = first - origin;
            DebugGizmos.Sphere(first, 0.1f, Color.blue, true, 10f);

            if (!Physics.Raycast(origin, firstDir, firstDir.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI)) {
                sightPoint = segment;
                DebugGizmos.Line(origin, sightPoint, Color.blue, 0.05f, true, 10f, false);
                return true;
            }

            Vector3 second = segment + HEIGHT_OFFSET_HALF;
            Vector3 secondDir = second - origin;
            if (!Physics.Raycast(origin, secondDir, secondDir.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI)) {
                sightPoint = second;
                DebugGizmos.Line(origin, sightPoint, Color.blue, 0.05f, true, 10f, false);
                return true;
            }

            Vector3 third = segment + HEIGHT_OFFSET;
            Vector3 thirdDir = third - origin;
            if (!Physics.Raycast(origin, thirdDir, thirdDir.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI)) {
                sightPoint = third;
                DebugGizmos.Line(origin, sightPoint, Color.blue, 0.025f, true, 10f, false);
                return true;
            }

            sightPoint = Vector3.zero;
            return false;
        }

        private const float HEIGHT = 1.6f;
        private const float HEIGHT_HALF = HEIGHT / 2f;
        private Vector3 HEIGHT_OFFSET = Vector3.up * HEIGHT;
        private Vector3 HEIGHT_OFFSET_HALF = Vector3.up * HEIGHT_HALF;

        private void findSegmentsBetweenCorner(Vector3 corner, Vector3 nextCorner, List<Vector3> segmentsList)
        {
            segmentsList.Add(corner);
            Vector3 cornerDirection = (nextCorner - corner);
            float sqrMag = cornerDirection.sqrMagnitude;
            if (sqrMag <= SEGMENT_LENGTH_SQR) {
                return;
            }
            if (sqrMag <= SEGMENT_LENGTH_SQR * 2f) {
                segmentsList.Add(Vector3.Lerp(corner, nextCorner, 0.5f));
                return;
            }
            float segmentLength = sqrMag / SEGMENT_LENGTH_SQR;
            Vector3 segmentDir = cornerDirection.normalized * segmentLength;
            int segmentCount = Mathf.RoundToInt(segmentLength);
            Vector3 segmentPoint = corner;
            for (int i = 0; i < segmentCount; i++) {
                segmentPoint += segmentDir;
                segmentsList.Add(segmentPoint);
            }
        }

        private readonly List<Vector3> _segments = new List<Vector3>();
        private const float SEGMENT_LENGTH = 0.5f;
        private const float SEGMENT_LENGTH_SQR = SEGMENT_LENGTH * SEGMENT_LENGTH;

        public IEnumerator FindBlindCorner(Vector3[] corners, Vector3 enemyPosition)
        {
            int count = corners.Length;
            if (count <= 1) {
                ClearBlindCorner();
                yield break;
            }

            Stopwatch sw = Stopwatch.StartNew();
            int totalRaycasts = 0;
            const int MAX_CASTS_PER_FRAME = 4;
            const int MAX_ITERATIONS_REAL_CORNER = 15;

            var transform = Bot.Transform;
            Vector3 lookPoint = transform.EyePosition;
            Vector3 lookOffset = lookPoint - Bot.Position;
            float heightOffset = lookOffset.y;

            Vector3 notVisibleCorner = enemyPosition;
            Vector3 lastVisibleCorner = corners[1];
            int index = 1;

            int raycasts = 0;

            // Note: currently this only finds the first corner they can't see past,
            // I should refactor and have it start from the last corner and descend until they CAN see a corner
            if (count > 2) {
                _corners.Clear();
                _corners.AddRange(corners);

                notVisibleCorner = _corners[2];
                lastVisibleCorner = _corners[1];

                for (int i = 1; i < count; i++) {
                    raycasts++;
                    Vector3 checkingCorner = _corners[i];
                    if (rayCastToCorner(checkingCorner, lookPoint, heightOffset)) {
                        index = i - 1;
                        lastVisibleCorner = _corners[i - 1];
                        notVisibleCorner = checkingCorner;
                        break;
                    }
                    if (raycasts >= MAX_CASTS_PER_FRAME) {
                        totalRaycasts += raycasts;
                        raycasts = 0;
                        yield return null;
                    }
                }
                _corners.Clear();
            }
            // end Note

            lastVisibleCorner.y += heightOffset;
            notVisibleCorner.y += heightOffset;

            Vector3 pointPastCorner = RaycastPastCorner(lastVisibleCorner, lookPoint, 0f, 10f);
            raycasts++;

            if (raycasts >= MAX_CASTS_PER_FRAME) {
                totalRaycasts += raycasts;
                raycasts = 0;
                yield return null;
            }

            float sign = Vector.FindFlatSignedAngle(pointPastCorner, notVisibleCorner, lookPoint);
            float angle = sign <= 0 ? -15f : 15f;
            float rotationStep = angle / MAX_ITERATIONS_REAL_CORNER;

            Vector3 blindCorner = lastVisibleCorner;
            Vector3 directionToBlind = lastVisibleCorner - lookPoint;
            float rayMaxDist = (pointPastCorner - lookPoint).magnitude;

            for (int i = 0; i < MAX_ITERATIONS_REAL_CORNER; i++) {
                raycasts++;

                directionToBlind = Vector.Rotate(directionToBlind, 0, rotationStep, 0);

                bool hit = Physics.Raycast(lookPoint, directionToBlind, rayMaxDist, LayerMaskClass.HighPolyWithTerrainMask);
                drawDebug(lookPoint + directionToBlind, lookPoint, hit);

                if (hit) {
                    //Logger.LogDebug($"Angle where LOS broken [{rotationStep * i}] after [{i}] iterations");
                    break;
                }

                blindCorner = lookPoint + directionToBlind;

                if (raycasts >= MAX_CASTS_PER_FRAME) {
                    totalRaycasts += raycasts;
                    raycasts = 0;
                    yield return null;
                }
            }

            blindCorner.y -= heightOffset;
            Enemy.Path.EnemyCorners.AddOrReplace(ECornerType.Blind, new EnemyCorner(blindCorner, angle, index));

            if (raycasts > 0) {
                totalRaycasts += raycasts;
                raycasts = 0;
                yield return null;
            }
            sw.Stop();
            if (_nextLogTime < Time.time) {
                _nextLogTime = Time.time + 5f;
                //float time = (sw.ElapsedMilliseconds / 1000f).Round100();
                //Logger.LogDebug($"Total Raycasts: [{totalRaycasts}] Time To Complete: [{time}] seconds");
            }
        }

        //private Vector3 findDispersionPositionAtNavMesh(Vector3 enemyPosition, float dispersion)
        //{
        //    const int iterations = 50;
        //    const float navSampleRange = 0.5f;
        //    for (int i = 0; i < iterations; i++)
        //    {
        //        Vector3 random = UnityEngine.Random.onUnitSphere * dispersion;
        //        random.y = 0;
        //        Vector3 point = enemyPosition + random;
        //        if (NavMesh.SamplePosition(point, out var hit, navSampleRange, -1))
        //        {
        //            return hit.position;
        //        }
        //    }
        //}

        //private void findBlockedPoint(Vector3[] corners, float segmentLength, Vector3 lastKnownPosition)
        //{
        //    const float reduceOffset = 0.66f;
        //
        //    Vector3? result = null;
        //    var transform = Bot.Transform;
        //    float lengthSqr = segmentLength * segmentLength;
        //    int count = corners.Length;
        //    for (int i = count - 1; i > 0; i--)
        //    {
        //        Vector3 start = corners[i];
        //        Vector3 lookSensor = transform.EyePosition;
        //        Vector3 botPosition = transform.Position;
        //        Vector3 offset = (lookSensor - botPosition);
        //        const int PointsToCheck = 5;
        //        Vector3 pointDir = offset / PointsToCheck;
        //
        //        Vector3 rayCastTarget = start;
        //        rayCastTarget.y += offset.y;
        //        result.Add(start);
        //
        //        Vector3 end = corners[i - 1];
        //        Vector3 direction = end - start;
        //        float sqrMagnitude = direction.sqrMagnitude;
        //
        //        if (sqrMagnitude <= lengthSqr)
        //            continue;
        //
        //        if (sqrMagnitude <= lengthSqr / 2f)
        //        {
        //            Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);
        //            result.Add(midPoint);
        //            continue;
        //        }
        //
        //        Vector3 directionNormal = direction.normalized;
        //        Vector3 segment = directionNormal * segmentLength;
        //        float currentLength = 0f;
        //        while (currentLength < lengthSqr)
        //        {
        //
        //        }
        //
        //
        //    }
        //}

        private float _nextLogTime;

        private void drawDebug(Vector3 corner, Vector3 lookPoint, bool hit)
        {
            if (SAINPlugin.DebugMode && SAINPlugin.DebugSettings.Gizmos.DebugDrawBlindCorner) {
                Color color = hit ? Color.red : Color.green;
                float lineWidth = 0.01f;
                float expireTime = 30f;

                //float lowerHeight = (Bot.Position - Bot.Transform.EyePosition).y * 0.8f;
                //corner.y += lowerHeight;
                //lookPoint.y += lowerHeight;

                DebugGizmos.Line(corner, lookPoint, color, lineWidth, true, expireTime, true);
            }
        }

        public static Vector3 RaycastPastCorner(Vector3 corner, Vector3 lookPoint, float addHeight, float addDistance = 2f)
        {
            corner.y += addHeight;
            Vector3 cornerDir = corner - lookPoint;

            Vector3 farPoint;
            if (Physics.Raycast(lookPoint, cornerDir, out var hit, addDistance, _mask)) {
                farPoint = hit.point;
            }
            else {
                farPoint = corner + cornerDir.normalized * addDistance;
            }
            Vector3 midPoint = Vector3.Lerp(farPoint, corner, 0.5f);
            return midPoint;
        }

        private bool rayCastToCorner(Vector3 corner, Vector3 lookPoint, float heightOffset)
        {
            corner.y += heightOffset;
            Vector3 direction = corner - lookPoint;
            return Physics.Raycast(lookPoint, direction, direction.magnitude, _mask);
        }

        private static readonly LayerMask _mask = LayerMaskClass.HighPolyWithTerrainMask;
        private readonly List<Vector3> _corners = new List<Vector3>();
    }
}