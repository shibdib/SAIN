using EFT;
using SAIN.Preset.GlobalSettings.Categories;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class ObstacleAgent : MonoBehaviour
    {
        private float CarvingTime = 0.5f;
        private float CarvingMoveThreshold = 0.1f;

        private NavMeshAgent Agent;
        private NavMeshObstacle Obstacle;

        private float LastMoveTime;
        private Vector3 LastPosition;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Obstacle = GetComponent<NavMeshObstacle>();

            Obstacle.enabled = false;
            Obstacle.carveOnlyStationary = false;
            Obstacle.carving = true;

            LastPosition = transform.position;
        }

        private void Update()
        {
            if (Vector3.Distance(LastPosition, transform.position) > CarvingMoveThreshold)
            {
                LastMoveTime = Time.time;
                LastPosition = transform.position;
            }
            if (LastMoveTime + CarvingTime < Time.time)
            {
                Agent.enabled = false;
                Obstacle.enabled = true;
            }
        }

        public void SetDestination(Vector3 Position)
        {
            Obstacle.enabled = false;

            LastMoveTime = Time.time;
            LastPosition = transform.position;

            StartCoroutine(MoveAgent(Position));
        }

        private IEnumerator MoveAgent(Vector3 Position)
        {
            yield return null;
            Agent.enabled = true;
            Agent.SetDestination(Position);
        }
    }

    public class SAINMoverClass : SAINBase, ISAINClass
    {
        public SAINMoverClass(BotComponent sain) : base(sain)
        {
            BlindFire = new BlindFireController(sain);
            SideStep = new SideStepClass(sain);
            Lean = new LeanClass(sain);
            Prone = new ProneClass(sain);
            Pose = new PoseClass(sain);
            SprintController = new SAINSprint(sain);
            DogFight = new DogFight(sain);
        }

        public DogFight DogFight { get; private set; }
        public SAINSprint SprintController { get; private set; }

        public void Init()
        {
            UpdateBodyNavObstacle(false);
        }

        public void UpdateBodyNavObstacle(bool value)
        {
            if (BotBodyObstacle == null)
            {
                //BotBodyObstacle = SAIN.GetOrAddComponent<NavMeshObstacle>();
                if (BotBodyObstacle == null)
                {
                    //Logger.LogError($"Bot Body Navmesh obstacle is null for [{SAIN.BotOwner.name}]");
                    return;
                }
                //BotBodyObstacle.radius = 0.25f;
                //BotBodyObstacle.shape = NavMeshObstacleShape.Capsule;
                //BotBodyObstacle.carveOnlyStationary = false;
            }
            //BotBodyObstacle.enabled = false;
            //BotBodyObstacle.carving = value;
        }

        public void Update()
        {
            SetStamina();

            Pose.Update();
            Lean.Update();
            //SideStep.Update();
            Prone.Update();
            BlindFire.Update();
            SprintController.Update();
        }

        public void Dispose()
        {
            SprintController?.Dispose();
        }

        public BlindFireController BlindFire { get; private set; }
        public SideStepClass SideStep { get; private set; }
        public LeanClass Lean { get; private set; }
        public PoseClass Pose { get; private set; }
        public ProneClass Prone { get; private set; }

        public NavMeshObstacle BotBodyObstacle { get; private set; }

        public bool GoToPoint(Vector3 point, out bool calculating, float reachDist = -1f, bool crawl = false, bool slowAtEnd = true, bool mustHaveCompletePath = false)
        {
            calculating = false;
            if (reachDist < 0f)
            {
                reachDist = SAINPlugin.LoadedPreset.GlobalSettings.General.BaseReachDistance;
            }
            CurrentPathStatus = BotOwner.Mover.GoToPoint(point, slowAtEnd, reachDist, false, mustHaveCompletePath, true);
            if (CurrentPathStatus != NavMeshPathStatus.PathInvalid)
            {
                if (crawl)
                {
                    Prone.SetProne(true);
                }
                SAINBot.DoorOpener.Update();
                return true;
            }
            return CurrentPathStatus != NavMeshPathStatus.PathInvalid;
        }

        public bool GoToPoint2(Vector3 point, bool mustHaveCompletePath = false, bool crawl = false)
        {
            if (CanGoToPoint(point, out NavMeshPath path, mustHaveCompletePath))
            {
                BotOwner.Mover.GoToByWay(path.corners, 1f);
                if (crawl)
                {
                    Prone.SetProne(true);
                }
                SAINBot.DoorOpener.Update();
                return true;
            }
            return false;
        }

        public bool GoToPoint3(Vector3 point, out bool calculating, float reachDist = -1f, bool crawl = false, bool slowAtEnd = true)
        {
            if (GoToPointCoroutine != null && _coroutineRunning)
            {
                calculating = true;
                return false;
            }

            if (GoToPointCoroutine != null)
            {
                SAINBot.StopCoroutine(GoToPointCoroutine);
                GoToPointCoroutine = null;
            }

            //BotBodyObstacle.enabled = false;

            //LastMoveTime = Time.time;
            //LastPosition = SAIN.Position;
            if (CanGoToPoint(point, out Vector3 pointToGo))
            {
                if (reachDist < 0f)
                {
                    reachDist = SAINPlugin.LoadedPreset.GlobalSettings.General.BaseReachDistance;
                }
                CurrentPathStatus = BotOwner.Mover.GoToPoint(pointToGo, slowAtEnd, reachDist, false, false, true);
                if (CurrentPathStatus == NavMeshPathStatus.PathComplete)
                {
                    if (crawl)
                    {
                        Prone.SetProne(true);
                    }
                    SAINBot.DoorOpener.Update();
                    calculating = false;
                    return true;
                }
            }

            CurrentPathStatus = NavMeshPathStatus.PathInvalid;

            //GoToPointCoroutine = SAIN.StartCoroutine(TryGoToPoint(point, reachDist, crawl));
            //calculating = _coroutineRunning;

            calculating = false;
            return CurrentPathStatus != NavMeshPathStatus.PathInvalid;
        }

        private IEnumerator TryGoToPoint(Vector3 point, float reachDist = -1f, bool crawl = false)
        {
            _coroutineRunning = true;
            yield return null;

            //SAIN.NavMeshAgent.enabled = true;

            if (CanGoToPoint(point, out Vector3 pointToGo))
            {
                if (reachDist < 0f)
                {
                    reachDist = BotOwner.Settings.FileSettings.Move.REACH_DIST;
                }
                CurrentPathStatus = BotOwner.Mover.GoToPoint(pointToGo, true, reachDist, false, false, true);
                if (CurrentPathStatus == NavMeshPathStatus.PathComplete)
                {
                    if (crawl)
                    {
                        Prone.SetProne(true);
                    }
                    SAINBot.DoorOpener.Update();
                    _coroutineRunning = false;
                    yield break;
                }
            }

            CurrentPathStatus = NavMeshPathStatus.PathInvalid;
            _coroutineRunning = false;
            yield return null;
        }

        private float CarvingTime = 0.5f;
        private float CarvingMoveThreshold = 0.1f;

        private float LastMoveTime;
        private Vector3 LastPosition;

        private Coroutine GoToPointCoroutine;
        private bool _coroutineRunning;
        public NavMeshPathStatus CurrentPathStatus { get; private set; } = NavMeshPathStatus.PathInvalid;

        public bool CanGoToPoint(Vector3 point, out Vector3 pointToGo, bool mustHaveCompletePath = true, float navSampleRange = 5f)
        {
            pointToGo = point;
            if (NavMesh.SamplePosition(point, out NavMeshHit targetHit, navSampleRange, -1) 
                && NavMesh.SamplePosition(SAINBot.Transform.Position, out NavMeshHit botHit, navSampleRange, -1))
            {
                if (CurrentPath == null)
                {
                    CurrentPath = new NavMeshPath();
                }
                else
                {
                    CurrentPath.ClearCorners();
                }
                if (NavMesh.CalculatePath(botHit.position, targetHit.position, -1, CurrentPath) && CurrentPath.corners.Length > 1)
                {
                    if (mustHaveCompletePath && CurrentPath.status != NavMeshPathStatus.PathComplete)
                    {
                        return false;
                    }
                    pointToGo = targetHit.position;
                    return true;
                }
            }
            return false;
        }

        public bool CanGoToPoint(Vector3 point, out NavMeshPath path, bool mustHaveCompletePath = true, float navSampleRange = 1f)
        {
            if (NavMesh.SamplePosition(point, out NavMeshHit targetHit, navSampleRange, -1)
                && NavMesh.SamplePosition(SAINBot.Transform.Position, out NavMeshHit botHit, navSampleRange, -1))
            {
                path = new NavMeshPath();
                if (NavMesh.CalculatePath(botHit.position, targetHit.position, -1, path) && path.corners.Length > 1)
                {
                    if (mustHaveCompletePath 
                        && path.status != NavMeshPathStatus.PathComplete)
                    {
                        return false;
                    }
                    return true;
                }
            }
            path = null;
            return false;
        }

        public SAINMovementPlan MovementPlan { get; private set; }

        public bool GoToPointNew(Vector3 point, float reachDist = -1f, bool crawl = false, bool mustHaveCompletePath = false, float navSampleRange = 3f)
        {
            if (FindPathToPoint(CurrentPath, point, mustHaveCompletePath, navSampleRange))
            {
                if (reachDist < 0f)
                {
                    reachDist = BotOwner.Settings.FileSettings.Move.REACH_DIST;
                }
                BotOwner.Mover.GoToByWay(CurrentPath.corners, reachDist);
                if (crawl)
                {
                    Prone.SetProne(true);
                }
                SAINBot.DoorOpener.Update();
                return true;
            }
            return false;
        }

        public bool CanGoToPointNew(Vector3 point, out Vector3 pointToGo, bool mustHaveCompletePath = false, float navSampleRange = 3f)
        {
            pointToGo = Vector3.zero;

            if (NavMesh.SamplePosition(point, out var navHit, navSampleRange, -1))
            {
                if (CurrentPath == null)
                {
                    CurrentPath = new NavMeshPath();
                }
                else
                {
                    CurrentPath.ClearCorners();
                }
                if (NavMesh.CalculatePath(SAINBot.Transform.Position, navHit.position, -1, CurrentPath) && CurrentPath.corners.Length > 1)
                {
                    if (mustHaveCompletePath && CurrentPath.status != NavMeshPathStatus.PathComplete)
                    {
                        return false;
                    }
                    pointToGo = navHit.position;

                    //SAINVaultClass.FindVaultPoint(Player, Path, out SAINVaultPoint vaultPoint);

                    return true;
                }
            }
            return false;
        }

        public bool FindPathToPoint(NavMeshPath path, Vector3 pointToGo, bool mustHaveCompletePath = false, float navSampleRange = 3f)
        {
            if (path == null)
            {
                path = new NavMeshPath();
            }

            if (NavMesh.SamplePosition(pointToGo, out var navHit, navSampleRange, -1))
            {
                path.ClearCorners();
                if (NavMesh.CalculatePath(SAINBot.Transform.Position, navHit.position, -1, path) && path.corners.Length > 1)
                {
                    if (mustHaveCompletePath && path.status != NavMeshPathStatus.PathComplete)
                    {
                        return false;
                    }

                    return true;
                }
            }
            return false;
        }

        public NavMeshPath CurrentPath { get; private set; }

        private void SetStamina()
        {
            if (SAINBot.CurrentTargetPosition != null 
                && !SAINBot.Extracting 
                && CurrentStamina < 0.1f)
            {
                Player.Physical.Stamina.UpdateStamina(Player.Physical.Stamina.TotalCapacity / 4f);
            }
        }

        public float CurrentStamina => Player.Physical.Stamina.NormalValue;

        public void SetTargetPose(float pose)
        {
            Pose.SetTargetPose(pose);
        }

        public void SetTargetMoveSpeed(float speed)
        {
            if (SprintController.Running || Player.IsSprintEnabled)
            {
                BotOwner.Mover?.SetTargetMoveSpeed(1f);
            }
            else
            {
                BotOwner.Mover?.SetTargetMoveSpeed(speed);
            }
        }

        public void StopMove(float delay = 0.1f, float forDuration = 0f)
        {
            if (Player?.IsSprintEnabled == true)
            {
                Sprint(false);
            }
            if (delay <= 0f)
            {
                stop(forDuration);
                return;
            }
            if (delay > 0 && 
                !_stopping && 
                BotOwner?.Mover?.IsMoving == true)
            {
                _stopping = true;
                SAINBot.StartCoroutine(StopAfterDelay(delay, forDuration));
            }
        }

        private float _nextStopMoveTime;

        private IEnumerator StopAfterDelay(float delay, float forDuration)
        {
            yield return new WaitForSeconds(delay); 
            stop(forDuration);
            _stopping = false;
        }

        private void stop(float forDuration)
        {
            if (BotOwner?.Mover?.IsMoving == true)
            {
                BotOwner.Mover.Stop();
            }
            SAINBot?.Mover.SprintController.CancelRun();
            PauseMovement(forDuration);
        }

        public void PauseMovement(float forDuration)
        {
            if (forDuration > 0)
            {
                BotOwner?.Mover?.MovementPause(forDuration);
            }
        }

        public void ResetPath(float delay)
        {
            SAINBot.StartCoroutine(resetPath(0.2f));
        }

        private IEnumerator resetPath(float delay)
        {
            yield return StopAfterDelay(delay, 0f);
            BotOwner?.Mover?.RecalcWay();
        }

        private bool _stopping;

        public void Sprint(bool value)
        {
            if (BotOwner.DoorOpener.Interacting)
            {
                value = false;
            }
            if (value)
            {
                SAINBot.Steering.LookToMovingDirection();
                FastLean(0f);
            }
            BotOwner.Mover.Sprint(value);
        }

        public void TryJump()
        {
            if (JumpTimer < Time.time && CanJump)
            {
                JumpTimer = Time.time + 0.5f;
                Player.MovementContext.TryJump();
            }
        }

        public void FastLean(LeanSetting value)
        {
            float num;
            switch (value)
            {
                case LeanSetting.Left:
                    num = -5f; break;
                case LeanSetting.Right:
                    num = 5f; break;
                default:
                    num = 0f; break;
            }
            FastLean(num);
        }

        public void FastLean(float value)
        {
            setTilt(value);
            handleShoulderSwap(value);
        }

        private void setTilt(float value)
        {
            if (Player.MovementContext.Tilt != value)
            {
                Player.MovementContext.SetTilt(value);
            }
        }

        private void handleShoulderSwap(float leanValue)
        {
            bool shoulderSwapped = isShoulderSwapped;
            if ((leanValue < 0 && !shoulderSwapped) 
                || (leanValue >= 0 && shoulderSwapped))
            {
                Player.MovementContext.LeftStanceController.ToggleLeftStance();
            }
        }

        private bool isShoulderSwapped => Player.MovementContext.LeftStanceController.LeftStance;

        public bool CanJump => Player.MovementContext.CanJump;

        private float JumpTimer = 0f;
    }
}