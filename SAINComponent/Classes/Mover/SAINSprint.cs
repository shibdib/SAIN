using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
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
    public enum RunStatus
    {
        None = 0,
        FirstTurn = 1,
        Running = 2,
        Turning = 3,
        NoStamina = 4,
        InteractingWithDoor = 5,
        ArrivingAtDestination = 6,
        CantSprint = 7,
    }

    public class SAINSprint : SAINBase, ISAINClass
    {
        static SAINSprint()
        {
        }

        public SAINSprint(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }


        public void Update()
        {
        }

        public bool Running => _runToPointCoroutine != null;

        public void CancelRun()
        {
            if (Running)
            {
                SAINBot.StopCoroutine(_runToPointCoroutine);
                _runToPointCoroutine = null;
                SAINBot.Mover.Sprint(false);
            }
        }

        public bool RunToPoint(Vector3 point, ESprintUrgency urgency, bool checkSameWay = true, System.Action callback = null)
        {
            if (checkSameWay && 
                Running && 
                (LastRunDestination - point).sqrMagnitude < 0.1f)
            {
                return true;
            }

            if (SAINBot.Mover.CanGoToPoint(point, out NavMeshPath path))
            {
                LastRunDestination = point; 
                CancelRun();
                CurrentPath = path;
                _lastUrgency = urgency;
                _runToPointCoroutine = SAINBot.StartCoroutine(RunToPoint(path, urgency, callback));
                return Running;
            }
            return false;
        }

        private ESprintUrgency _lastUrgency;

        public NavMeshPath CurrentPath;

        public bool RecalcPath()
        {
            return RunToPoint(LastRunDestination, _lastUrgency, false);
        }

        public Vector3 LastRunDestination { get; private set; }

        private Coroutine _runToPointCoroutine;

        public RunStatus CurrentRunStatus { get; private set; }

        public Vector3 currentCorner()
        {
            if (_currentPath == null)
            {
                return Vector3.zero;
            }
            return _currentPath.corners[_currentIndex];
        }

        private int _currentIndex = 0;
        private NavMeshPath _currentPath;

        private IEnumerator RunToPoint(NavMeshPath path, ESprintUrgency urgency, System.Action callback = null)
        {
            _currentPath = path;

            BotOwner.Mover.Stop();
            BotOwner.Mover.IsMoving = true;
            float startTime = Time.time;
            _currentIndex = 1;

            // First step, look towards the path we want to run
            yield return firstTurn(path.corners[1]);

            // Start running!
            yield return runPath(path, urgency);

            callback?.Invoke();

            CurrentRunStatus = RunStatus.None;
        }

        private Vector3 moveToNextCorner(NavMeshPath path)
        {
            if (totalCorners() > _currentIndex)
            {
                _currentIndex++;
            }
            return currentCorner();
        }

        private Vector3? getNextCorner(NavMeshPath path)
        {
            if (totalCorners() > _currentIndex)
            {
                return _currentPath.corners[_currentIndex + 1];
            }
            return null;
        }

        private int totalCorners()
        {
            return _currentPath != null ? _currentPath.corners.Length - 1 : 0;
        }

        private bool onLastCorner()
        {
            return _currentPath != null ? totalCorners() <= _currentIndex : false;
        }

        private static GlobalMoveSettings _moveSettings => SAINPlugin.LoadedPreset.GlobalSettings.Move;

        private IEnumerator runPath(NavMeshPath path, ESprintUrgency urgency)
        {
            for (int i = _currentIndex; i <= totalCorners(); i++)
            {
                float cornerTime = Time.time;
                // Track distance to target corner in the path.
                float distToCurrent = distanceToCurrentCornerSqr();
                while (distToCurrent > _moveSettings.BotSprintBaseReachDist)
                {
                    SAINBot.DoorOpener.Update();
                    trackMovement();

                    // Start or stop sprinting with a buffer
                    handleSprinting(distToCurrent, path, urgency);

                    if (BotOwner.DoorOpener.Interacting)
                    {
                        yield return null;
                        continue;
                    }

                    float timeSinceNoMove = timeSinceNotMoving;
                    if (timeSinceNoMove > _moveSettings.BotSprintRecalcTime)
                    {
                        RecalcPath();
                        break;
                    }
                    else if (timeSinceNoMove > _moveSettings.BotSprintTryJumpTime)
                    {
                        SAINBot.Mover.TryJump();
                    }
                    else if (timeSinceNoMove > _moveSettings.BotSprintTryVaultTime)
                    {
                        SAINBot.Mover.TryVault();
                    }

                    Vector3 destination = currentCorner();
                    float speed = IsSprintEnabled ? _moveSettings.BotSprintTurnSpeed : _moveSettings.BotSprintFirstTurnSpeed;
                    float dotProduct = steer(destination, speed);
                    move((destination - SAINBot.Position).normalized);
                    distToCurrent = distanceToCurrentCornerSqr();

                    DistanceToCurrentCorner = distToCurrent;

                    if (onLastCorner() && 
                        distToCurrent <= _moveSettings.BotSprintFinalDestReachDist)
                    {
                        yield break;
                    }

                    yield return null;
                }
                moveToNextCorner(path);
            }
        }

        public float DistanceToCurrentCorner { get; private set; }

        private bool checkMissedCorner(Vector3 currentCorner, float dotProduct)
        {
            _lastDotProduct = dotProduct;
            return false;
        }

        private float _lastDotProduct;

        private float findStartSprintStamina(ESprintUrgency urgency)
        {
            switch (urgency)
            {
                case ESprintUrgency.None:
                case ESprintUrgency.Low:
                    return 0.5f;

                case ESprintUrgency.Middle:
                    return 0.33f;

                case ESprintUrgency.High:
                    return 0.2f;

                default:
                    return 0.5f;
            }
        }

        private float findEndSprintStamina(ESprintUrgency urgency)
        {
            switch (urgency)
            {
                case ESprintUrgency.None:
                case ESprintUrgency.Low:
                    return 0.25f;

                case ESprintUrgency.Middle:
                    return 0.15f;

                case ESprintUrgency.High:
                    return 0.01f;

                default:
                    return 0.25f;
            }
        }

        private void handleSprinting(float distToCurrent, NavMeshPath path, ESprintUrgency urgency)
        {
            // I cant sprint :(
            if (!Player.MovementContext.CanSprint)
            {
                CurrentRunStatus = RunStatus.CantSprint;
                return;
            }

            bool isSprinting = Player.IsSprintEnabled;

            if (isSprinting && _moveSettings.EditSprintSpeed)
            {
                if (SAINBot.IsSpeedHacker)
                {
                    Player.MovementContext.SprintSpeed = 50f;
                }
                else
                {
                    Player.MovementContext.SprintSpeed = 2f;
                }
            }

            // Were messing with a door, dont sprint
            if (BotOwner.DoorOpener.Interacting)
            {
                CurrentRunStatus = RunStatus.InteractingWithDoor;
                SAINBot.Mover.EnableSprintPlayer(false);
                return;
            }

            // We are arriving to our destination, stop sprinting when you get close.
            if (onLastCorner() && 
                (currentCorner() - SAINBot.Position).sqrMagnitude < _moveSettings.BotSprintBufferDist)
            {
                SAINBot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = RunStatus.ArrivingAtDestination;
                return;
            }

            float staminaValue = Player.Physical.Stamina.NormalValue;

            if (isSprinting)
            {
                // We are out of stamina, stop sprinting.
                if (shallPauseSprintStamina(staminaValue, urgency))
                {
                    SAINBot.Mover.EnableSprintPlayer(false);
                    CurrentRunStatus = RunStatus.NoStamina;
                    return;
                }
                // We are approaching a sharp corner, or we are currently not looking in the direction we need to go, stop sprinting
                if (shallPauseSprintAngle(path))
                {
                    SAINBot.Mover.EnableSprintPlayer(false);
                    CurrentRunStatus = RunStatus.Turning;
                    return;
                }
            }

            // If we arne't already sprinting, and our corner were moving to is far enough away, and I have enough stamina, and the angle isn't too sharp... enable sprint
            if (!isSprinting &&
                distToCurrent > _moveSettings.BotSprintMinDist &&
                shallStartSprintStamina(staminaValue, urgency) &&
                !shallPauseSprintAngle(path))
            {
                SAINBot.Mover.EnableSprintPlayer(true);
                CurrentRunStatus = RunStatus.Running;
                return;
            }
        }

        private bool shallPauseSprintStamina(float stamina, ESprintUrgency urgency) => stamina <= findEndSprintStamina(urgency);

        private bool shallStartSprintStamina(float stamina, ESprintUrgency urgency) => stamina >= findStartSprintStamina(urgency);

        private bool shallPauseSprintAngle(NavMeshPath path)
        {
            bool shallPauseSprintForTurn = true;
            Vector3? currentCorner = this.currentCorner();
            if (currentCorner != null)
            {
                shallPauseSprintForTurn = checkShallPauseSprintFromTurn(currentCorner.Value, _moveSettings.BotSprintCurrentCornerAngleMax);
                if (!shallPauseSprintForTurn)
                {
                    Vector3? nextCorner = getNextCorner(path);
                    if (nextCorner != null && 
                        findAngle(currentCorner.Value, nextCorner.Value) < _moveSettings.BotSprintNextCornerAngleMax)
                    {
                        shallPauseSprintForTurn = false;
                        //Logger.LogDebug($"Angle To Next Corner in Sprint path: [{angle}] : Pausing Sprint? [{shallPauseSprintForTurn}] : MinAnglePause: {minAnglePause}");
                    }
                }
            }
            return shallPauseSprintForTurn;
        }

        private bool checkShallPauseSprintFromTurn(Vector3 currentCorner, float angleThresh = 25f)
        {
            return findAngleFromLook(currentCorner) >= angleThresh;
        }

        private float findAngle(Vector3 start, Vector3 end)
        {
            Vector3 origin = SAINBot.Position;
            Vector3 aDir = start - origin;
            Vector3 bDir = end - origin;
            aDir.y = 0;
            bDir.y = 0;
            return Vector3.Angle(aDir, bDir);
        }

        private float findAngleFromLook(Vector3 end)
        {
            Vector3 origin = SAINBot.Position;
            Vector3 aDir = SAINBot.LookDirection;
            Vector3 bDir = end - origin;
            aDir.y = 0;
            bDir.y = 0;
            return Vector3.Angle(aDir, bDir);
        }

        private void trackMovement()
        {
            if (nextCheckPosTime < Time.time)
            {
                nextCheckPosTime = Time.time + 0.5f;
                positionMoving = (SAINBot.Position - lastCheckPos).sqrMagnitude > _moveSettings.BotSprintNotMovingThreshold;
                if (positionMoving)
                {
                    timeNotMoving = -1f;
                    lastCheckPos = SAINBot.Position;
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
                BotOwner.AIData.SetPosToVoxel(SAINBot.Position);
            }
        }

        private float _nextCheckVoxel;

        private float distanceToCurrentCornerSqr()
        {
            Vector3 botPos = SAINBot.Position;
            if (NavMesh.SamplePosition(botPos, out var hit, 0.5f, -1))
            {
                botPos = hit.position;
            }
            Vector3 dir = currentCorner() - botPos;
            //dir.y = 0f;
            return dir.sqrMagnitude;
        }

        private IEnumerator firstTurn(Vector3 firstCorner)
        {
            CurrentRunStatus = RunStatus.FirstTurn;
            SAINBot.Mover.EnableSprintPlayer(false);

            // First step, look towards the path we want to run
            float dotProduct = 0f;
            while (dotProduct < _moveSettings.BotSprintFirstTurnDotThreshold)
            {
                BotOwner.Mover.IsMoving = true;
                Vector3 targetLookDir = firstCorner - SAINBot.Position;
                targetLookDir.y = 0f;
                dotProduct = steer(firstCorner, _moveSettings.BotSprintFirstTurnSpeed);
                if (!BotOwner.DoorOpener.Interacting)
                {
                    move(targetLookDir);
                }
                yield return null;
            }
        }

        private float steer(Vector3 target, float turnSpeed)
        {
            Vector3 playerPosition = SAINBot.Position;
            Vector3 currentLookDirNormal = SAINBot.Person.Transform.LookDirection.normalized;
            Vector3 targetLookDir = (target - playerPosition);
            Vector3 targetLookDirNormal = targetLookDir.normalized;

            if (!BotOwner.DoorOpener.Interacting)
            {
                if (shallSteerbyPriority())
                {
                    SAINBot.Steering.SteerByPriority();
                }
                else
                {
                    SAINBot.Steering.LookToDirection(targetLookDirNormal, true, turnSpeed);
                }
            }
            float dotProduct = Vector3.Dot(targetLookDirNormal, currentLookDirNormal);
            return dotProduct;
        }

        private bool shallSteerbyPriority()
        {
            switch (CurrentRunStatus)
            {
                case RunStatus.Turning:
                case RunStatus.FirstTurn:
                case RunStatus.Running:
                    return false;

                default:
                    return true;
            }
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

        public void Dispose()
        {
        }

        private bool IsSprintEnabled => Player.IsSprintEnabled;
    }

    public enum ESprintUrgency
    {
        None = 0,
        Low = 1,
        Middle = 2,
        High = 3,
    }
}