using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Debug
{
    public class SAINBotUnstuckClass : SAINBase, ISAINClass
    {
        static SAINBotUnstuckClass()
        {
            _pathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
        }

        private static readonly FieldInfo _pathControllerField;

        public SAINBotUnstuckClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            PathController = _pathControllerField.GetValue(BotOwner.Mover) as PathControllerClass;
            DontUnstuckMe = DontUnstuckTheseTypes.Contains(SAIN.Info.Profile.WildSpawnType);
        }

        public bool BotIsMoving { get; private set; }
        private bool _botIsMoving;

        public float TimeStartedMoving { get; private set; }
        public float TimeStoppedMoving { get; private set; }

        private float _timeStartMoving;

        private float _timeStopMoving;
        public float TimeSinceMovingStarted => TimeStartedMoving - Time.time;

        private void CheckIfMoving()
        {
            float time = Time.time;

            // Check if a bot is being told to move by the botowner pathfinder, and record the time this starts and stops
            bool botWasMoving = _botIsMoving;
            _botIsMoving = BotOwner.Mover.IsMoving;
            if (_botIsMoving && !botWasMoving)
            {
                _timeStartMoving = time;
            }
            else if (!_botIsMoving && botWasMoving)
            {
                _timeStopMoving = time;
            }

            // How long a bot must be "moving" for before we check
            const float movingForPeriod = 0.25f;

            // Has the bot been told to move for over x seconds? then record the state for use in other classes
            if (_botIsMoving && time - _timeStartMoving > movingForPeriod)
            {
                if (!BotIsMoving)
                {
                    TimeStartedMoving = time;
                }
                BotIsMoving = true;
            }
            else if (!_botIsMoving && time - _timeStopMoving > movingForPeriod)
            {
                if (BotIsMoving)
                {
                    TimeStoppedMoving = time;
                }
                BotIsMoving = false;
            }
        }

        private bool FixOffMeshBot()
        {
            if (_nextCheckNavMeshTime < Time.time)
            {
                _nextCheckNavMeshTime = Time.time + 1f;
                bool wasOnNavMesh = _isOnNavMesh;
                _isOnNavMesh = CheckBotIsOnNavMesh();

                if (!_isOnNavMesh)
                {
                    if (wasOnNavMesh)
                    {
                        _timeOffMeshStart = Time.time;
                    }
                    if (Time.time - _timeOffMeshStart > 3f && _nextResetTime < Time.time)
                    {
                        _nextResetTime = Time.time + 5f;
                        Logger.LogWarning($"{BotOwner.name} is off navmesh! Trying to fix...");
                        SAIN.Mover.ResetPath(0.33f);
                    }
                }
                if (_isOnNavMesh)
                {
                    if (_timeOffMeshStart > 0)
                    {
                        _timeOffMeshStart = -1f;
                    }
                }
            }
            return _isOnNavMesh;
        }

        private float _timeOffMeshStart;
        private float _nextResetTime;

        public Vector2 findMoveDirection(Vector3 direction)
        {
            Vector2 v = new Vector2(direction.x, direction.z);
            Vector3 vector = Quaternion.Euler(0f, 0f, Player.Rotation.x) * v;
            vector = Helpers.Vector.NormalizeFastSelf(vector);
            return new Vector2(vector.x, vector.y);
        }

        private Coroutine FindPathCoroutine;

        private IEnumerator TrackPosition()
        {
            var wait = new WaitForSeconds(0.5f);
            while (BotOwner != null)
            {
                _isOnNavMesh = CheckBotIsOnNavMesh();
                if (_isOnNavMesh)
                {
                    Vector3 newPos = SAIN.Position;
                    int count = _positions.Count;
                    if (count > 0)
                    {
                        Vector3 last = _positions[count - 1];
                        if ((newPos - last).sqrMagnitude > 2f)
                        {
                            count++;
                            _positions.Add(newPos);
                        }
                    }
                }
                if (_positions.Count > _positions.Capacity)
                {
                    _positions.RemoveAt(0);
                }
                yield return wait;
            }
        }

        private readonly List<Vector3> _positions = new List<Vector3>(10);

        public void AddPreVaultPos(Vector3 pos)
        {
            if (PreVaultPositions.Count >= PreVaultPositions.Capacity)
            {
                PreVaultPositions.RemoveAt(0);
            }
            PreVaultPositions.Add(pos);
        }
        public readonly List<Vector3> PreVaultPositions = new List<Vector3>(10);

        private IEnumerator FindPathBackToNavMesh()
        {
            while (!_isOnNavMesh && BotOwner != null && BotOwner.Mover != null)
            {
                Vector3 currentPosition = SAIN.Position;
                Vector3 lookDirection = SAIN.Transform.LookDirection;
                Vector3 headPosition = SAIN.Transform.Head;

                const int max = 40;
                const float rotationAngle = 360f / (float)max;

                Vector3 pointToLook = Vector3.zero;
                float furthestHitDist = 0f;

                int count = 0;
                // Find direction to look for navmesh
                for (int i = 0; i < max; i++)
                {
                    count++;

                    float rotation = rotationAngle * (i + 1);
                    Quaternion rotate = Quaternion.Euler(0, rotation, 0f);
                    Vector3 direction = rotate * lookDirection;

                    if (!Physics.SphereCast(headPosition, 0.05f, direction, out RaycastHit hit, 5f, -1))
                    {
                        pointToLook = headPosition + direction;
                        break;
                    }
                    else
                    {
                        float sqrMag = (hit.point - headPosition).sqrMagnitude;
                        if (sqrMag > furthestHitDist)
                        {
                            furthestHitDist = sqrMag;
                            pointToLook = hit.point;
                        }
                    }

                    if (count >= 5)
                    {
                        count = 0;
                        yield return null;
                    }
                }

                yield return null;

                Vector3 navHitPoint = Vector3.zero;
                for (int i = 0; i < 5; i++)
                {
                    float range = 0.5f * (i + 1);
                    if (NavMesh.SamplePosition(pointToLook, out NavMeshHit hit, range, -1))
                    {
                        navHitPoint = hit.position;
                        break;
                    }
                    yield return null;
                }

                if (navHitPoint != Vector3.zero)
                {
                    DebugGizmos.Line(SAIN.Position, navHitPoint);
                    Vector3 closestEdgePoint = Vector3.zero;
                    if (NavMesh.FindClosestEdge(navHitPoint, out NavMeshHit edge, -1))
                    {
                        closestEdgePoint = edge.position;

                        BotOwner.MovementPause(5f);
                        DebugGizmos.Line(SAIN.Position, closestEdgePoint);

                        float startMoveTime = 0f;
                        while (startMoveTime < 5f)
                        {
                            startMoveTime += Time.deltaTime;

                            //Player.Move(findMoveDirection((Singleton<GameWorld>.Instance.MainPlayer.Position - currentPosition).normalized));
                            Player.Move(findMoveDirection((closestEdgePoint - currentPosition).normalized));

                            _isOnNavMesh = CheckBotIsOnNavMesh();
                            if (_isOnNavMesh)
                            {
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }
            }
        }

        private bool _isOnNavMesh;
        private float _nextCheckNavMeshTime;

        private bool CheckBotIsOnNavMesh()
        {
            return NavMesh.SamplePosition(SAIN.Position, out _, 0.15f, -1);
        }

        private void CheckRecalcPath()
        {
            if (BotIsMoving 
                && Time.time - TimeStartedMoving > 0.5f 
                && _recalcPathVault < Time.time)
            {
                Vector3 lookDir = Player.LookDirection.normalized;
                Vector3 targetDir = BotOwner.Mover.NormDirCurPoint;
                if (Vector3.Dot(lookDir, targetDir) > 0.75f)
                {
                    if (tryVault())
                    {
                        _botVaulted = true;
                        _recalcPathVault = Time.time + 2f;
                        return;
                    }
                }
                _recalcPathVault = Time.time + 1f;
            }

            if (_recalcPathTimer < Time.time 
                && BotIsMoving 
                && !BotHasChangedPosition 
                && Time.time - TimeStartedMoving > 1f)
            {
                _recalcPathTimer = Time.time + 2f;
                SAIN.Mover.ResetPath(0.33f);
            }
        }

        private float _recalcPathVault;
        private float _recalcPathTimer;

        private void CheckIfPositionChanged()
        {
            if (CheckPositionTimer < Time.time)
            {
                CheckPositionTimer = Time.time + 0.5f;

                bool botChangedPositionLast = BotHasChangedPosition;

                const float DistThreshold = 0.1f;
                BotHasChangedPosition = (LastPos - SAIN.Position).sqrMagnitude > DistThreshold * DistThreshold;

                if (botChangedPositionLast && !BotHasChangedPosition)
                {
                    TimeStartedChangingPosition = Time.time;
                }
                else if (BotHasChangedPosition)
                {
                    TimeStartedChangingPosition = 0f;
                }

                LastPos = SAIN.Position;
            }
        }

        private IEnumerator trackPostVault()
        {
            Vector3? currentPathDestination = null;
            if (BotOwner.Mover.HavePath)
            {
                currentPathDestination = BotOwner?.Mover?.LastDestination();
            }

            yield return new WaitForSeconds(1f);

            if (SAIN == null || BotOwner == null || Player == null || Player.HealthController.IsAlive == false)
            {
                yield break;
            }

            NavMeshPath path = new NavMeshPath();
            _botStuckAfterVault = !NavMesh.CalculatePath(SAIN.Position, preVaultPosition, -1, path) || path.status != NavMeshPathStatus.PathComplete;
            if (_botStuckAfterVault)
            {
                Logger.LogWarning($"{BotOwner.name} has vaulted to somewhere they can't get down from! Trying to fix...");
            }
        }

        private bool _botStuckAfterVault;

        private Coroutine postVaultTracker;

        private bool tryVault()
        {
            Vector3 currentPos = SAIN.Position;
            if (Player.MovementContext.TryVaulting())
            {
                AddPreVaultPos(currentPos);
                preVaultPosition = currentPos;
                if (postVaultTracker != null)
                {
                    SAIN.StopCoroutine(postVaultTracker);
                }
                postVaultTracker = SAIN.StartCoroutine(trackPostVault());
                return true;
            }
            return false;
        }

        private Vector3 preVaultPosition;

        private float _nextVaultCheckTime;
        private bool DontUnstuckMe;

        private static readonly List<WildSpawnType> DontUnstuckTheseTypes = new List<WildSpawnType>
        {
            WildSpawnType.marksman,
            WildSpawnType.shooterBTR,
        };

        private void CheckBotVaulted()
        {
            if (_botVaulted)
            {
                _botVaulted = false;
                _botVaultedTime = Time.time;
            }

            if (_botVaultedTime != -1f 
                && _botVaultedTime + 1f < Time.time)
            {
                _botVaultedTime = -1f;
                SAIN.Mover.ResetPath(0.1f);
                //BotOwner.Mover.RecalcWay();
            }
        }

        private bool _botVaulted;
        private float _botVaultedTime;

        public void Update()
        {
            if (SAIN.BotActive
                && !SAIN.GameIsEnding)
            {
                if (DontUnstuckMe)
                {
                    return;
                }

                if (!FixOffMeshBot())
                {
                    return;
                }

                if (_nextVaultCheckTime < Time.time 
                    && BotOwner.Mover.IsMoving 
                    && TimeStartedMoving + 1f < Time.time)
                {
                    _nextVaultCheckTime = Time.time + 0.5f;

                    Vector3 lookDir = Player.LookDirection.normalized;
                    Vector3 targetDir = BotOwner.Mover.NormDirCurPoint;
                    if (Vector3.Dot(lookDir, targetDir) > 0.75f)
                    {
                        if (tryVault())
                        {
                            _botVaulted = true;
                            _nextVaultCheckTime = Time.time + 2f;
                        }
                    }
                }

                CheckIfMoving();
                CheckIfPositionChanged();
                CheckRecalcPath(); 
                CheckBotVaulted();

                if (CheckStuckTimer < Time.time)
                {
                    if (BotOwner.DoorOpener.Interacting)
                    {
                        CheckStuckTimer = Time.time + 1f;
                        BotIsStuck = false;
                    }
                    else
                    {
                        CheckStuckTimer = Time.time + 0.5f;

                        bool stuck = BotStuckGeneric()
                            || BotStuckOnObject()
                            || BotStuckOnPlayer();

                        if (!BotIsStuck && stuck)
                        {
                            TimeStuck = Time.time;
                        }
                        BotIsStuck = stuck;
                    }

                    // If the bot is no longer stuck, but we are checking if we can teleport them, cancel the coroutine
                    if (!BotIsStuck
                        && TeleportCoroutineStarted
                        && TeleportCoroutine != null)
                    {
                        SAIN.StopCoroutine(TeleportCoroutine);
                        TeleportCoroutineStarted = false;
                        HasTriedJumpOrVault = false;
                        JumpTimer = Time.time + 1f;
                        IsTeleporting = false;
                    }

                    if (BotIsStuck && TimeSinceStuck > 2f)
                    {
                        if (DebugStuckTimer < Time.time && TimeSinceStuck > 5f)
                        {
                            DebugStuckTimer = Time.time + 5f;
                            Logger.LogWarning($"[{BotOwner.name}] has been stuck for [{TimeSinceStuck}] seconds " +
                                $"on [{StuckHit.transform?.name}] object " +
                                $"at [{StuckHit.transform?.position}] " +
                                $"with Current Decision as [{SAIN.Memory.Decisions.Main.Current}]");
                        }

                        if (HasTriedJumpOrVault
                            && TimeSinceStuck > 10f
                            && TimeSinceTriedJumpOrVault + 2f < Time.time
                            && !TeleportCoroutineStarted)
                        {
                            TeleportCoroutineStarted = true;
                            TeleportCoroutine = SAIN.StartCoroutine(CheckIfTeleport());
                        }

                        if (JumpTimer < Time.time && TimeSinceStuck > 2f)
                        {
                            JumpTimer = Time.time + 1f;

                            if (!tryVault())
                            {
                                HasTriedJumpOrVault = true;
                                TimeSinceTriedJumpOrVault = Time.time;
                            }
                            else
                            {
                                _botVaulted = true;
                            }
                        }
                    }
                }
            }
        }

        private float TimeSinceTriedJumpOrVault;
        private bool HasTriedJumpOrVault;
        private const float MinDistance = 100f;
        private const float MaxDistance = 300f;
        private const float PathLengthCoef = 1.25f;
        private const float MinDistancePathLength = MinDistance * PathLengthCoef;

        private bool TeleportCoroutineStarted;
        private Coroutine TeleportCoroutine;

        private IEnumerator CheckIfTeleport()
        {
            bool shallTeleport = true;
            var humanPlayers = GetHumanPlayers();

            Vector3? teleportDestination = null;

            const float minTeleDist = 1f;
            if (BotOwner.Mover.HavePath)
            {
                for (int i = PathController.CurPath.CurIndex; i < PathController.CurPath.Length - 1; i++)
                {
                    Vector3 corner = PathController.CurPath.GetPoint(i);
                    Vector3 cornerDirection = corner - SAIN.Position;
                    float cornerDistance = cornerDirection.sqrMagnitude;
                    if (cornerDirection.sqrMagnitude >= minTeleDist * minTeleDist)
                    {
                        teleportDestination = new Vector3?(corner);
                        break;
                    }
                    yield return null;
                }
            }

            Vector3 botPosition = SAIN.Position;

            if (teleportDestination != null)
            {
                var allPlayers = Singleton<GameWorld>.Instance?.AllAlivePlayersList;
                if (allPlayers != null)
                {
                    foreach (var player in allPlayers)
                    {
                        if (ShallCheckPlayer(player))
                        {
                            if (!BotIsStuck)
                            {
                                shallTeleport = false;
                                yield break;
                            }

                            // Make sure the player isn't visible to the bot
                            if (SAIN.Memory.VisiblePlayers.Contains(player))
                            {
                                shallTeleport = false;
                                break;
                            }

                            Vector3 playerPosition = player.Position;

                            // Makes sure the bot isn't too close to a human for them to hear
                            float sqrMag = (playerPosition - botPosition).sqrMagnitude;
                            if (sqrMag < MinDistance * MinDistance)
                            {
                                shallTeleport = false;
                                break;
                            }

                            // Checks the max distance to do a path calculation
                            if (sqrMag < MaxDistance * MaxDistance)
                            {
                                NavMeshPath path = CalcPath(botPosition, playerPosition, out float pathLength);
                                if (CheckPathLength(playerPosition, path, pathLength) == false)
                                {
                                    shallTeleport = false;
                                    break;
                                }
                            }

                            // Check next player on the next frame
                            yield return null;
                        }
                    }
                }
            }

            IsTeleporting = BotIsStuck && shallTeleport && teleportDestination != null;

            if (IsTeleporting)
            {
                Teleport(teleportDestination.Value + Vector3.up * 0.25f);
                float distance = (teleportDestination.Value - botPosition).magnitude;
                Logger.LogDebug($"Teleporting stuck bot: [{Player.name}] [{distance}] meters to the next corner they are trying to go to");
            }

            yield return null;
        }

        private bool IsTeleporting;

        private bool CheckVisibilityToAllPlayers(Vector3 point)
        {
            var allPlayers = Singleton<GameWorld>.Instance?.AllAlivePlayersList;
            if (allPlayers == null)
            {
                return false;
            }

            Vector3 testPoint = point + Vector3.up;

            foreach (var player in allPlayers)
            {
                if (ShallCheckPlayer(player))
                {

                }
            }
            return false;
        }

        private bool ShallCheckPlayer(Player player)
        {
            if (Player == null || Player.HealthController == null || Player.AIData == null)
            {
                return false;
            }
            return Player.HealthController.IsAlive == true && Player.AIData.IsAI == false;
        }

        private void Teleport(Vector3 position)
        {
            if (teleportTimer < Time.time)
            {
                teleportTimer = Time.time + 3f;
                Player.Teleport(position);
            }
        }

        private float teleportTimer;

        public PathControllerClass PathController { get; private set; }

        private static NavMeshPath CalcPath(Vector3 start, Vector3 end, out float pathLength)
        {
            if (PathToPlayer == null)
            {
                PathToPlayer = new NavMeshPath();
            }
            if (NavMesh.SamplePosition(end, out NavMeshHit hit, 1f, -1))
            {
                PathToPlayer.ClearCorners();
                if (NavMesh.CalculatePath(start, hit.position, -1, PathToPlayer))
                {
                    pathLength = PathToPlayer.CalculatePathLength();
                    return PathToPlayer;
                }
            }
            pathLength = 0f;
            return null;
        }

        private static bool CheckPathLength(Vector3 end, NavMeshPath path, float pathLength)
        {
            if (path == null)
            {
                return false;
            }
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                Vector3 lastCorner = path.corners[path.corners.Length - 1];
                float sqrMag = (lastCorner - end).magnitude;
                float combinedLength = sqrMag + pathLength;
                if (combinedLength < MinDistancePathLength)
                {
                    return false;
                }
            }
            if (path.status == NavMeshPathStatus.PathComplete && pathLength < MinDistancePathLength)
            {
                return false;
            }
            return path.status != NavMeshPathStatus.PathInvalid;
        }

        private List<Player> GetHumanPlayers()
        {
            HumanPlayers.Clear();
            var allPlayers = Singleton<GameWorld>.Instance?.AllAlivePlayersList;
            if (allPlayers != null)
            {
                foreach (var player in allPlayers)
                {
                    if (player != null && player.AIData.IsAI == false && player.HealthController.IsAlive)
                    {
                        HumanPlayers.Add(player);
                    }
                }
            }
            return HumanPlayers;
        }

        private static NavMeshPath PathToPlayer;
        private List<Player> HumanPlayers = new List<Player>();

        public void Dispose()
        {
        }

        private RaycastHit StuckHit = new RaycastHit();
        private float DebugStuckTimer = 0f;
        private float CheckStuckTimer = 0f;
        public float TimeSinceStuck => Time.time - TimeStuck;
        public float TimeStuck { get; private set; }

        private float CheckPositionTimer = 0f;

        private Vector3 LastPos = Vector3.zero;

        public float TimeSpentNotMoving => Time.time - TimeStartedChangingPosition;

        public float TimeStartedChangingPosition { get; private set; }

        public bool BotIsStuck { get; private set; }

        private bool CanBeStuckDecisions(SoloDecision decision)
        {
            return decision == SoloDecision.Search || decision == SoloDecision.MoveToCover || decision == SoloDecision.DogFight || decision == SoloDecision.RunToCover || decision == SoloDecision.RunAway || decision == SoloDecision.UnstuckSearch || decision == SoloDecision.UnstuckDogFight || decision == SoloDecision.UnstuckMoveToCover;
        }

        public bool BotStuckOnPlayer()
        {
            var decision = SAIN.Memory.Decisions.Main.Current;
            if (!BotHasChangedPosition && CanBeStuckDecisions(decision))
            {
                if (BotOwner.Mover == null)
                {
                    return false;
                }
                Vector3 botPos = BotOwner.Position;
                botPos.y += 0.4f;
                Vector3 moveDir = BotOwner.Mover.DirCurPoint;
                moveDir.y = 0;
                Vector3 lookDir = BotOwner.LookDirection;
                lookDir.y = 0;

                var moveHits = Physics.SphereCastAll(botPos, 0.15f, moveDir, 0.5f, LayerMaskClass.PlayerMask);
                if (moveHits.Length > 0)
                {
                    foreach (var move in moveHits)
                    {
                        if (move.transform.name != BotOwner.name)
                        {
                            StuckHit = move;
                            return true;
                        }
                    }
                }

                var lookHits = Physics.SphereCastAll(botPos, 0.15f, lookDir, 0.5f, LayerMaskClass.PlayerMask);
                if (lookHits.Length > 0)
                {
                    foreach (var look in lookHits)
                    {
                        if (look.transform.name != BotOwner.name)
                        {
                            StuckHit = look;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool BotStuckGeneric()
        {
            return BotIsMoving && !BotHasChangedPosition && !BotOwner.DoorOpener.Interacting && TimeSpentNotMoving > 2f;
        }

        public bool BotStuckOnObject()
        {
            if (CanBeStuckDecisions(SAIN.Memory.Decisions.Main.Current) && !BotHasChangedPosition && !BotOwner.DoorOpener.Interacting && SAIN.Decision.TimeSinceChangeDecision > 1f)
            {
                if (BotOwner.Mover == null)
                {
                    return false;
                }
                Vector3 botPos = BotOwner.Position;
                botPos.y += 0.4f;
                Vector3 moveDir = BotOwner.Mover.DirCurPoint;
                moveDir.y = 0;
                if (Physics.SphereCast(botPos, 0.15f, moveDir, out var hit, 0.25f, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    StuckHit = hit;
                    return true;
                }
            }
            return false;
        }

        public bool BotHasChangedPosition { get; private set; }

        private float JumpTimer = 0f;
    }
}