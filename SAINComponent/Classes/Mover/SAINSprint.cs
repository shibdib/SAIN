using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINSprint : SAINBase, ISAINClass
    {
        static SAINSprint()
        {
            _pathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
        }

        public SAINSprint(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            _pathController = getPathController(BotOwner.Mover);
        }

        private static PathControllerClass getPathController(BotMover mover)
        {
            return (PathControllerClass)_pathControllerField?.GetValue(mover);
        }

        public void Update()
        {
        }

        public bool Running => _runToPointCoroutine != null;

        public void CancelRun()
        {
            if (Running)
            {
                SAIN.StopCoroutine(_runToPointCoroutine);
                _runToPointCoroutine = null;
                SAIN.Mover.Sprint(false);
            }
        }

        public bool RunToPoint(Vector3 point)
        {
            if (SAIN.Mover.CanGoToPoint(point, out NavMeshPath path))
            {
                LastRunDestination = path.corners[path.corners.Length - 1];
                if (_runToPointCoroutine != null)
                {
                    SAIN.StopCoroutine(_runToPointCoroutine);
                    _runToPointCoroutine = null;
                }
                CurrentPath = path;
                _runToPointCoroutine = SAIN.StartCoroutine(RunToPoint(path));
                return _runToPointCoroutine != null;
            }
            return false;
        }

        private void checkForDoors()
        {
            Vector3 botPosition = SAIN.Position + Vector3.up;
            Vector3 lookDirection = SAIN.LookDirection;

            if (Physics.Raycast(botPosition, lookDirection, out var doorHit, 2f, LayerMaskClass.DoorLayer))
            {
                Door door = doorHit.collider?.gameObject?.GetComponent<Door>();
                if (door == null)
                {
                    door = doorHit.collider?.gameObject?.GetComponentInParent<Door>();
                }
                if (door != null)
                {
                    Logger.LogAndNotifyInfo("Found Door");
                    if (door.DoorState == EDoorState.Shut)
                    {
                        door.method_3(EDoorState.Open);
                    }
                }
                else
                {
                    Logger.LogAndNotifyInfo("No Door");
                }
            }
        }

        public bool RunToCoverPoint(CoverPoint point)
        {
            return point != null && RunToPoint(point.Position);
        }

        public NavMeshPath CurrentPath;

        public bool RecalcPath()
        {
            if (CurrentPath == null)
            {
                return false;
            }

            if (SAIN.Mover.CanGoToPoint(CurrentPath.corners[CurrentPath.corners.Length - 1], out NavMeshPath path))
            {
                LastRunDestination = path.corners[path.corners.Length - 1];
                if (_runToPointCoroutine != null)
                {
                    SAIN.StopCoroutine(_runToPointCoroutine);
                    _runToPointCoroutine = null;
                }
                CurrentPath = path;
                _runToPointCoroutine = SAIN.StartCoroutine(RunToPoint(path));
                return true;
            }
            return false;
        }

        public Vector3 LastRunDestination { get; private set; }

        private Coroutine _runToPointCoroutine;

        public enum RunStatus
        {
            None = 0,
            FirstTurn = 1,
            Running = 2,
            Turning = 3,
            NoStamina = 4,
            InteractingWithDoor = 5,
        }

        public RunStatus CurrentRunStatus { get; private set; }

        public Vector3 currentCorner()
        {
            if (_currentPath == null)
            {
                return Vector3.zero;
            }
            return _currentPath.corners[_currentIndex];
        }

        private Vector3 _nextCorner;
        private int _currentIndex = 0;
        private NavMeshPath _currentPath;

        private IEnumerator RunToPoint(NavMeshPath path)
        {
            _currentPath = path;

            BotOwner.Mover.IsMoving = true;
            float startTime = Time.time;
            _currentIndex = 1;

            // First step, look towards the path we want to run
            yield return firstTurn(path.corners[1]);

            // Start running!
            yield return runPath(path);

            OnRunComplete?.Invoke();

            CurrentRunStatus = RunStatus.None;
        }

        private Vector3 nextCorner(NavMeshPath path)
        {
            if (totalCorners() > _currentIndex)
            {
                _currentIndex++;
            }
            return currentCorner();
        }

        public Action OnRunComplete;

        private int totalCorners()
        {
            return _currentPath != null ? _currentPath.corners.Length - 1 : 0;
        }
        private bool onLastCorner()
        {
            return _currentPath != null ? totalCorners() == _currentIndex : false;
        }

        private IEnumerator runPath(NavMeshPath path)
        {
            CurrentRunStatus = RunStatus.Running;

            // config variables
            const float reachDist = 0.1f;
            const float turnSpeedSprint = 200f;
            const float turnSpeedTurning = 360f;

            for (int i = _currentIndex; i <= totalCorners(); i++)
            {
                float cornerTime = Time.time;
                // Track distance to target corner in the path.
                float distToCurrent = distanceToCurrentCornerSqr();
                while (distToCurrent > reachDist)
                {
                    SAIN.DoorOpener.Update();
                    trackMovement();

                    // Start or stop sprinting with a buffer
                    handleSprinting(distToCurrent);

                    if (BotOwner.DoorOpener.Interacting)
                    {
                        yield return null;
                        continue;
                    }

                    if (timeSinceNotMoving > 1f)
                    {
                        RecalcPath();
                        break;
                    }
                    else if (timeSinceNotMoving > 0.25f)
                    {
                        Player.MovementContext.TryVaulting();
                    }

                    steer(currentCorner(), SAIN.Mover.IsSprinting ? turnSpeedSprint : turnSpeedTurning);
                    move((currentCorner() - SAIN.Position).normalized);

                    distToCurrent = distanceToCurrentCornerSqr();
                    yield return null;
                }
                nextCorner(path);
            }
        }

        const float reachDistRunning = 1f;
        const float reachDistRunningBuffer = 1.5f;

        private void handleSprinting(float distToCurrent)
        {
            if (BotOwner.DoorOpener.Interacting)
            {
                CurrentRunStatus = RunStatus.InteractingWithDoor;
                SAIN.Mover.Sprint(false);
                return;
            }

            float staminaValue = Player.Physical.Stamina.NormalValue;
            if (staminaValue < 0.1f)
            {
                CurrentRunStatus = RunStatus.NoStamina;
                SAIN.Mover.Sprint(false);
            }
            else if (staminaValue > 0.5f)
            {
                if (distToCurrent < reachDistRunning)
                {
                    SAIN.Mover.Sprint(false);
                    CurrentRunStatus = RunStatus.Turning;
                }
                else if (distToCurrent > reachDistRunningBuffer)
                {
                    SAIN.Mover.Sprint(true);
                    CurrentRunStatus = RunStatus.Running;
                }
            }

            if (SAIN.Mover.IsSprinting)
            {
                Player.MovementContext.SprintSpeed = 2f;
            }
        }

        private void trackMovement()
        {
            if (nextCheckPosTime < Time.time)
            {
                nextCheckPosTime = Time.time + 0.5f;
                positionMoving = (SAIN.Position - lastCheckPos).sqrMagnitude > 0.05f;
                if (positionMoving)
                {
                    timeNotMoving = -1f;
                    lastCheckPos = SAIN.Position;
                }
                else if (timeNotMoving < 0)
                {
                    timeNotMoving = Time.time;
                }
            }
        }

        private bool positionMoving;
        private Vector3 lastCheckPos;
        private float nextCheckPosTime;
        private float timeSinceNotMoving => positionMoving ? 0f : Time.time - timeNotMoving;
        private float timeNotMoving;

        private void updateVoxel()
        {
            if (this._nextCheckVoxel < Time.time)
            {
                this._nextCheckVoxel = Time.time + 0.5f;
                BotOwner.AIData.SetPosToVoxel(SAIN.Position);
            }
        }

        private float _nextCheckVoxel;

        private float distanceToCurrentCornerSqr()
        {
            Vector3 dir = currentCorner() - SAIN.Position;
            //dir.y = 0f;
            return dir.sqrMagnitude;
        }

        private IEnumerator firstTurn(Vector3 firstCorner)
        {
            // config variables
            const float dotThreshold = 0.9f;
            const float firstTurnSpeed = 360f;
            const float firstTurnMoveDistThreshold = 1f;
            const float firstTurnMoveDistThresholdSQR = firstTurnMoveDistThreshold * firstTurnMoveDistThreshold;

            CurrentRunStatus = RunStatus.FirstTurn;

            SAIN.Mover.Sprint(false);

            // First step, look towards the path we want to run
            float dotProduct = 0f;
            while (dotProduct < dotThreshold)
            {
                Vector3 targetLookDir = firstCorner - SAIN.Position;
                targetLookDir.y = 0f;
                dotProduct = steer(firstCorner, firstTurnSpeed);
                if (!BotOwner.DoorOpener.Interacting)
                {
                    move(targetLookDir);
                }
                if (targetLookDir.sqrMagnitude > firstTurnMoveDistThresholdSQR)
                {
                    //move(targetLookDir);
                }
                yield return null;
            }
        }

        private float steer(Vector3 target, float turnSpeed)
        {
            Vector3 playerPosition = SAIN.Position;
            Vector3 currentLookDirNormal = SAIN.Person.Transform.LookDirection.normalized;
            Vector3 targetLookDir = (target - playerPosition);
            Vector3 targetLookDirNormal = targetLookDir.normalized;

            if (!BotOwner.DoorOpener.Interacting)
            {
                SAIN.Steering.LookToDirection(targetLookDirNormal, true, turnSpeed);
            }
            float dotProduct = Vector3.Dot(targetLookDirNormal, currentLookDirNormal);
            return dotProduct;
        }

        private void move(Vector3 direction)
        {
            //checkForDoors();
            trackMovement();
            updateVoxel();
            Player.CharacterController.SetSteerDirection(direction);
            BotOwner.AimingData?.Move(Player.Speed);
            Player.Move(findMoveDirection(direction));
        }

        public Vector2 findMoveDirection(Vector3 direction)
        {
            Vector2 v = new Vector2(direction.x, direction.z);
            Vector3 vector = Quaternion.Euler(0f, 0f, Player.Rotation.x) * v;
            vector = Helpers.Vector.NormalizeFastSelf(vector);
            return new Vector2(vector.x, vector.y);
        }

        private Coroutine trackSprintCoroutine;
        private static FieldInfo _pathControllerField;

        public void Dispose()
        {
            if (NextCornerObject != null)
            {
                GameObject.Destroy(NextCornerObject);
                NextCornerObject = null;
                DebugGizmos.DestroyLabel(NextCornerGUIObject);
                NextCornerGUIObject = null;
            }
            if (CurrentCornerObject != null)
            {
                GameObject.Destroy(CurrentCornerObject);
                CurrentCornerObject = null;
                DebugGizmos.DestroyLabel(CurrentCornerGUIObject);
                CurrentCornerGUIObject = null;
            }
        }

        private IEnumerator TrackSprint(NavMeshPath path)
        {
            while (true)
            {
                Vector3 botPosition = SAIN.Position;
                Vector3? currentCorner = CurrentCorner();

                if (currentCorner != null)
                {
                    Vector3 currentCornerDirection = (currentCorner.Value - botPosition).normalized;
                    if (CurrentCornerObject ==  null)
                    {
                        CurrentCornerObject = DebugGizmos.Line(botPosition, currentCorner.Value, 0.1f, -1f);
                        CurrentCornerGUIObject = new GUIObject
                        {
                            WorldPos = currentCorner.Value,
                            Text = $"Current Corner",
                        };
                        DebugGizmos.AddGUIObject(CurrentCornerGUIObject);
                    }
                    else
                    {
                        DebugGizmos.UpdatePositionLine(botPosition, currentCorner.Value, CurrentCornerObject);
                        CurrentCornerGUIObject.WorldPos = currentCorner.Value;
                    }

                    Vector3? nextCorner = NextCorner();

                    if (nextCorner != null)
                    {
                        Vector3 nextCornerDirection = (nextCorner.Value - currentCorner.Value).normalized;
                        currentCornerDirection.y = 0;
                        nextCornerDirection.y = 0;
                        float angle = Vector3.Angle(nextCornerDirection, currentCornerDirection);
                        if (CurrentCornerGUIObject != null)
                        {
                            CurrentCornerGUIObject.Text = $"Current Corner. Angle To Next: [{angle}]";
                        }
                        if (NextCornerObject == null)
                        {
                            NextCornerObject = DebugGizmos.Line(currentCorner.Value, nextCorner.Value, 0.1f, -1f);
                            NextCornerGUIObject = new GUIObject
                            {
                                WorldPos = nextCorner.Value,
                                Text = $"Next Corner",
                            };
                            DebugGizmos.AddGUIObject(NextCornerGUIObject);
                        }
                        else
                        {
                            DebugGizmos.UpdatePositionLine(currentCorner.Value, nextCorner.Value, NextCornerObject);
                            NextCornerGUIObject.WorldPos = nextCorner.Value;
                        }
                    }
                    else if (NextCornerObject != null)
                    {
                        GameObject.Destroy(NextCornerObject);
                        NextCornerObject = null;
                        DebugGizmos.DestroyLabel(NextCornerGUIObject);
                        NextCornerGUIObject = null;
                    }
                }
                else
                {
                    // We have no current corner, and no next corner, destroy both
                    if (CurrentCornerObject != null)
                    {
                        GameObject.Destroy(CurrentCornerObject);
                        CurrentCornerObject = null;
                        DebugGizmos.DestroyLabel(CurrentCornerGUIObject);
                        CurrentCornerGUIObject = null;
                    }
                    if (NextCornerObject != null)
                    {
                        GameObject.Destroy(NextCornerObject);
                        NextCornerObject = null;
                        DebugGizmos.DestroyLabel(NextCornerGUIObject);
                        NextCornerGUIObject = null;
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        private GUIObject CurrentCornerGUIObject;
        private GameObject CurrentCornerObject;
        private GUIObject NextCornerGUIObject;
        private GameObject NextCornerObject;

        private bool IsSprintEnabled => Player.IsSprintEnabled;

        private Vector3? CurrentCorner()
        {
            return _pathController?.CurPath?.CurrentCorner();
        }

        private Vector3? NextCorner()
        {
            if (_pathController.CurPath != null)
            {
                int i = _pathController.CurPath.CurIndex;
                int max = _pathController.CurPath.Length - 1;
                if (i < max)
                {
                    return _pathController.CurPath.GetPoint(i + 1);
                }
            }
            return null;
        }
        private PathControllerClass _pathController;
    }
}