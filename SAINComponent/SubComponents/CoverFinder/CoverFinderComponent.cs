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

        private Collider[] Colliders = new Collider[200];

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
                    DebugGizmos.Line(CoverPoints.PickRandom().Position, SAIN.Transform.Head, Color.yellow, 0.035f, true, 0.1f);
                }
            }
        }

        static bool DebugCoverFinder => SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder;

        public void LookForCover(Vector3 targetPosition, Vector3 originPoint)
        {
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

        public static float CoverMinHeight => SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverMinHeight;
        public static float CoverMinEnemyDist => SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverMinEnemyDistance;

        public CoverPoint FallBackPoint { get; private set; }

        private Vector3 LastPositionChecked = Vector3.zero;

        private IEnumerator FindCover()
        {
            while (true)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                //UpdateSpotted();

                for (int i = CoverPoints.Count - 1 ; i >= 0; i--)
                {
                    var coverPoint = CoverPoints[i];
                    if (coverPoint == null || !RecheckCoverPoint(coverPoint) || coverPoint.Spotted)
                    {
                        CoverPoints.RemoveAt(i);
                    }
                    yield return null;
                }

                int totalChecked = 0;
                int waitCount = 0;
                int coverCount = CoverPoints.Count;

                const float DistanceThreshold = 5;
                if (coverCount < 3 
                    || (LastPositionChecked - OriginPoint).sqrMagnitude > DistanceThreshold * DistanceThreshold)
                {
                    LastPositionChecked = OriginPoint;

                    Collider[] colliders = GetColliders(out int hits);
                    for (int i = 0; i < hits; i++)
                    {
                        if (coverCount > 4)
                        {
                            break;
                        }
                        else if (coverCount == 4)
                        {
                            yield return new WaitForSeconds(0.1f);
                        }
                        else if (coverCount == 3)
                        {
                            yield return new WaitForSeconds(0.05f);
                        }
                        else if (coverCount == 2)
                        {
                            yield return new WaitForSeconds(0.025f);
                        }
                        else if (coverCount == 1)
                        {
                            yield return null;
                        }

                        if (waitCount >= 5)
                        {
                            waitCount = 0;
                            yield return null;
                        }

                        totalChecked++;
                        waitCount++;
                        if (CoverAnalyzer.CheckCollider(colliders[i], out var newPoint))
                        {
                            CoverPoints.Add(newPoint);
                            coverCount++;
                        }
                    }

                    if (coverCount > 1)
                    {
                        OrderPointsByPathDist(CoverPoints);
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
                    OrderPointsByPathDist(CoverPoints);
                }

                stopwatch.Stop();
                if (SAINPlugin.DebugMode)
                {
                    Logger.LogDebug($"Time to Complete Cover Finder Loop: [{stopwatch.ElapsedMilliseconds}ms]");
                }
                yield return new WaitForSeconds(CoverUpdateFrequency);
            }
        }

        public static void OrderPointsByPathDist(List<CoverPoint> points)
        {
            points.Sort((x, y) => x.PathLength.CompareTo(y.PathLength));
        }

        static float CoverUpdateFrequency => SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverUpdateFrequency;

        private static CoverPoint FindFallbackPoint(List<CoverPoint> points)
        {
            CoverPoint result = null;
            CoverPoint safestResult = null;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];

                if (result == null
                    || point.Collider.bounds.size.y > result.Collider.bounds.size.y)
                {
                    if (point.IsSafePath)
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

            if ((BotOwner.Position - FallBackPoint.Position).sqrMagnitude > FallBackPointResetDistance * FallBackPointResetDistance)
            {
                if (SAINPlugin.DebugMode)
                    Logger.LogInfo($"Resetting fallback point for {BotOwner.name}...");

                FallBackPointNextAllowedResetTime = Time.time + FallBackPointNextAllowedResetDelayTime;
                return true;
            }
            return false;
        }

        private float DebugLogTimer = 0f;

        public List<SpottedCoverPoint> SpottedPoints { get; private set; } = new List<SpottedCoverPoint>();

        public bool RecheckCoverPoint(CoverPoint coverPoint)
        {
            return coverPoint != null && !PointIsSpotted(coverPoint) && CoverAnalyzer.CheckCollider(coverPoint);
        }

        private bool PointIsSpotted(CoverPoint point)
        {
            if (point == null)
            {
                return true;
            }
            if (point.Spotted)
            {
                SpottedPoints.Add(new SpottedCoverPoint(point.Position));
            }
            return point.Spotted;
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