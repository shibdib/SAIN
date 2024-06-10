using EFT;
using SAIN.Helpers;
using SAIN.Plugin;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverFinderComponent : MonoBehaviour, ISAINSubComponent
    {
        public CoverFinderStatus CurrentStatus { get; private set; }

        public Vector3 OriginPoint
        {
            get
            {
                if (_sampleOriginTime < Time.time)
                {
                    _sampleOriginTime = Time.time + 0.1f;
                    Vector3 botPos = Bot.Position;
                    if (NavMesh.SamplePosition(botPos, out var hit, 0.5f, -1))
                    {
                        botPos = hit.position;
                    }
                    _origin = botPos;
                }
                return _origin;
            }
        }

        public Vector3 TargetPoint
        {
            get
            {
                if (_sampleTargetTime < Time.time)
                {
                    _sampleTargetTime = Time.time + 0.1f;
                    if (getTargetPosition(out Vector3? target))
                    {
                        Vector3 targetValue = target.Value;
                        if (NavMesh.SamplePosition(targetValue, out var hit, 0.5f, -1))
                        {
                            targetValue = hit.position;
                        }
                        _targetPoint = targetValue;
                    }
                }
                return _targetPoint;
            }
        }

        public BotComponent Bot { get; private set; }
        public Player Player => Bot.Player;
        public BotOwner BotOwner => Bot.BotOwner;

        public List<CoverPoint> CoverPoints { get; } = new List<CoverPoint>();
        private CoverAnalyzer CoverAnalyzer { get; set; }
        private ColliderFinder ColliderFinder { get; set; }
        public bool ProcessingLimited { get; private set; }

        public CoverPoint FallBackPoint { get; private set; }

        public readonly List<CoverPoint> OldCoverPoints = new List<CoverPoint>(_maxOldPoints);
        public List<SpottedCoverPoint> SpottedCoverPoints { get; private set; } = new List<SpottedCoverPoint>();

        private int _targetCoverCount
        {
            get
            {
                if (_nextUpdateTargetTime < Time.time)
                {
                    _nextUpdateTargetTime = Time.time + 0.1f;

                    int targetCount;
                    if (PerformanceMode)
                    {
                        if (Bot.Enemy != null)
                        {
                            targetCount = Bot.Enemy.IsAI ? 2 : 4;
                        }
                        else
                        {
                            targetCount = 2;
                        }
                    }
                    else
                    {
                        if (Bot.Enemy != null)
                        {
                            targetCount = Bot.Enemy.IsAI ? 4 : 8;
                        }
                        else
                        {
                            targetCount = 2;
                        }
                    }
                    _targetCount = targetCount;
                }
                return _targetCount;
            }
        }

        public void Init(BotComponent botComponent)
        {
            Bot = botComponent;
            ProfileId = botComponent.ProfileId;
            BotName = botComponent.name;

            ColliderFinder = new ColliderFinder(this);
            CoverAnalyzer = new CoverAnalyzer(Bot, this);

            botComponent.OnBotDisabled += StopLooking;
            botComponent.OnSAINDisposed += botDisposed;
        }

        private void Update()
        {
            if (DebugCoverFinder)
            {
                if (CoverPoints.Count > 0)
                {
                    DebugGizmos.Line(CoverPoints.PickRandom().Position, Bot.Transform.HeadPosition, Color.yellow, 0.035f, true, 0.1f);
                }
            }
        }

        private bool getTargetPosition(out Vector3? target)
        {
            if (Bot.Grenade.GrenadeDangerPoint != null)
            {
                target = Bot.Grenade.GrenadeDangerPoint;
                return true;
            }
            target = Bot.CurrentTargetPosition;
            return target != null;
        }

        public void LookForCover()
        {
            if (_findCoverPointsCoroutine == null)
            {
                _findCoverPointsCoroutine = StartCoroutine(findCoverLoop());
            }
            if (_recheckCoverPointsCoroutine == null)
            {
                _recheckCoverPointsCoroutine = StartCoroutine(recheckCoverLoop());
            }
        }

        public void StopLooking()
        {
            if (_findCoverPointsCoroutine != null)
            {
                CurrentStatus = CoverFinderStatus.None;
                StopCoroutine(_findCoverPointsCoroutine);
                _findCoverPointsCoroutine = null;

                StopCoroutine(_recheckCoverPointsCoroutine);
                _recheckCoverPointsCoroutine = null;

                CoverPoints.Clear();

                if (Bot != null)
                {
                    Bot.Cover.CoverInUse = null;
                }

                FallBackPoint = null;
            }
        }

        private void addOldPoint(CoverPoint point)
        {
            if (point != null)
            {
                OldCoverPoints.Add(point);
            }
            if (OldCoverPoints.Count > _maxOldPoints)
            {
                OldCoverPoints.RemoveAt(0);
            }
        }

        private IEnumerator recheckCoverPoints(List<CoverPoint> coverPoints, bool limit = true)
        {
            // if (!limit || (limit && HavePositionsChanged()))
            bool avoidingGrenade = Bot.Decision.CurrentSoloDecision == SoloDecision.AvoidGrenade;
            if (havePositionsChanged() || avoidingGrenade)
            {
                bool shallLimit = limit && !avoidingGrenade && shallLimitProcessing();
                WaitForSeconds wait = new WaitForSeconds(0.05f);

                CoverFinderStatus lastStatus = CurrentStatus;
                CurrentStatus = shallLimit ? CoverFinderStatus.RecheckingPointsWithLimit : CoverFinderStatus.RecheckingPointsNoLimit;

                CoverPoint coverInUse = Bot.Cover.CoverInUse;
                bool updated = false;
                if (coverInUse != null)
                {
                    if (!PointStillGood(coverInUse, avoidingGrenade, out updated, out ECoverFailReason failReason))
                    {
                        //Logger.LogWarning(failReason);
                        coverInUse.IsBad = true;
                        addOldPoint(coverInUse);
                    }
                    if (updated)
                    {
                        yield return shallLimit ? wait : null;
                    }
                }

                for (int i = coverPoints.Count - 1; i >= 0; i--)
                {
                    var coverPoint = coverPoints[i];
                    if (!PointStillGood(coverPoint, avoidingGrenade, out updated, out ECoverFailReason failReason))
                    {
                        //Logger.LogWarning(failReason);
                        coverPoint.IsBad = true;
                        addOldPoint(coverPoint);
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

        private bool havePositionsChanged()
        {
            float recheckThresh = 0.5f;
            if (PerformanceMode)
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

        private bool shallLimitProcessing()
        {
            ProcessingLimited =
                Bot.Enemy?.IsAI == true ||
                limitProcessingFromDecision(Bot.Decision.CurrentSoloDecision);

            return ProcessingLimited;
        }

        private static bool limitProcessingFromDecision(SoloDecision decision)
        {
            switch (decision)
            {
                case SoloDecision.MoveToCover:
                case SoloDecision.RunToCover:
                case SoloDecision.Retreat:
                case SoloDecision.RunAway:
                    return false;

                case SoloDecision.HoldInCover:
                case SoloDecision.Search:
                    return true;

                default:
                    return PerformanceMode;
            }
        }

        private bool colliderAlreadyUsed(Collider collider)
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
            return collider != null &&
                _excludedColliderNames.Contains(collider.transform?.parent?.name);
        }

        private IEnumerator recheckCoverLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);
            while (true)
            {
                ClearSpotted();

                _tempRecheckList.AddRange(CoverPoints);
                yield return recheckCoverPoints(_tempRecheckList, false);
                //yield return checkPathSafety(_tempRecheckList);

                CoverPoints.RemoveAll(x => x.IsBad);
                _tempRecheckList.Clear();

                yield return null;

                OrderPointsByPathDist(CoverPoints, Bot);

                yield return wait;
            }
        }

        private bool needToFindCover(int coverCount, out int max)
        {
            const float distThreshold = 5f;
            const float distThresholdSqr = distThreshold * distThreshold;
            max = _targetCoverCount;
            bool needToFindCover =
                coverCount < max / 2
                || (coverCount <= 1 && coverCount < max)
                || (_lastPositionChecked - OriginPoint).sqrMagnitude >= distThresholdSqr;
            return needToFindCover;
        }

        private IEnumerator findCoverLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);
            while (true)
            {
                int coverCount = CoverPoints.Count;
                if (needToFindCover(coverCount, out int max))
                {
                    CurrentStatus = CoverFinderStatus.SearchingColliders;
                    _lastPositionChecked = OriginPoint;

                    Stopwatch fullStopWatch = Stopwatch.StartNew();
                    Stopwatch findFirstPointStopWatch = coverCount == 0 && SAINPlugin.DebugMode ? Stopwatch.StartNew() : null;

                    Collider[] colliders = _colliderArray;
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
                        OrderPointsByPathDist(CoverPoints, Bot);
                    }

                    if (coverCount > 0)
                    {
                        FallBackPoint = FindFallbackPoint(CoverPoints);
                        if (_debugLogTimer < Time.time && DebugCoverFinder)
                        {
                            _debugLogTimer = Time.time + 1f;
                            Logger.LogInfo($"[{BotOwner.name}] - Found [{coverCount}] CoverPoints. Colliders checked: [{_totalChecked}] Collider Array Size = [{ColliderFinder.HitCount}]");
                        }
                    }
                    else
                    {
                        FallBackPoint = null;
                        if (_debugLogTimer < Time.time && DebugCoverFinder)
                        {
                            _debugLogTimer = Time.time + 1f;
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

                bool failed = false;
                ECoverFailReason failReason = ECoverFailReason.None;
                CoverPoint newPoint = null;

                if (!failed &&
                    filterColliderByName(collider))
                {
                    failed = true;
                    failReason = ECoverFailReason.ExcludedName;
                }

                if (!failed &&
                    colliderAlreadyUsed(collider))
                {
                    failed = true;
                    failReason = ECoverFailReason.ColliderUsed;
                }

                // The main Calculations
                if (!failed &&
                    !CoverAnalyzer.CheckCollider(collider, out newPoint, out failReason))
                {
                    failed = true;
                }

                if (!failed &&
                    newPoint != null)
                {
                    CoverPoints.Add(newPoint);
                }

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

        public static void OrderPointsByPathDist(List<CoverPoint> points, BotComponent sain)
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

        public bool PointStillGood(CoverPoint coverPoint, bool avoidingGrenade, out bool updated, out ECoverFailReason failReason)
        {
            updated = false;
            failReason = ECoverFailReason.None;
            if (coverPoint == null || coverPoint.IsBad)
            {
                failReason = ECoverFailReason.NullOrBad;
                return false;
            }
            if (!coverPoint.ShallUpdate && !avoidingGrenade)
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

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private void botDisposed(string profileId, BotOwner bot)
        {
            if (ProfileId == profileId)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            try
            {
                StopLooking();
                StopAllCoroutines();
                if (Bot != null)
                {
                    Bot.OnSAINDisposed -= botDisposed;
                    Bot.OnBotDisabled -= StopLooking;
                }
            }
            catch { }

            Logger.LogWarning($"Coverfinder for Bot [{BotName}] Destroyed");
            Destroy(this);
        }

        private float _sampleOriginTime;
        private Vector3 _origin;
        private float _sampleTargetTime;
        private readonly Collider[] _colliderArray = new Collider[250];
        private string ProfileId;
        private int _targetCount;
        private float _nextUpdateTargetTime;
        private Vector3 _targetPoint;
        private Vector3 _lastPositionChecked = Vector3.zero;
        private Vector3 _lastRecheckTargetPosition;
        private Vector3 _lastRecheckBotPosition;
        private int _totalChecked;
        private static float _debugTimer;
        private static float _debugTimer2;
        private float _debugLogTimer = 0f;
        private float _nextClearSpottedTime;
        private string BotName;
        private Coroutine _findCoverPointsCoroutine;
        private Coroutine _recheckCoverPointsCoroutine;
        private readonly List<CoverPoint> _tempRecheckList = new List<CoverPoint>();

        private static bool AllCollidersAnalyzed;
        private const int _maxOldPoints = 10;
        public static bool PerformanceMode { get; private set; } = false;
        public static float CoverMinHeight { get; private set; } = 0.5f;
        public static float CoverMinEnemyDist { get; private set; } = 5f;
        public static float CoverMinEnemyDistSqr { get; private set; } = 25f;
        public static bool DebugCoverFinder { get; private set; } = false;

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

        static CoverFinderComponent()
        {
            PresetHandler.OnPresetUpdated += updateSettings;
            updateSettings();
        }

        private static void updateSettings()
        {
            PerformanceMode = SAINPlugin.LoadedPreset.GlobalSettings.Performance.PerformanceMode;
            CoverMinHeight = SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverMinHeight;
            CoverMinEnemyDist = SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverMinEnemyDistance;
            CoverMinEnemyDistSqr = CoverMinEnemyDist * CoverMinEnemyDist;
            DebugCoverFinder = SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder;
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
    }
}