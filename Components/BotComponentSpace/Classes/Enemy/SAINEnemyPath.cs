using EFT;
using SAIN.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class SAINEnemyPath : EnemyBase, ISAINEnemyClass
    {
        public EPathDistance EPathDistance
        {
            get
            {
                const float VeryCloseDist = 10f;
                const float CloseDist = 40f;
                const float FarDist = 100f;
                const float VeryFarDist = 200f;

                float distance = PathDistance;
                EPathDistance pathDistance;
                if (distance <= VeryCloseDist)
                {
                    pathDistance = SAIN.EPathDistance.VeryClose;
                }
                else if (distance <= CloseDist)
                {
                    pathDistance = SAIN.EPathDistance.Close;
                }
                else if (distance <= FarDist)
                {
                    pathDistance = SAIN.EPathDistance.Mid;
                }
                else if (distance <= VeryFarDist)
                {
                    pathDistance = SAIN.EPathDistance.Far;
                }
                else
                {
                    pathDistance = SAIN.EPathDistance.VeryFar;
                }

                return pathDistance;
            }
        }

        public float PathDistance { get; private set; } = float.MaxValue;
        public Vector3? LastCornerToEnemy { get; private set; }

        public bool CanSeeLastCornerToEnemy
        {
            get
            {
                var last = LastCornerToEnemy;
                if (last == null)
                {
                    return false;
                }

                if (_nextCheckLast > Time.time)
                {
                    return _canSeeLast;
                }
                _nextCheckLast = Time.time + 0.33f;

                Vector3 cornerTarget = last.Value + Vector3.up;
                Vector3 headPos = Bot.Transform.EyePosition;
                Vector3 direction = cornerTarget - headPos;
                _canSeeLast = !Physics.Raycast(headPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);

                return _canSeeLast;
            }
        }

        public Vector3? BlindCornerToEnemy
        {
            get
            {
                Vector3? blindCorner = _blindCornerFinder.BlindCorner;

                if (blindCorner != null &&
                    SAINPlugin.DebugMode)
                {
                    if (blindcornerGUIObject == null)
                    {
                        blindcornerGUIObject = DebugGizmos.CreateLabel(blindCorner.Value, $"Blind Corner for {BotOwner.name} => {Enemy.EnemyPlayer?.name}");
                    }
                    else
                    {
                        blindcornerGUIObject.WorldPos = blindCorner.Value;
                    }

                    if (blindcornerGameObject != null)
                    {
                        blindcornerGameObject.transform.position = blindCorner.Value;
                    }
                    else
                    {
                        blindcornerGameObject = DebugGizmos.Sphere(blindCorner.Value, 0.1f, Color.red, false);
                    }
                }

                if (!SAINPlugin.DebugMode && blindcornerGUIObject != null)
                {
                    DebugGizmos.DestroyLabel(blindcornerGUIObject);
                    Object.Destroy(blindcornerGameObject);
                    blindcornerGUIObject = null;
                }
                return blindCorner;
            }
        }

        public NavMeshPath PathToEnemy { get; private set; }
        public NavMeshPathStatus PathToEnemyStatus { get; private set; }

        public SAINEnemyPath(SAINEnemy enemy) : base(enemy)
        {
            PathToEnemy = new NavMeshPath();
            _blindCornerFinder = new BlindCornerFinder(enemy);
        }

        public void Init()
        {
            Bot.OnBotDisabled += stopLoop;
            Enemy.OnEnemyForgotten += onEnemyForgotten;
            Enemy.OnEnemyKnown += onEnemyKnown;
        }

        private void stopLoop()
        {
            if (_calcPathCoroutine != null)
            {
                Enemy.Bot.StopCoroutine(_calcPathCoroutine);
                _calcPathCoroutine = null;
                Clear();
            }
        }

        public void onEnemyForgotten(SAINEnemy enemy)
        {
            stopLoop();
        }

        public void onEnemyKnown(SAINEnemy enemy)
        {
            if (_calcPathCoroutine == null)
            {
                _calcPathCoroutine = Enemy.Bot.StartCoroutine(calcPathLoop());
            }
        }

        public void Update()
        {
            if (_calcPathCoroutine == null && Enemy.EnemyKnown)
            {
                Logger.LogWarning($"Enemy Known but coroutine was not started!");
                _calcPathCoroutine = Enemy.Bot.StartCoroutine(calcPathLoop());
            }
        }

        public void Dispose()
        {
            stopLoop();
            Enemy.OnEnemyForgotten -= onEnemyForgotten;
            Enemy.OnEnemyKnown -= onEnemyKnown;
            Enemy.Bot.OnBotDisabled -= stopLoop;
        }

        private IEnumerator calcPathLoop()
        {
            while (true)
            {
                float timeAdd = calcDelayOnDistance();

                if (_calcPathTime + timeAdd < Time.time)
                {
                    _calcPathTime = Time.time;
                    bool isCurrentEnemy = Enemy.IsCurrentEnemy;
                    if (isCurrentEnemy || isEnemyInRange())
                    {
                        //Stopwatch sw = Stopwatch.StartNew();
                        yield return Bot.StartCoroutine(calcPathToEnemy(isCurrentEnemy));
                        //sw.Stop();
                        //if (!Enemy.IsAI)
                        //{
                        //    Logger.LogDebug($"{sw.ElapsedMilliseconds} ms to calcPath");
                        //}
                    }
                }

                yield return null;
            }
        }

        public void Clear()
        {
            if (PathToEnemy != null && PathToEnemy.corners.Length > 0)
                PathToEnemy.ClearCorners();

            PathToEnemyStatus = NavMeshPathStatus.PathInvalid;
            LastCornerToEnemy = null;
            _blindCornerFinder.BlindCorner = null;
            PathDistance = float.MaxValue;
        }

        private float calcDelayOnDistance()
        {
            bool performanceMode = SAINPlugin.LoadedPreset.GlobalSettings.Performance.PerformanceMode;
            bool currentEnemy = Enemy.IsCurrentEnemy;
            bool isAI = Enemy.IsAI;
            float distance = Enemy.RealDistance;

            float maxDelay = isAI ? MAX_FREQ_CALCPATH_AI : MAX_FREQ_CALCPATH;
            if (currentEnemy)
                maxDelay *= CURRENTENEMY_COEF;
            if (performanceMode)
                maxDelay *= PERFORMANCE_MODE_COEF;

            if (distance > MAX_FREQ_CALCPATH_DISTANCE)
            {
                return maxDelay;
            }

            float minDelay = isAI ? MIN_FREQ_CALCPATH_AI : MIN_FREQ_CALCPATH;
            if (currentEnemy)
                minDelay *= CURRENTENEMY_COEF;
            if (performanceMode)
                minDelay *= PERFORMANCE_MODE_COEF;

            if (distance < MIN_FREQ_CALCPATH_DISTANCE)
            {
                return minDelay;
            }

            float difference = distance - MIN_FREQ_CALCPATH_DISTANCE;
            float distanceRatio = difference / DISTANCE_DIFFERENCE;
            float delayDifference = maxDelay - minDelay;

            float result = distanceRatio * delayDifference + minDelay;
            float clampedResult = Mathf.Clamp(result, minDelay, maxDelay);

            if (_nextLogTime < Time.time)
            {
                _nextLogTime = Time.time + 10f;
                //Logger.LogDebug($"{BotOwner.name} calcPathFreqResults for [{Enemy.EnemyPerson.Nickname}] Result: [{result}] preClamped: [[{result}] [{distanceRatio} * {delayDifference} + {minDelay}]] : Distance: [{distance}] : IsAI? [{isAI}] : Current Enemy? [{currentEnemy}] : MinDelay [{minDelay}] : MaxDelay [{maxDelay}]");
            }

            return clampedResult;
        }

        private float _nextLogTime;

        private const float MAX_FREQ_CALCPATH = 2f;
        private const float MAX_FREQ_CALCPATH_AI = 4f;
        private const float MAX_FREQ_CALCPATH_DISTANCE = 250f;

        private const float MIN_FREQ_CALCPATH = 0.5f;
        private const float MIN_FREQ_CALCPATH_AI = 1f;
        private const float MIN_FREQ_CALCPATH_DISTANCE = 50f;

        private const float DISTANCE_DIFFERENCE = MAX_FREQ_CALCPATH_DISTANCE - MIN_FREQ_CALCPATH_DISTANCE;
        private const float PERFORMANCE_MODE_COEF = 1.5f;
        private const float CURRENTENEMY_COEF = 0.5f;

        private bool isEnemyInRange()
        {
            return Enemy.IsAI && Enemy.RealDistance <= MAX_CALCPATH_RANGE_AI || 
                !Enemy.IsAI && Enemy.RealDistance <= MAX_CALCPATH_RANGE;
        }

        private const float MAX_CALCPATH_RANGE = 500f;
        private const float MAX_CALCPATH_RANGE_AI = 300f;

        private IEnumerator calcPathToEnemy(bool isCurrentEnemy)
        {
            // We should always have a not null LastKnownPosition here, but have the real position as a fallback just in case
            Vector3 enemyPosition = Enemy.KnownPlaces.LastKnownPosition ?? EnemyPosition;
            Vector3 botPosition = Bot.Position;

            // Did we already check the current enemy position and has the bot not moved? dont recalc path then
            if (checkPositionsChanged(botPosition, enemyPosition))
            {
                // calculate a path to the enemys position
                yield return Bot.StartCoroutine(calculatePath(botPosition, enemyPosition, PathToEnemy, isCurrentEnemy));

                switch (PathToEnemyStatus)
                {
                    case NavMeshPathStatus.PathInvalid:
                        LastCornerToEnemy = null;
                        break;

                    case NavMeshPathStatus.PathPartial:
                    case NavMeshPathStatus.PathComplete:
                        yield return Bot.StartCoroutine(analyzePath(PathToEnemy, enemyPosition, isCurrentEnemy));
                        break;
                }

                Enemy.OnPathUpdated?.Invoke(Enemy);
            }
        }

        private bool checkPositionsChanged(Vector3 botPosition, Vector3 enemyPosition)
        {
            // Did we already check the current enemy position and has the bot not moved? dont recalc path then
            if (_enemyLastPosChecked != null
                && (_enemyLastPosChecked.Value - enemyPosition).sqrMagnitude < 0.1f
                && (_botLastPosChecked - botPosition).sqrMagnitude < 0.1f)
            {
                return false;
            }

            // cache the positions we are currently checking
            _enemyLastPosChecked = enemyPosition;
            _botLastPosChecked = botPosition;
            return true;
        }

        private IEnumerator calculatePath(Vector3 botPosition, Vector3 enemyPosition, NavMeshPath path, bool isCurrentEnemy)
        {
            path.ClearCorners();
            NavMesh.CalculatePath(botPosition, enemyPosition, -1, path);
            PathToEnemyStatus = path.status;
            PathDistance = CalculatePathLength(path.corners);
            yield return null;
        }

        private IEnumerator analyzePath(NavMeshPath path, Vector3 enemyPosition, bool isCurrentEnemy)
        {
            findLastCorner(enemyPosition, path.status, path.corners);
            if (isCurrentEnemy)
            {
                yield return Bot.StartCoroutine(_blindCornerFinder.FindBlindCorner(path));
            }
            else
            {
                yield return null;
            }
        }

        public float CalculatePathLength(Vector3[] corners)
        {
            if (corners == null)
            {
                return float.MaxValue;
            }
            float result = 0f;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[i + 1];
                result += (a - b).magnitude;
            }
            return result;
        }

        private void findLastCorner(Vector3 enemyPosition, NavMeshPathStatus pathStatus, Vector3[] corners)
        {
            int length = corners.Length;
            // find the last corner before arriving at an enemy position, and then check if we can see it.
            if (pathStatus == NavMeshPathStatus.PathComplete &&
                length > 2)
            {
                LastCornerToEnemy = corners[length - 2];
                return;
            }

            LastCornerToEnemy = corners[length - 1];
        }

        private Coroutine _calcPathCoroutine;
        private GUIObject blindcornerGUIObject;
        private GameObject blindcornerGameObject;
        private bool _canSeeLast;
        private float _nextCheckLast;
        private Vector3? _enemyLastPosChecked;
        private Vector3 _botLastPosChecked;
        private readonly BlindCornerFinder _blindCornerFinder;
        private float _calcPathTime = 0f;
    }

    public class BlindCornerFinder : EnemyBase
    {
        public BlindCornerFinder(SAINEnemy enemy) : base(enemy)
        {
        }

        public IEnumerator FindBlindCorner(NavMeshPath path)
        {
            _corners.Clear();
            _corners.AddRange(path.corners);
            if (_corners.Count > 2)
            {
                Vector3 lookPoint = Bot.Transform.EyePosition;
                Vector3 lookOffset = lookPoint - Bot.Position;
                yield return Bot.StartCoroutine(findBlindCorner(_corners, lookPoint, lookOffset.y));
                yield return Bot.StartCoroutine(findRealCorner(_blindCornerGround, _cornerNotVisible, lookPoint, lookOffset.y));
            }
            _corners.Clear();
        }

        private IEnumerator clearShortCorners(List<Vector3> corners, float min)
        {
            int removed = 0;
            int count = corners.Count;

            //StringBuilder stringBuilder = new StringBuilder();
            //stringBuilder.AppendLine($"Clearing Short Corners of [{count}] for [{Bot.name}] with min [{min}]...");

            for (int i = count - 2; i >= count; i--)
            {
                Vector3 cornerA = corners[i];
                Vector3 cornerB = corners[i + 1];

                float magnitude = (cornerA - cornerB).magnitude;
                //Logger.LogDebug($"{i} to {i + 1} mag: [{magnitude}] min [{min}]");

                if (magnitude > min)
                    continue;

                corners[i + 1] = Vector3.Lerp(cornerA, cornerB, 0.5f);
                corners.RemoveAt(i);

                //stringBuilder.AppendLine($"Corner [{i + 1}] replaced. Removed [{i}] because Magnitude [{magnitude}] with min [{min}]");
                removed++;
            }

            if (removed > 0)
            {
                //stringBuilder.AppendLine($"Finished Clearing Short Corners. Removed [{removed}] corners from [{count}]");
                //Logger.LogDebug(stringBuilder.ToString());
            }

            yield return null;
        }

        private IEnumerator findBlindCorner(List<Vector3> corners, Vector3 lookPoint, float height)
        {
            Vector3? result = null;
            Vector3? notVisCorner = null;
            int count = corners.Count;
            if (count > 2)
            {
                for (int i = 1; i < count; i++)
                {
                    Vector3 target = corners[i];
                    target.y += height;
                    Vector3 direction = target - lookPoint;

                    if (Physics.Raycast(lookPoint, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        result = corners[i - 1];
                        notVisCorner = corners[i];
                        //DebugGizmos.Line(target, lookPoint, Color.red, 0.05f, true, 5f, true);
                        break;
                    }
                    //DebugGizmos.Line(target, lookPoint, Color.white, 0.05f, true, 5f, true);
                    //yield return null;
                }
                if (result == null && count > 1)
                {
                    result = corners[1];
                }
                if (notVisCorner == null && count > 2)
                {
                    notVisCorner = corners[count - 1];
                }
            }
            _blindCornerGround = result ?? Vector3.zero;
            _cornerNotVisible = notVisCorner ?? Vector3.zero;
            yield return null;
        }

        private IEnumerator findRealCorner(Vector3 blindCorner, Vector3 notVisibleCorner, Vector3 lookPoint, float height, int iterations = 15)
        {
            //StringBuilder stringBuilder = new StringBuilder();
            //stringBuilder.AppendLine($"Finding Real Blind Corner for [{Bot.name}]...");

            if (blindCorner == Vector3.zero)
            {
                BlindCorner = null;
                yield break;
            }
            blindCorner.y += height;
            BlindCorner = blindCorner;
            if (notVisibleCorner == Vector3.zero)
            {
                yield break;
            }

            float sign = Vector.FindFlatSignedAngle(blindCorner, notVisibleCorner, lookPoint);
            float angle = sign <= 0 ? -10f : 10f;
            float rotationStep = angle / iterations;

            //stringBuilder.AppendLine($"Angle to check [{angle}] Step Angle [{rotationStep}]");

            Vector3 directionToBlind = blindCorner - lookPoint;

            int raycasts = 0;

            for (int i = 0; i < iterations; i++)
            {
                directionToBlind = Vector.Rotate(directionToBlind, 0, rotationStep, 0);
                if (!Physics.Raycast(lookPoint, directionToBlind, directionToBlind.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    BlindCorner = lookPoint + directionToBlind;
                }
                else
                {
                    //stringBuilder.AppendLine($"Angle where LOS broken [{rotationStep * i}] after [{i}] iterations");
                    break;
                }
                raycasts++;

                if (raycasts >= 3)
                {
                    yield return null;
                }
            }

            //stringBuilder.AppendLine("Finished Checking for real Blind Corner");
            //Logger.LogAndNotifyDebug(stringBuilder.ToString());
        }

        public Vector3? BlindCorner { get; set; }
        private Vector3 _blindCornerGround;
        private Vector3 _cornerNotVisible;
        private readonly List<Vector3> _corners = new List<Vector3>();
    }
}