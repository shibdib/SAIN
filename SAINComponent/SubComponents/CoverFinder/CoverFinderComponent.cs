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
using UnityEngine.PlayerLoop;
using Comfort.Common;

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
            SAIN.Decision.OnDecisionMade += decisionMade;
        }

        private void decisionMade(SoloDecision solo, SquadDecision _, SelfDecision __, float ___)
        {

        }

        public Vector3 OriginPoint
        {
            get
            {
                return SAIN.Position;
            }
        }

        private Vector3 _originPoint;
        public Vector3 TargetPoint
        {
            get
            {
                if (getTargetPosition(out Vector3? target))
                {
                    _targetPoint = target.Value;
                }
                return _targetPoint;
            }
        }
        private Vector3 _targetPoint;

        public SAINComponentClass SAIN { get; private set; }
        public Player Player { get; private set; }
        public BotOwner BotOwner { get; private set; }

        public readonly List<CoverPoint> CoverPoints  = new List<CoverPoint>();
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
                    DebugGizmos.Line(CoverPoints.PickRandom().Position, SAIN.Transform.HeadPosition, Color.yellow, 0.035f, true, 0.1f);
                }
            }
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

        private int TargetCoverCount
        {
            get
            {
                if (_nextUpdateTargetTime < Time.time)
                {
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
                            targetCount = 2;
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
                    _targetCoverCount = targetCount;
                }
                return _targetCoverCount;
            }
        }

        private int _targetCoverCount;
        private float _nextUpdateTargetTime;

        private static bool DebugCoverFinder => SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder;

        public void LookForCover()
        {
            if (FindCoverPointsCoroutine == null)
            {
                FindCoverPointsCoroutine = StartCoroutine(findCoverLoop());
            }
            if (RecheckCoverPointsCoroutine == null)
            {
                RecheckCoverPointsCoroutine = StartCoroutine(recheckCoverLoop());
            }
        }

        public void StopLooking()
        {
            if (FindCoverPointsCoroutine != null)
            {
                CurrentStatus = CoverFinderStatus.None;
                StopCoroutine(FindCoverPointsCoroutine);
                FindCoverPointsCoroutine = null;

                StopCoroutine(RecheckCoverPointsCoroutine);
                RecheckCoverPointsCoroutine = null;

                CoverPoints.Clear();
                SAIN.Cover.CoverInUse = null;
                FallBackPoint = null;
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

        private Vector3 _lastPositionChecked = Vector3.zero;

        private IEnumerator RecheckCoverPoints(List<CoverPoint> coverPoints, bool limit = true)
        {
            if (!limit || (limit && HavePositionsChanged()))
            {
                bool shallLimit = limit && shallLimitProcessing();
                WaitForSeconds wait = new WaitForSeconds(0.05f);

                CoverFinderStatus lastStatus = CurrentStatus;
                CurrentStatus = shallLimit ? CoverFinderStatus.RecheckingPointsWithLimit : CoverFinderStatus.RecheckingPointsNoLimit;

                CoverPoint coverInUse = SAIN.Cover.CoverInUse;
                bool updated = false;
                if (coverInUse != null)
                {
                    if (!PointStillGood(coverInUse, out updated, out ECoverFailReason failReason))
                    {
                        //Logger.LogWarning(failReason);
                        coverInUse.IsBad = true;
                    }
                    if (updated)
                    {
                        yield return shallLimit ? wait : null;
                    }
                }

                for (int i = coverPoints.Count - 1; i >= 0; i--)
                {
                    var coverPoint = coverPoints[i];
                    if (!PointStillGood(coverPoint, out updated, out ECoverFailReason failReason))
                    {
                        //Logger.LogWarning(failReason);
                        coverPoint.IsBad = true;
                    }
                    if (updated)
                    {
                        yield return shallLimit ? wait : null;
                    }
                }
                CurrentStatus = lastStatus;

                yield return null;
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

        private bool shallLimitProcessing()
        {
            if (SAIN.HasEnemy && SAIN.Enemy.IsAI)
            {
                ProcessingLimited = true;
                return true;
            }

            ProcessingLimited = false;
            SoloDecision soloDecision = SAIN.Decision.CurrentSoloDecision;
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.PerformanceMode 
                && soloDecision != SoloDecision.MoveToCover 
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

        private bool filterColliderByName(Collider collider)
        {
            if (collider == null)
            {
                return true;
            }
            if (_excludedColliderNames.Contains(collider.transform?.parent?.name))
            {
                //Logger.LogInfo($"Filtered collider.transform?.parent?.name [{collider.transform?.parent?.name}]");
                return true;
            }
            return false;
            if (_excludedColliderNames.Contains(collider.name))
            {
                Logger.LogInfo($"Filtered collider.name [{collider.name}]");
                return true;
            }
            if (_excludedColliderNames.Contains(collider.gameObject?.name))
            {
                Logger.LogInfo($"Filtered collider.gameObject?.name [{collider.gameObject?.name}]");
                return true;
            }
            if (_excludedColliderNames.Contains(collider.attachedRigidbody?.name))
            {
                Logger.LogInfo($"Filtered collider.attachedRigidbody?.name [{collider.attachedRigidbody?.name}]");
                return true;
            }
            if (_excludedColliderNames.Contains(collider.transform?.name))
            {
                Logger.LogInfo($"Filtered collider.transform?.name [{collider.transform?.name}]");
                return true;
            }
            if (_excludedColliderNames.Contains(collider.transform?.parent?.gameObject?.name))
            {
                Logger.LogInfo($"Filtered collider.transform?.parent?.gameObject?.name [{collider.transform?.parent?.gameObject?.name}]");
                return true;
            }
            return false;
        }

        private IEnumerator recheckCoverLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);
            while (true)
            {
                ClearSpotted();

                _tempRecheckList.AddRange(CoverPoints);
                yield return RecheckCoverPoints(_tempRecheckList, false);
                //yield return checkPathSafety(_tempRecheckList);

                CoverPoints.RemoveAll(x => x.IsBad);
                _tempRecheckList.Clear();

                yield return null;

                OrderPointsByPathDist(CoverPoints, SAIN);

                yield return wait;
            }
        }

        private readonly List<CoverPoint> _tempRecheckList = new List<CoverPoint>();
        private readonly List<CoverPoint> _tempNewCoverList = new List<CoverPoint>();

        private IEnumerator findCoverLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);
            while (true)
            {
                const float distThreshold = 5f;
                const float distThresholdSqr = distThreshold * distThreshold;

                int max = TargetCoverCount;
                int coverCount = CoverPoints.Count;
                bool needToFindCover = 
                    coverCount < max / 2 
                    || (coverCount <= 1 && coverCount < max)
                    || (_lastPositionChecked - OriginPoint).sqrMagnitude >= distThresholdSqr;

                if (needToFindCover)
                {
                    CurrentStatus = CoverFinderStatus.SearchingColliders;
                    _lastPositionChecked = OriginPoint;
                    Stopwatch fullStopWatch = Stopwatch.StartNew();

                    Stopwatch findFirstPointStopWatch = coverCount == 0 && SAINPlugin.DebugMode ? Stopwatch.StartNew() : null;

                    Collider[] colliders = Colliders;
                    yield return ColliderFinder.GetNewColliders(colliders);
                    ColliderFinder.SortArrayBotDist(colliders);

                    int hits = ColliderFinder.HitCount;

                    yield return findCoverPoints(colliders, ColliderFinder.HitCount, max, findFirstPointStopWatch);

                    coverCount = CoverPoints.Count;

                    if (coverCount > 0)
                    {
                        //yield return checkPathSafety(CoverPoints);
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
                            Logger.LogInfo($"[{BotOwner.name}] - Found [{coverCount}] CoverPoints. Colliders checked: [{_totalChecked}] Collider Array Size = [{ColliderFinder.HitCount}]");
                        }
                    }
                    else
                    {
                        FallBackPoint = null;
                        if (DebugLogTimer < Time.time && DebugCoverFinder)
                        {
                            DebugLogTimer = Time.time + 1f;
                            Logger.LogWarning($"[{BotOwner.name}] - No Cover Found! Valid Colliders checked: [{_totalChecked}] Collider Array Size = [{ColliderFinder.HitCount}]");
                        }
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
                }

                CurrentStatus = CoverFinderStatus.None;
                yield return wait;
            }
        }

        private int _totalChecked;

        private IEnumerator findCoverPoints(Collider[] colliders, int hits, int max, Stopwatch debugStopWatch)
        {
            _totalChecked = 0;
            int waitCount = 0;
            for (int i = 0; i < hits; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                _totalChecked++;

                waitCount++;
                ECoverFailReason failReason = ECoverFailReason.None;
                // The main Calculations
                if (!filterColliderByName(collider))
                {
                    if (!ColliderAlreadyUsed(collider))
                    {
                        if (CoverAnalyzer.CheckCollider(collider, out var newPoint, out failReason))
                        {
                            CoverPoints.Add(newPoint);
                        }
                    }
                    else
                    {
                        failReason = ECoverFailReason.ColliderUsed;
                    }
                }
                else
                {
                    failReason = ECoverFailReason.ExcludedName;
                }

                //if (failReason != ECoverFailReason.None)
                //    Logger.LogWarning(failReason);

                int coverCount = CoverPoints.Count;

                if (coverCount >= max)
                {
                    break;
                }
                // Main Optimization, scales with the amount of points a bot currently has, and slows down the rate as it grows.
                if (coverCount > 2)
                {
                    yield return null;
                }
                else if (coverCount > 0)
                {
                    // How long did it take to find at least 1 point?
                    if (debugStopWatch?.IsRunning == true)
                    {
                        debugStopWatch.Stop();
                        if (_debugTimer < Time.time)
                        {
                            _debugTimer = Time.time + 5;
                            Logger.LogAndNotifyDebug($"Time to Find First CoverPoint: [{debugStopWatch.ElapsedMilliseconds}ms]");
                        }
                    }
                    if (waitCount >= 3 || shallLimitProcessing())
                    {
                        waitCount = 0;
                        yield return null;
                    }
                }
                else if (waitCount >= 5)
                {
                    waitCount = 0;
                    yield return null;
                }
            }
        }

        private static float _debugTimer;
        private static float _debugTimer2;

        public static void OrderPointsByPathDist(List<CoverPoint> points, SAINComponentClass sain)
        {
            points.Sort((x, y) => x.RoundedPathLength.CompareTo(y.RoundedPathLength));
        }

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

        private IEnumerator checkPathSafety(List<CoverPoint> coverPoints)
        {
            for (int i = 0; i < coverPoints.Count; i++)
            {
                var cover = coverPoints[i];
                if (cover != null && !cover.IsBad)
                {
                    cover.CheckPathSafety(out bool didCheck);
                    if (didCheck)
                    {
                        yield return null;
                    }
                }
            }
            yield return null;
        }

        private float DebugLogTimer = 0f;

        public List<SpottedCoverPoint> SpottedCoverPoints { get; private set; } = new List<SpottedCoverPoint>();

        public bool PointStillGood(CoverPoint coverPoint, out bool updated, out ECoverFailReason failReason)
        {
            updated = false;
            failReason = ECoverFailReason.None;
            if (coverPoint == null || coverPoint.IsBad)
            {
                failReason = ECoverFailReason.NullOrBad;
                return false;
            }
            if (!coverPoint.ShallUpdate)
            {
                return true;
            }
            if (PointIsSpotted(coverPoint))
            {
                failReason = ECoverFailReason.Spotted;
                return false;
            }
            updated = true;
            return CoverAnalyzer.CheckCollider(coverPoint, out failReason);
        }

        private void ClearSpotted()
        {
            if (_nextClearSpottedTime < Time.time)
            {
                _nextClearSpottedTime = Time.time + 0.5f;
                SpottedCoverPoints.RemoveAll(x => x.IsValidAgain);
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

        public void Dispose()
        {
            try
            {
                StopAllCoroutines();
                SAIN.Decision.OnDecisionMade -= decisionMade;
                Destroy(this);
            }
            catch { }
        }

        private Coroutine FindCoverPointsCoroutine;
        private Coroutine RecheckCoverPointsCoroutine;

    }
}