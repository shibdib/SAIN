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
    public class SAINEnemyPath : EnemyBase
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
            enemy.Bot.OnBotDisabled += StopLoop;
            _blindCornerFinder = new BlindCornerFinder(enemy);
        }

        public void StopLoop()
        {
            if (_calcPathCoroutine != null)
            {
                Enemy.Bot.StopCoroutine(_calcPathCoroutine);
                _calcPathCoroutine = null;
                Clear();
            }
        }

        public void Update()
        {
            if (_calcPathCoroutine == null)
            {
                _calcPathCoroutine = Enemy.Bot.StartCoroutine(calcPathLoop());
            }
        }

        public void Dispose()
        {
            StopLoop();
            Enemy.Bot.OnBotDisabled -= StopLoop;
        }

        public void UpdateOld(bool isCurrentEnemy)
        {
            float timeAdd = 1f;
            if (!isCurrentEnemy && Enemy.IsAI)
            {
                timeAdd = 4f;
            }
            if (!isCurrentEnemy && !Enemy.IsAI)
            {
                timeAdd = 1f;
            }
            if (isCurrentEnemy && !Enemy.IsAI)
            {
                timeAdd = 0.33f;
            }

            if (_calcPathTime + timeAdd < Time.time)
            {
                _calcPathTime = Time.time;
                if (!isCurrentEnemy)
                {
                    bool clearPath = false;
                    if (Enemy.Seen == false && Enemy.Heard == false)
                    {
                        clearPath = true;
                    }
                    else if (Enemy.TimeSinceLastKnownUpdated > BotOwner.Settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC)
                    {
                        clearPath = true;
                    }
                    else if (Enemy.IsAI && Enemy.RealDistance > 300 || !Enemy.IsAI && Enemy.RealDistance > 500)
                    {
                        clearPath = true;
                    }
                    if (clearPath)
                    {
                        Clear();
                        return;
                    }
                }
                calcPathToEnemy(isCurrentEnemy);
            }
        }

        private IEnumerator calcPathLoop()
        {
            while (true)
            {
                bool isCurrentEnemy = Enemy.IsCurrentEnemy;
                float timeAdd = findDelay(isCurrentEnemy);

                if (_calcPathTime + timeAdd < Time.time)
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    _calcPathTime = Time.time;

                    if (!isCurrentEnemy && shallClear())
                    {
                        Clear();
                        watch.Stop();
                        yield return null;
                        continue;
                    }

                    yield return calcPathToEnemy(isCurrentEnemy);
                    watch.Stop();
                    Logger.LogAndNotifyDebug($"Time To CalcPath [{watch.ElapsedMilliseconds}].ms");
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

        private float findDelay(bool isCurrentEnemy)
        {
            if (!isCurrentEnemy)
            {
                return Enemy.IsAI ? 1f : 0.5f;
            }
            return Enemy.IsAI ? 0.5f : 0.25f;
        }

        private bool shallClear()
        {
            bool clearPath = false;
            if (Enemy.Seen == false && Enemy.Heard == false)
            {
                clearPath = true;
            }
            else if (Enemy.TimeSinceLastKnownUpdated > BotOwner.Settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC)
            {
                clearPath = true;
            }
            else if (Enemy.IsAI && Enemy.RealDistance > 300 || !Enemy.IsAI && Enemy.RealDistance > 500)
            {
                clearPath = true;
            }
            return clearPath;
        }

        private IEnumerator calcPathToEnemy(bool isCurrentEnemy)
        {
            // We should always have a not null LastKnownPosition here, but have the real position as a fallback just in case
            Vector3 enemyPosition = Enemy.KnownPlaces.LastKnownPosition ?? EnemyPosition;
            Vector3 botPosition = Bot.Position;

            // Did we already check the current enemy position and has the bot not moved? dont recalc path then
            if (!checkShallCalc(botPosition, enemyPosition))
            {
                yield break;
            }

            // calculate a path to the enemys position
            yield return calculatePath(botPosition, enemyPosition, PathToEnemy, isCurrentEnemy);

            switch (PathToEnemyStatus)
            {
                case NavMeshPathStatus.PathInvalid:
                    LastCornerToEnemy = null;
                    break;

                case NavMeshPathStatus.PathPartial:
                case NavMeshPathStatus.PathComplete:
                    yield return analyzePath(PathToEnemy, enemyPosition, isCurrentEnemy);
                    break;
            }
        }

        private bool checkShallCalc(Vector3 botPosition, Vector3 enemyPosition)
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
                yield return _blindCornerFinder.FindBlindCorner(path);
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
            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;
            Vector3 lookPoint = Bot.Transform.EyePosition;
            Vector3 lookOffset = lookPoint - Bot.Position;

            _corners.AddRange(path.corners);

            //yield return clearShortCorners(_corners, 0.1f);
            //yield return clearShortCorners(_corners, 0.2f);
            //yield return clearShortCorners(_corners, 0.5f);
            //yield return clearShortCorners(_corners, 1f);
            //yield return clearShortCorners(_corners, 2f);
            //yield return clearShortCorners(_corners, 5f);

            yield return findBlindCorner(_corners, lookPoint, lookOffset.y);
            yield return findRealCorner(_blindCornerGround, _cornerNotVisible, lookPoint, lookOffset.y);
            _corners.Clear();
        }

        private IEnumerator clearShortCorners(List<Vector3> corners, float min)
        {
            int removed = 0;
            int count = corners.Count;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Clearing Short Corners of [{count}] for [{Bot.name}] with min [{min}]...");

            for (int i = count - 2; i >= count; i--)
            {
                Vector3 cornerA = corners[i];
                Vector3 cornerB = corners[i + 1];

                float magnitude = (cornerA - cornerB).magnitude;
                Logger.LogDebug($"{i} to {i + 1} mag: [{magnitude}] min [{min}]");

                if (magnitude > min)
                    continue;

                corners[i + 1] = Vector3.Lerp(cornerA, cornerB, 0.5f);
                corners.RemoveAt(i);

                stringBuilder.AppendLine($"Corner [{i + 1}] replaced. Removed [{i}] because Magnitude [{magnitude}] with min [{min}]");
                removed++;
            }

            if (removed > 0)
            {
                stringBuilder.AppendLine($"Finished Clearing Short Corners. Removed [{removed}] corners from [{count}]");
                Logger.LogDebug(stringBuilder.ToString());
            }

            yield return null;
        }

        private IEnumerator findBlindCorner(List<Vector3> corners, Vector3 lookPoint, float height)
        {
            Vector3? result = null;
            Vector3? notVisCorner = null;
            int count = corners.Count;
            for (int i = 1; i < count; i++)
            {
                Vector3 target = corners[i];
                target.y += height;
                Vector3 direction = target - lookPoint;
                
                if (Physics.Raycast(lookPoint, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    result = corners[i - 1];
                    notVisCorner = corners[i];
                    DebugGizmos.Line(target, lookPoint, Color.red, 0.05f, true, 5f, true);
                    break;
                }
                DebugGizmos.Line(target, lookPoint, Color.white, 0.05f, true, 5f, true);
                yield return null;
            }
            _blindCornerGround = result ?? corners[1];
            _cornerNotVisible = notVisCorner ?? corners[corners.Count - 1];
        }

        private IEnumerator findRealCorner(Vector3 blindCorner, Vector3 notVisibleCorner, Vector3 lookPoint, float height, int iterations = 45)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Finding Real Blind Corner for [{Bot.name}]...");

            blindCorner.y += height;

            BlindCorner = blindCorner;
            float sign = Vector.FindFlatSignedAngle(blindCorner, notVisibleCorner, lookPoint);
            float angle = sign <= 0 ? -20f : 20f;
            float rotationStep = angle / iterations;

            stringBuilder.AppendLine($"Angle to check [{angle}] Step Angle [{rotationStep}]");

            Vector3 directionToBlind = blindCorner - lookPoint;

            for (int i = 0; i < iterations; i++)
            {
                directionToBlind = Vector.Rotate(directionToBlind, 0, rotationStep, 0);
                if (!Physics.Raycast(lookPoint, directionToBlind, directionToBlind.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    BlindCorner = lookPoint + directionToBlind;
                }
                else
                {
                    stringBuilder.AppendLine($"Angle where LOS broken [{rotationStep * i}] after [{i}] iterations");
                    break;
                }

                yield return null;
            }

            stringBuilder.AppendLine("Finished Checking for real Blind Corner");
            Logger.LogAndNotifyDebug(stringBuilder.ToString());
        }

        public Vector3? BlindCorner { get; set; }
        private Vector3 _blindCornerGround;
        private Vector3 _cornerNotVisible;
        private readonly List<Vector3> _corners = new List<Vector3>();
    }
}