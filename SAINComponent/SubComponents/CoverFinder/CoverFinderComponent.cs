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
using System.Threading.Tasks;
using UnityEngine.AI;
using RootMotion.FinalIK;
using static RootMotion.FinalIK.GenericPoser;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public enum CoverFinderStatus
    {
        None = 0,
        Idle = 1,
        SearchingColliders = 1,
        RecheckingPointsWithLimit = 2,
        RecheckingPointsNoLimit = 3,
    }

    public class CoverFinderComponent : MonoBehaviour, ISAINSubComponent
    {
        public bool ProcessingLimited { get; private set; }
        public CoverFinderStatus CurrentStatus { get; private set; }

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

        private Collider[] Colliders = new Collider[250];

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
            if (getTargetPosition(out Vector3? target))
            {
                TargetPoint = target.Value;
            }
            setTargetCoverCounts();
        }

        private bool getTargetPosition(out Vector3? target)
        {
            if (SAIN.Grenade.GrenadeDangerPoint != null)
            {
                target = SAIN.Grenade.GrenadeDangerPoint;
                return true;
            }
            target = SAIN.CurrentTargetPosition;
            return target != null;
        }

        private void setTargetCoverCounts()
        {
            if (_nextUpdateTargetTime > Time.time)
            {
                return;
            }
            _nextUpdateTargetTime = Time.time + 0.1f;

            int targetCount;
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.PerformanceMode)
            {
                if (SAIN.Enemy != null)
                {
                    targetCount = SAIN.Enemy.IsAI ? 2 : 4;
                }
                else
                {
                    targetCount = 1;
                }
            }
            else
            {
                if (SAIN.Enemy != null)
                {
                    targetCount = SAIN.Enemy.IsAI ? 4 : 8;
                }
                else
                {
                    targetCount = 2;
                }
            }
            TargetCoverCount = targetCount;
        }

        private float _nextUpdateTargetTime;
        private int TargetCoverCount = 1;
        private static bool DebugCoverFinder => SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder;

        public void LookForCover()
        {
            if (TakeCoverCoroutine == null)
            {
                TakeCoverCoroutine = StartCoroutine(FindCover());
            }
        }

        public void StopLooking()
        {
            if (TakeCoverCoroutine != null)
            {
                CurrentStatus = CoverFinderStatus.None;
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

        private IEnumerator RecheckCoverPoints(bool limit = true)
        {
            if (!limit || (limit && HavePositionsChanged()))
            {
                var _lastStatus = CurrentStatus;
                CurrentStatus = CoverFinderStatus.RecheckingPointsNoLimit;
                float time = Time.time;
                for (int i = CoverPoints.Count - 1; i >= 0; i--)
                {
                    var coverPoint = CoverPoints[i];
                    if (coverPoint == null)
                    {
                        CoverPoints.RemoveAt(i);
                    }
                    else if (coverPoint.TimeLastUpdated + 0.25f < time)
                    {
                        if (PointStillGood(coverPoint) == false)
                        {
                            coverPoint.IsBad = true;
                            CoverPoints.RemoveAt(i);
                        }

                        if (limit && ShallLimitProcessing())
                        {
                            CurrentStatus = CoverFinderStatus.RecheckingPointsWithLimit;
                            yield return new WaitForSeconds(0.05f);
                            continue;
                        }
                        else
                        {
                            yield return null;
                            continue;
                        }
                    }
                    continue;
                }
                CurrentStatus = _lastStatus;
            }
        }

        private bool HavePositionsChanged()
        {
            float recheckThresh = 0.5f;
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.PerformanceMode)
            {
                recheckThresh = 1.5f;
            }
            if ((_lastRecheckTargetPosition - TargetPoint).sqrMagnitude < recheckThresh * recheckThresh
                && (_lastRecheckBotPosition - OriginPoint).sqrMagnitude < recheckThresh * recheckThresh)
            {
                return false;
            }

            _lastRecheckTargetPosition = TargetPoint;
            _lastRecheckBotPosition = OriginPoint;

            return true;
        }

        private Vector3 _lastRecheckTargetPosition;
        private Vector3 _lastRecheckBotPosition;

        private bool ShallLimitProcessing()
        {
            if (SAIN.HasEnemy && SAIN.Enemy.IsAI)
            {
                ProcessingLimited = true;
                return true;
            }

            ProcessingLimited = false;
            SoloDecision soloDecision = SAIN.Decision.CurrentSoloDecision;
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.PerformanceMode 
                && soloDecision != SoloDecision.WalkToCover 
                && soloDecision != SoloDecision.RunToCover 
                && soloDecision != SoloDecision.Retreat 
                && soloDecision != SoloDecision.RunAway)
            {
                ProcessingLimited = true;
            }
            else if (soloDecision == SoloDecision.HoldInCover 
                || soloDecision == SoloDecision.Search)
            {
                ProcessingLimited = true;
            }

            return ProcessingLimited;
        }

        private static readonly List<string> _excludedColliderNames = new List<string>
        {
            "metall_fence_2",
            "metallstolb",
            "stolb",
            "fonar_stolb",
            "fence_grid",
            "metall_fence_new",
            "ladder_platform",
            "frame_L",
            "frame_small_collider",
            "bump2x_p3_set4x",
            "bytovka_ladder",
            "sign",
            "sign17_lod",
            "ograda1",
            "ladder_metal"
        };

        private bool ColliderAlreadyUsed(Collider collider)
        {
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                if (collider == CoverPoints[i].Collider)
                {
                    return true;
                }
            }
            return false;
        }

        private bool filterCollider(Collider collider)
        {
            if (_excludedColliderNames.Contains(collider.name)
                    || _excludedColliderNames.Contains(collider.gameObject?.name)
                    || _excludedColliderNames.Contains(collider.attachedRigidbody?.name))
            {
                return true;
            }
            return false;
        }

        private IEnumerator FindCover()
        {
            while (true)
            {
                Stopwatch fullStopWatch = Stopwatch.StartNew();

                ClearSpotted();

                if (HavePositionsChanged())
                {
                    CurrentStatus = CoverFinderStatus.RecheckingPointsNoLimit;
                    float time = Time.time;
                    for (int i = CoverPoints.Count - 1; i >= 0; i--)
                    {
                        var coverPoint = CoverPoints[i];
                        if (coverPoint == null)
                        {
                            CoverPoints.RemoveAt(i);
                        }
                        else
                        {
                            if (coverPoint.TimeLastUpdated + 0.25f < Time.time)
                            {
                                if (!PointStillGood(coverPoint))
                                {
                                    coverPoint.IsBad = true;
                                    CoverPoints.RemoveAt(i);
                                }
                                yield return null;
                            }
                        }
                    }
                }

                CurrentStatus = CoverFinderStatus.Idle;

                Stopwatch findFirstPointStopWatch = null;
                if (CoverPoints.Count == 0 && SAINPlugin.DebugMode)
                {
                    findFirstPointStopWatch = Stopwatch.StartNew();
                }

                int totalChecked = 0;
                int waitCount = 0;
                int coverCount = CoverPoints.Count;
                Vector3 targetPositionAtStart = TargetPoint;
                int recheckPointCount = 0;
                SoloDecision decisionAtStart = SAIN.Decision.CurrentSoloDecision;

                int max = TargetCoverCount;

                float DistanceThreshold = 4;
                if (SAINPlugin.LoadedPreset.GlobalSettings.General.PerformanceMode)
                {
                    DistanceThreshold = 6f;
                }

                if (coverCount < max
                    || (LastPositionChecked - OriginPoint).sqrMagnitude > DistanceThreshold * DistanceThreshold)
                {
                    CurrentStatus = CoverFinderStatus.SearchingColliders;
                    LastPositionChecked = OriginPoint;

                    Collider[] colliders = GetColliders(out int hits);
                    for (int i = 0; i < hits; i++)
                    {
                        totalChecked++;
                        waitCount++;

                        // The main Calculations
                        Collider collider = colliders[i];
                        if (!filterCollider(collider) 
                            && !ColliderAlreadyUsed(collider)
                            && CoverAnalyzer.CheckCollider(collider, out var newPoint))
                        {
                            CoverPoints.Add(newPoint);
                            coverCount++;

                            // Limit the cpu time per frame. Generic optimization
                            if (ShallLimitProcessing())
                            {
                                yield return null;
                                //yield return new WaitForSeconds(0.05f);
                            }
                        }

                        // Check if a bot's decision has changed mid-loop. If so, have the existing points be rechecked right now.
                        SoloDecision decisionRightNow = SAIN.Decision.CurrentSoloDecision;
                        if (coverCount > 0
                            && decisionRightNow != decisionAtStart
                            && (decisionAtStart == SoloDecision.HoldInCover || decisionAtStart == SoloDecision.Search || decisionAtStart == SoloDecision.None))
                        {
                            if (decisionRightNow == SoloDecision.RunToCover || decisionRightNow == SoloDecision.Retreat || decisionRightNow == SoloDecision.WalkToCover)
                            {
                                decisionAtStart = decisionRightNow;
                                yield return RecheckCoverPoints(false);
                            }
                        }

                        // Check if a bot's target has moved mid-loop. If so, have the existing points be rechecked right now.
                        recheckPointCount++;
                        if (coverCount > 0 && recheckPointCount >= 10)
                        {
                            recheckPointCount = 0;
                            if ((targetPositionAtStart - TargetPoint).sqrMagnitude > 1f)
                            {
                                targetPositionAtStart = TargetPoint;

                                yield return RecheckCoverPoints(false);
                            }
                        }

                        // Generic Optimization
                        // if (coverCount > 0 && ShallLimitProcessing())
                        // {
                        //     yield return null;
                        // }


                        // Main Optimization, scales with the amount of points a bot currently has, and slows down the rate as it grows.
                        if (coverCount >= max)
                        {
                            break;
                        }
                        else if (coverCount > 2)
                        {
                            yield return null;
                        }
                        else if (coverCount > 0)
                        {
                            // How long did it take to find at least 1 point?
                            if (findFirstPointStopWatch?.IsRunning == true)
                            {
                                findFirstPointStopWatch.Stop();
                                if (_debugTimer < Time.time)
                                {
                                    _debugTimer = Time.time + 5;
                                    Logger.LogAndNotifyDebug($"Time to Find First CoverPoint: [{findFirstPointStopWatch.ElapsedMilliseconds}ms]");
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
                    Logger.LogAndNotifyDebug($"Time to Complete Coverfinder Loop: [{fullStopWatch.ElapsedMilliseconds}ms]");
                }
                CurrentStatus = CoverFinderStatus.None;
                yield return new WaitForSeconds(CoverUpdateFrequency);
            }
        }

        private static float _debugTimer;
        private static float _debugTimer2;

        public static void OrderPointsByPathDist(List<CoverPoint> points, SAINComponentClass sain)
        {
            points.Sort((x, y) => x.PathLength.CompareTo(y.PathLength));
        }

        private static float CoverUpdateFrequency => SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverUpdateFrequency;

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
                    if (point.IsSafePath)
                    {
                        safestResult = point;
                    }
                    result = point;
                }
            }
            return safestResult ?? result;
        }

        private static float FallBackPointResetDistance = 35;
        private static float FallBackPointNextAllowedResetDelayTime = 3f;
        private static float FallBackPointNextAllowedResetTime = 0;

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

        public List<SpottedCoverPoint> SpottedCoverPoints { get; private set; } = new List<SpottedCoverPoint>();

        public bool PointStillGood(CoverPoint coverPoint)
        {
            return coverPoint != null && !PointIsSpotted(coverPoint) && CoverAnalyzer.CheckCollider(coverPoint);
        }

        private void ClearSpotted()
        {
            if (_nextClearSpottedTime < Time.time)
            {
                _nextClearSpottedTime = Time.time + 0.5f;

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
                Vector3 spottedPointPos = spottedPoint.CoverPoint.Position;
                if (spottedPoint.TooClose(spottedPointPos, point.Position))
                {
                    return true;
                }
            }
            if (point.Spotted)
            {
                SpottedCoverPoints.Add(new SpottedCoverPoint(point));
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
            try
            {
                StopAllCoroutines();
                Destroy(this);
            }
            catch { }
        }

        private Coroutine TakeCoverCoroutine;

        public Vector3 OriginPoint => SAIN.Position;
        public Vector3 TargetPoint { get; private set; }
    }
}