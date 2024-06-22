using EFT;
using SAIN.Helpers;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyPath : EnemyBase, ISAINEnemyClass
    {
        public EPathDistance EPathDistance
        {
            get
            {
                float distance = PathDistance;
                if (distance <= ENEMY_DISTANCE_VERYCLOSE)
                {
                    return EPathDistance.VeryClose;
                }
                if (distance <= ENEMY_DISTANCE_CLOSE)
                {
                    return EPathDistance.Close;
                }
                if (distance <= ENEMY_DISTANCE_MID)
                {
                    return EPathDistance.Mid;
                }
                if (distance <= ENEMY_DISTANCE_FAR)
                {
                    return EPathDistance.Far;
                }
                return EPathDistance.VeryFar;
            }
        }

        const float ENEMY_DISTANCE_VERYCLOSE = 10f;
        const float ENEMY_DISTANCE_CLOSE = 20f;
        const float ENEMY_DISTANCE_MID = 80f;
        const float ENEMY_DISTANCE_FAR = 150f;

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

        public SAINEnemyPath(Enemy enemy) : base(enemy)
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

        public void onEnemyForgotten(Enemy enemy)
        {
            stopLoop();
        }

        public void onEnemyKnown(Enemy enemy)
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
            Bot.OnBotDisabled -= stopLoop;
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
}