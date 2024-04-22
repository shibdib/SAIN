using BepInEx.Logging;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Linq;
using System;
using System.Diagnostics;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverFinderComponent : MonoBehaviour, ISAINSubComponent
    {
        public void Init(SAINComponentClass sain)
        {
            SAIN = sain;
            Player = sain.Player;
            BotOwner = sain.BotOwner;

            ColliderFinder = new ColliderFinder(this);
            CoverAnalyzer = new CoverAnalyzer(SAIN, this);
        }

        public SAINComponentClass SAIN { get; private set; }
        public Player Player { get; private set; }
        public BotOwner BotOwner { get; private set; }

        public List<CoverPoint> CoverPoints { get; private set; } = new List<CoverPoint>();
        public CoverAnalyzer CoverAnalyzer { get; private set; }
        public ColliderFinder ColliderFinder { get; private set; }

        private Collider[] Colliders = new Collider[150];

        private void Update()
        {
            if (SAIN == null || BotOwner == null || Player == null)
            {
                Dispose();
                return;
            }
            if (DebugCoverFinder)
            {
                if (CoverPoints.Count > 0)
                {
                    DebugGizmos.Line(CoverPoints.PickRandom().GetPosition(SAIN), SAIN.Transform.Head, Color.yellow, 0.035f, true, 0.1f);
                }
            }
        }

        static bool DebugCoverFinder => SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder;

        public void LookForCover(Vector3 targetPosition, Vector3 originPoint)
        {
            //AnalyzeAllColliders();

            TargetPoint = targetPosition;
            OriginPoint = originPoint;

            if (TakeCoverCoroutine == null)
            {
                TakeCoverCoroutine = StartCoroutine(FindCover());
            }
        }

        public void StopLooking()
        {
            if (TakeCoverCoroutine != null)
            {
                StopCoroutine(TakeCoverCoroutine);
                TakeCoverCoroutine = null;
            }
        }

        private static void AnalyzeAllColliders()
        {
            if (!AllCollidersAnalyzed)
            {
                AllCollidersAnalyzed = true;
                float minHeight = CoverFinderComponent.CoverMinHeight;
                const float minX = 0.1f;
                const float minZ = 0.1f;

                Collider[] allColliders = new Collider[500000];
                int hits = Physics.OverlapSphereNonAlloc(Vector3.zero, 1000f, allColliders);

                int hitReduction = 0;
                for (int i = 0; i < hits; i++)
                {
                    Vector3 size = allColliders[i].bounds.size;
                    if (size.y < CoverFinderComponent.CoverMinHeight
                        || size.x < minX && size.z < minZ)
                    {
                        allColliders[i] = null;
                        hitReduction++;
                    }
                }
                Logger.LogError($"All Colliders Analyzed. [{hits - hitReduction}] are suitable out of [{hits}] colliders");
            }
        }

        private static bool AllCollidersAnalyzed;

        public static float CoverMinHeight => SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverMinHeight;
        public static float CoverMinEnemyDist => SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverMinEnemyDistance;

        public CoverPoint FallBackPoint { get; private set; }

        private Vector3 LastPositionChecked = Vector3.zero;

        private IEnumerator FindCover()
        {
            while (true)
            {
                Stopwatch fullStopWatch = Stopwatch.StartNew();

                if (_nextClearSpottedTime < Time.time)
                {
                    _nextClearSpottedTime = Time.time + 1f;

                    for (int i = SpottedCoverPoints.Count - 1; i >= 0; i--)
                    {
                        var spottedPoint = SpottedCoverPoints[i];
                        if (spottedPoint.IsValidAgain)
                        {
                            SpottedCoverPoints.RemoveAt(i);
                        }
                    }
                }

                for (int i = CoverPoints.Count - 1 ; i >= 0; i--)
                {
                    var coverPoint = CoverPoints[i];
                    if (coverPoint == null 
                        || !RecheckCoverPoint(coverPoint))
                    {
                        CoverPoints.RemoveAt(i);
                    }
                    yield return null;
                }

                Stopwatch findFirstPointStopWatch = null;
                if (CoverPoints.Count == 0 && SAINPlugin.DebugMode)
                {
                    findFirstPointStopWatch = Stopwatch.StartNew();
                }

                int totalChecked = 0;
                int waitCount = 0;
                int coverCount = CoverPoints.Count;

                const float DistanceThreshold = 5;
                if (coverCount < 5 
                    || (LastPositionChecked - OriginPoint).sqrMagnitude > DistanceThreshold * DistanceThreshold)
                {
                    LastPositionChecked = OriginPoint;

                    Collider[] colliders = GetColliders(out int hits);
                    for (int i = 0; i < hits; i++)
                    {
                        totalChecked++;
                        waitCount++;
                        if (CoverAnalyzer.CheckCollider(colliders[i], out var newPoint))
                        {
                            CoverPoints.Add(newPoint);
                            coverCount++;
                        }
                        if (coverCount >= 10)
                        {
                            break;
                        }
                        else if (coverCount > 2)
                        {
                            yield return null;
                        }
                        else if (coverCount > 0)
                        {
                            if (findFirstPointStopWatch?.IsRunning == true)
                            {
                                findFirstPointStopWatch.Stop();
                                if (_debugTimer < Time.time)
                                {
                                    _debugTimer = Time.time + 5;
                                    Logger.LogDebug($"Time to Find First CoverPoint: [{findFirstPointStopWatch.ElapsedMilliseconds}ms]");
                                    Logger.NotifyDebug($"Time to Find First CoverPoint: [{findFirstPointStopWatch.ElapsedMilliseconds}ms]");
                                }
                            }
                            if (waitCount >= 3)
                            {
                                waitCount = 0;
                                yield return null;
                            }
                        }
                        else if (waitCount >= 10)
                        {
                            waitCount = 0;
                            yield return null;
                        }
                    }

                    if (coverCount > 1)
                    {
                        OrderPointsByPathDist(CoverPoints, SAIN);
                    }

                    if (coverCount > 0)
                    {
                        FallBackPoint = FindFallbackPoint(CoverPoints);
                        if (DebugLogTimer < Time.time && DebugCoverFinder)
                        {
                            DebugLogTimer = Time.time + 1f;
                            Logger.LogInfo($"[{BotOwner.name}] - Found [{coverCount}] CoverPoints. Colliders checked: [{totalChecked}] Collider Array Size = [{hits}]");
                        }
                    }
                    else
                    {
                        FallBackPoint = null;
                        if (DebugLogTimer < Time.time && DebugCoverFinder)
                        {
                            DebugLogTimer = Time.time + 1f;
                            Logger.LogWarning($"[{BotOwner.name}] - No Cover Found! Valid Colliders checked: [{totalChecked}] Collider Array Size = [{hits}]");
                        }
                    }
                }
                else
                {
                    OrderPointsByPathDist(CoverPoints, SAIN);
                }

                if (findFirstPointStopWatch?.IsRunning == true)
                {
                    findFirstPointStopWatch.Stop();
                }

                fullStopWatch.Stop();
                if (_debugTimer2 < Time.time && SAINPlugin.DebugMode)
                {
                    _debugTimer2 = Time.time + 5;
                    Logger.LogDebug($"Time to Complete Coverfinder Loop: [{fullStopWatch.ElapsedMilliseconds}ms]");
                    Logger.NotifyDebug($"Time to Complete Coverfinder Loop: [{fullStopWatch.ElapsedMilliseconds}ms]");
                }

                yield return new WaitForSeconds(CoverUpdateFrequency);
            }
        }

        private static float _debugTimer;
        private static float _debugTimer2;

        public static void OrderPointsByPathDist(List<CoverPoint> points, SAINComponentClass sain)
        {
            points.Sort((x, y) => x.GetPathLength(sain).CompareTo(y.GetPathLength(sain)));
        }

        static float CoverUpdateFrequency => SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverUpdateFrequency;

        private CoverPoint FindFallbackPoint(List<CoverPoint> points)
        {
            CoverPoint result = null;
            CoverPoint safestResult = null;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];

                if (result == null
                    || point.Collider.bounds.size.y > result.Collider.bounds.size.y)
                {
                    if (point.IsSafePath(SAIN))
                    {
                        safestResult = point;
                    }
                    result = point;
                }
            }
            return safestResult ?? result;
        }

        static float FallBackPointResetDistance = 35;
        static float FallBackPointNextAllowedResetDelayTime = 3f;
        static float FallBackPointNextAllowedResetTime = 0;
        
        private bool CheckResetFallback()
        {
            if (FallBackPoint == null || Time.time < FallBackPointNextAllowedResetTime)
            {
                return false;
            }

            if ((BotOwner.Position - FallBackPoint.GetPosition(SAIN)).sqrMagnitude > FallBackPointResetDistance * FallBackPointResetDistance)
            {
                if (SAINPlugin.DebugMode)
                    Logger.LogInfo($"Resetting fallback point for {BotOwner.name}...");

                FallBackPointNextAllowedResetTime = Time.time + FallBackPointNextAllowedResetDelayTime;
                return true;
            }
            return false;
        }

        private float DebugLogTimer = 0f;

        public List<SpottedCoverPoint> SpottedCoverPoints { get; private set; } = new List<SpottedCoverPoint>();

        public bool RecheckCoverPoint(CoverPoint coverPoint)
        {
            return coverPoint != null && !PointIsSpotted(coverPoint) && CoverAnalyzer.CheckCollider(coverPoint);
        }

        private void ClearSpotted()
        {
            if (_nextClearSpottedTime < Time.time)
            {
                _nextClearSpottedTime = Time.time + 1f;

                for (int i = SpottedCoverPoints.Count - 1; i >= 0; i--)
                {
                    var spottedPoint = SpottedCoverPoints[i];
                    if (spottedPoint.IsValidAgain)
                    {
                        SpottedCoverPoints.RemoveAt(i);
                    }
                }
            }
        }

        private float _nextClearSpottedTime;

        private bool PointIsSpotted(CoverPoint point)
        {
            if (point == null)
            {
                return true;
            }

            ClearSpotted();

            foreach (var spottedPoint in SpottedCoverPoints)
            {
                Vector3 spottedPointPos = spottedPoint.CoverPoint.GetPosition(SAIN);
                if (spottedPoint.TooClose(spottedPointPos, point.GetPosition(SAIN)))
                {
                    return true;
                }
            }
            bool spotted = point.GetSpotted(SAIN);
            if (spotted)
            {
                SpottedCoverPoints.Add(new SpottedCoverPoint(point));
            }
            return spotted;
        }

        private Collider[] GetColliders(out int hits)
        {
            const float CheckDistThresh = 3f * 3f;
            const float ColliderSortDistThresh = 1f * 2f;

            float distance = (LastCheckPos - OriginPoint).sqrMagnitude;
            if (distance > CheckDistThresh)
            {
                LastCheckPos = OriginPoint;
                ColliderFinder.GetNewColliders(out hits, Colliders);
                LastHitCount = hits;
            }
            if (distance > ColliderSortDistThresh)
            {
                ColliderFinder.SortArrayBotDist(Colliders);
            }

            hits = LastHitCount;

            return Colliders;
        }

        private Vector3 LastCheckPos = Vector3.zero + Vector3.down * 100f;
        private int LastHitCount = 0;

        public void Dispose()
        {
            try {
                StopAllCoroutines();
                Destroy(this); }
            catch { }
        }

        private Coroutine TakeCoverCoroutine;

        public Vector3 OriginPoint { get; private set; }
        public Vector3 TargetPoint { get; private set; }
    }
}