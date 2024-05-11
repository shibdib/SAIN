using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using static EFT.SpeedTree.TreeWind;

namespace SAIN.SAINComponent.Classes
{
    public enum SearchStates
    {
        None,
        FindRoute,
        MoveToCorner,
        CheckCorners,
        HoldPosition,
        Wait,
        RushEnemy,
    }

    public class SAINSearchClass : SAINBase, ISAINClass
    {
        public SAINSearchClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public MoveDangerPoint SearchMovePoint { get; private set; }
        public Vector3 ActiveDestination { get; private set; }

        public bool ShallStartSearch(out Vector3 destination, bool mustHaveTarget = false)
        {
            if (SAIN.Decision.CurrentSoloDecision != SoloDecision.Search
                && _nextRecalcSearchTime < Time.time)
            {
                _nextRecalcSearchTime = Time.time + 30f;
                SAIN.Info.CalcTimeBeforeSearch();
            }
            destination = Vector3.zero;

            return WantToSearch() && HasPathToSearchTarget(out destination, mustHaveTarget);
        }

        private float _nextRecalcSearchTime;

        public bool WantToSearch()
        {
            bool wantToSearch = false;
            float timeBeforeSearch = SAIN.Info.TimeBeforeSearch;

            SAINEnemy enemy = SAIN.Enemy;

            if (enemy != null)
            {
                wantToSearch = shallSearch(enemy, timeBeforeSearch);
            }
            else if (SAIN.Info.PersonalitySettings.WillSearchFromAudio)
            {
                Vector3? target = SAIN.CurrentTargetPosition;
                if (target != null)
                {
                    if (SAIN.Info.Profile.IsPMC)
                    {
                        wantToSearch = true;
                    }
                    else if (SAIN.Memory.BotZoneCollider == null
                            || checkBotZone(target.Value))
                    {
                        wantToSearch = true;
                    }
                }
            }
            return wantToSearch;
        }

        private bool shallSearch(SAINEnemy enemy, float timeBeforeSearch)
        {
            if (enemy.SearchStarted)
            {
                return shallContinueSearch(enemy, timeBeforeSearch);
            }
            else
            {
                return shallBeginSearch(enemy, timeBeforeSearch);
            }
        }

        private bool shallBeginSearch(SAINEnemy enemy, float timeBeforeSearch)
        {
            if (SAIN.Info.PersonalitySettings.WillSearchForEnemy
                && !SAIN.Suppression.IsSuppressed
                && !enemy.IsVisible
                && !BotOwner.Memory.IsUnderFire
                && Time.time - BotOwner.Memory.UnderFireTime > 30f)
            {
                if (enemy.Seen && enemy.TimeSinceSeen >= timeBeforeSearch)
                {
                    return true;
                }
                else if ((enemy.Seen || enemy.Heard)
                    && enemy.TimeSinceLastKnownUpdated >= timeBeforeSearch)
                {
                    return true;
                }
            }
            return false;
        }

        private bool shallContinueSearch(SAINEnemy enemy, float timeBeforeSearch)
        {
            if (SAIN.Info.PersonalitySettings.WillSearchForEnemy
                && !SAIN.Suppression.IsSuppressed
                && !enemy.IsVisible
                && !BotOwner.Memory.IsUnderFire)
            {
                timeBeforeSearch = Mathf.Clamp(timeBeforeSearch / 4f, 0.2f, 30f);

                if (enemy.Seen && enemy.TimeSinceSeen >= timeBeforeSearch)
                {
                    return true;
                }
                else if ((enemy.Seen || enemy.Heard)
                    && enemy.TimeSinceLastKnownUpdated >= timeBeforeSearch)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasPathToSearchTarget(out Vector3 finalDestination, bool needTarget = false)
        {
            if (_nextCheckSearchTime < Time.time)
            {
                _nextCheckSearchTime = Time.time + 1f;
                Vector3? destination = SearchMovePos(out bool hasTarget);
                if (destination == null)
                {
                    _canStartSearch = false;
                }
                else if (needTarget && !hasTarget)
                {
                    _canStartSearch = false;
                }
                else if (CalculatePath(destination.Value) == NavMeshPathStatus.PathComplete)
                {
                    _canStartSearch = true;
                    FinalDestination = destination.Value;
                }
                else
                {
                    _canStartSearch = false;
                }
            }
            finalDestination = FinalDestination;
            return _canStartSearch;
        }

        private bool checkBotZone(Vector3 target)
        {
            if (SAIN.Memory.BotZoneCollider != null)
            {
                Vector3 closestPointInZone = SAIN.Memory.BotZoneCollider.ClosestPointOnBounds(target);
                float distance = (target - closestPointInZone).sqrMagnitude;
                if (distance > 50f * 50f)
                {
                    return false;
                }
            }
            return true;
        }

        private bool _canStartSearch;
        private float _nextCheckSearchTime;

        private void updateSearchDestination()
        {
            if (!SearchedTargetPosition && (FinalDestination - SAIN.Position).sqrMagnitude < 1f)
            {
                SearchedTargetPosition = true;
            }

            if (SearchedTargetPosition || _finishedSearchPath)
            {
                Vector3? newTarget = SearchMovePos(out bool hasTarget, true);
                
                if (newTarget != null
                    && CalculatePath(newTarget.Value) == NavMeshPathStatus.PathComplete)
                {
                    SearchedTargetPosition = false;
                    _finishedSearchPath = false;
                    FinalDestination = newTarget.Value;
                }
            }


            if (_nextCheckPosTime < Time.time)
            {
                _nextCheckPosTime = Time.time + 2f;

                Vector3? newTarget = SearchMovePos(out bool hasTarget);

                if (newTarget != null
                    && hasTarget 
                    && (newTarget.Value - FinalDestination).sqrMagnitude > 5f * 5f
                    && CalculatePath(newTarget.Value) == NavMeshPathStatus.PathComplete)
                {
                    SearchedTargetPosition = false;
                    _finishedSearchPath = false;
                    FinalDestination = newTarget.Value;
                }
            }

        }

        public void Search(bool shallSprint, float reachDist = -1f)
        {
            if (reachDist > 0)
            {
                ReachDistance = reachDist;
            }

            updateSearchDestination();
            CheckIfStuck();
            SwitchSearchModes(shallSprint);
            SearchMovePoint?.DrawDebug();
        }

        private float _nextCheckPosTime;

        public bool SearchedTargetPosition { get; private set; }

        public Vector3? SearchMovePos(out bool hasTarget, bool randomSearch = false)
        {
            var enemy = SAIN.Enemy;
            if (enemy != null && (enemy.Seen || enemy.Heard))
            {
                if (enemy.IsVisible)
                {
                    hasTarget = true;
                    return SAIN.Enemy.EnemyPosition;
                }
                else
                {
                    var knownPlaces = enemy.KnownPlaces.KnownPlaces;
                    for (int i = knownPlaces.Count - 1; i >= 0; i--)
                    {
                        EnemyPlace enemyPlace = knownPlaces[i];
                        if (enemyPlace?.Position != null && !enemyPlace.HasArrived && !enemyPlace.HasSeen)
                        {
                            hasTarget = true;
                            return enemyPlace.Position.Value;
                        }
                    }
                    hasTarget = false;
                    if (randomSearch)
                    {
                        return RandomSearch();
                    }
                    return null;
                }
            }
            else
            {
                var Target = BotOwner.Memory.GoalTarget;
                if (Target != null && Target?.Position != null)
                {
                    hasTarget = true;
                    return Target.Position.Value;
                }
                /*
                var sound = BotOwner.BotsGroup.YoungestPlace(BotOwner, 200f, true);
                if (sound != null && !sound.IsCome)
                {
                    if ((sound.Position - BotOwner.Position).sqrMagnitude < 2f)
                    {
                        if (EFTMath.RandomBool(33))
                        {
                            SAIN.Talk.Say(EFTMath.RandomBool() ? EPhraseTrigger.Clear : EPhraseTrigger.LostVisual, null, true);
                        }
                        sound.IsCome = true;
                        BotOwner.CalcGoal();
                    }
                    else
                    {
                        return sound.Position;
                    }
                }
                */
            }

            hasTarget = false;
            if (randomSearch)
            {
                return RandomSearch();
            }
            return null;
        }

        private const float ComeToRandomDist = 1f;

        private Vector3 RandomSearch()
        {
            float dist = (RandomSearchPoint - BotOwner.Position).sqrMagnitude;
            if (dist < ComeToRandomDist * ComeToRandomDist || dist > 60f * 60f)
            {
                RandomSearchPoint = GenerateSearchPoint();
            }
            return RandomSearchPoint;
        }

        private Vector3 RandomSearchPoint = Vector3.down * 300f;

        private Vector3 GenerateSearchPoint()
        {
            Vector3 start = BotOwner.Position;
            float dispersion = 30f;
            for (int i = 0; i < 10; i++)
            {
                float dispNum = EFTMath.Random(-dispersion, dispersion);
                Vector3 vector = new Vector3(start.x + dispNum, start.y, start.z + dispNum);
                if (NavMesh.SamplePosition(vector, out var hit, 10f, -1))
                {
                    Path.ClearCorners();
                    if (NavMesh.CalculatePath(hit.position, start, -1, Path) && Path.status == NavMeshPathStatus.PathComplete)
                    {
                        return hit.position;
                    }
                }
            }
            return start;
        }

        public ESearchMove NextState = ESearchMove.None;
        public ESearchMove CurrentState = ESearchMove.None;
        public ESearchMove LastState = ESearchMove.None;
        public float WaitTimer { get; private set; }
        private float RecalcPathTimer;

        private bool ShallRecalcPath()
        {
            if (RecalcPathTimer < Time.time)
            {
                RecalcPathTimer = Time.time + 0.5f;
                return true;
            }
            return false;
        }

        private bool WaitAtPoint()
        {
            if (WaitPointTimer < 0)
            {
                float baseTime = 3;
                var personalitySettings = SAIN.Info.PersonalitySettings;
                if (personalitySettings != null)
                {
                    baseTime /= personalitySettings.AggressionMultiplier;
                }
                float waitTime = baseTime * Random.Range(0.25f, 1.25f);
                WaitPointTimer = Time.time + waitTime;
                BotOwner.Mover.MovementPause(waitTime, false);
            }
            if (WaitPointTimer < Time.time)
            {
                WaitPointTimer = -1;
                return false;
            }
            return true;
        }

        private float WaitPointTimer = -1;

        private int Index = 0;

        public Vector3 FinalDestination { get; private set; } = Vector3.zero;

        private Vector3 NextCorner()
        {
            int i = Index;
            if (Path.corners.Length > i)
            {
                Index++;
                return Path.corners[i];
            }
            return Path.corners[Path.corners.Length - 1];
        }

        private bool MoveToPoint(Vector3 destination, bool shallSprint)
        {
            RecalcPathTimer = Time.time + 2;

            if (!shallSprint)
            {
                //SAIN.Mover.Sprint(false);
            }

            _Running = false;
            if (shallSprint 
                && BotOwner.BotRun.Run(destination, false, SAINPlugin.LoadedPreset.GlobalSettings.General.SprintReachDistance))
            {
                SAIN.Steering.LookToMovingDirection(500f, true);
                _Running = true;
                return true;
            }
            else
            {
                SAIN.Mover.Sprint(false);
                return SAIN.Mover.GoToPoint(destination, out _);
            }
        }

        private bool _Running;
        private bool _setMaxSpeedPose;

        private float DecideSpeed(Vector3 targetPosition, out float poseLevel)
        {
            const float slowDownDist = 50;
            const float minSpeedThreshold = 10f;

            float sqrMag = (targetPosition - SAIN.Position).sqrMagnitude;

            if (sqrMag > slowDownDist * slowDownDist)
            {
                poseLevel = 1f;
                return 1f;
            }

            float speedRatio = ( sqrMag - minSpeedThreshold * minSpeedThreshold ) / ( (slowDownDist * slowDownDist) - (minSpeedThreshold * minSpeedThreshold) );

            var persSettings = SAIN.Info.PersonalitySettings;
            float minSpeed = SAIN.HasEnemy ? persSettings.SearchHasEnemySpeed : persSettings.SearchNoEnemySpeed;

            poseLevel = 1f;
            return Mathf.Clamp(speedRatio, minSpeed, 1f);
        }

        private float _nextGenSearchTime;

        private void SwitchSearchModes(bool shallSprint)
        {
            var persSettings = SAIN.Info.PersonalitySettings;
            float speed = 1f;
            float pose = 1f;
            _setMaxSpeedPose = false;
            // Environment id of 0 means a bot is outside.
            if (shallSprint || SAIN.Mover.IsSprinting || Player.IsSprintEnabled || _Running)
            {
                _setMaxSpeedPose = true;
                speed = 1f;
                pose = 1f;
            }
            else if (Player.AIData.EnvironmentId == 0 && !SAIN.Memory.IsIndoors)
            {
                if (persSettings.Sneaky && SAIN.Cover.CoverPoints.Count > 2 && Time.time - BotOwner.Memory.UnderFireTime > 30f)
                {
                    speed = 0.7f;
                    pose = 1f;
                }
                else
                {
                    speed = 1f;
                    pose = 1f;
                }
            }
            else if (persSettings.Sneaky)
            {
                speed = persSettings.SneakySpeed;
                pose = persSettings.SneakyPose;
            }
            else
            {
                //speed = DecideSpeed(FinalDestination, out pose);
            }

            if (SearchMovePoint == null)
            {

            }

            LastState = CurrentState;
            switch (LastState)
            {
                case ESearchMove.None:
                    if ((shallSprint || SearchMovePoint == null) 
                        && MoveToPoint(FinalDestination, shallSprint))
                    {
                        ActiveDestination = FinalDestination;
                        CurrentState = ESearchMove.DirectMove;
                        NextState = ESearchMove.None;
                    }
                    else if (SearchMovePoint != null && MoveToPoint(SearchMovePoint.StartPeekPosition, shallSprint))
                    {
                        ActiveDestination = SearchMovePoint.StartPeekPosition;
                        CurrentState = ESearchMove.MoveToStartPeek;
                        NextState = ESearchMove.Wait;
                    }
                    break;

                case ESearchMove.DirectMove:

                    if (_setMaxSpeedPose)
                    {
                        SAIN.Mover.SetTargetMoveSpeed(1f);
                        SAIN.Mover.SetTargetPose(1f);
                    }
                    else
                    {
                        SAIN.Mover.SetTargetMoveSpeed(speed);
                        SAIN.Mover.SetTargetPose(pose);
                    }

                    if (BotIsAtPoint(ActiveDestination))
                    {
                        ActiveDestination = NextCorner();
                        MoveToPoint(ActiveDestination, shallSprint);
                    }
                    else if (ShallRecalcPath())
                    {
                        MoveToPoint(ActiveDestination, shallSprint);
                    }
                    break;

                case ESearchMove.MoveToStartPeek:

                    if (_setMaxSpeedPose)
                    {
                        SAIN.Mover.SetTargetMoveSpeed(1f);
                        SAIN.Mover.SetTargetPose(1f);
                    }
                    else
                    {
                        SAIN.Mover.SetTargetMoveSpeed(speed);
                        SAIN.Mover.SetTargetPose(pose);
                    }

                    if (BotIsAtPoint(ActiveDestination) 
                        && MoveToPoint(SearchMovePoint.EndPeekPosition, shallSprint))
                    {
                        ActiveDestination = SearchMovePoint.EndPeekPosition;
                        CurrentState = ESearchMove.Wait;
                        NextState = ESearchMove.MoveToEndPeak;
                    }
                    else if (ShallRecalcPath())
                    {
                        MoveToPoint(ActiveDestination, shallSprint);
                    }
                    break;

                case ESearchMove.MoveToEndPeak:

                    if (_setMaxSpeedPose)
                    {
                        SAIN.Mover.SetTargetMoveSpeed(1f);
                        SAIN.Mover.SetTargetPose(1f);
                    }
                    else
                    {
                        SAIN.Mover.SetTargetMoveSpeed(0.1f);
                        SAIN.Mover.SetTargetPose(pose);
                    }

                    if (BotIsAtPoint(ActiveDestination) && MoveToPoint(SearchMovePoint.DangerPoint, shallSprint))
                    {
                        ActiveDestination = SearchMovePoint.DangerPoint;
                        CurrentState = ESearchMove.Wait;
                        NextState = ESearchMove.MoveToDangerPoint;
                    }
                    else if (ShallRecalcPath())
                    {
                        MoveToPoint(ActiveDestination, shallSprint);
                    }
                    break;

                case ESearchMove.MoveToDangerPoint:

                    if (_setMaxSpeedPose)
                    {
                        SAIN.Mover.SetTargetMoveSpeed(1f);
                        SAIN.Mover.SetTargetPose(1f);
                    }
                    else
                    {
                        SAIN.Mover.SetTargetMoveSpeed((speed / 2f).Round100());
                        SAIN.Mover.SetTargetPose(pose);
                    }

                    if (BotIsAtPoint(ActiveDestination))
                    {
                        _finishedSearchPath = true;
                        return;
                    }
                    else if (ShallRecalcPath())
                    {
                        MoveToPoint(ActiveDestination, shallSprint);
                    }
                    break;

                case ESearchMove.Wait:
                    SAIN.Mover.SetTargetMoveSpeed(0f);
                    SAIN.Mover.SetTargetPose(0.75f);
                    if (!WaitAtPoint())
                    {
                        CurrentState = NextState;
                        NextState = ESearchMove.None;
                    }
                    break;
            }
        }

        private bool _finishedSearchPath;

        private bool CheckIfStuck()
        {
            bool botIsStuck = 
                (!SAIN.BotStuck.BotHasChangedPosition && SAIN.BotStuck.TimeSpentNotMoving > 3f) 
                || SAIN.BotStuck.BotIsStuck;

            if (botIsStuck && UnstuckMoveTimer < Time.time)
            {
                UnstuckMoveTimer = Time.time + 2f;

                var TargetPosition = SAIN.CurrentTargetPosition;
                if (TargetPosition != null)
                {
                    CalculatePath(TargetPosition.Value, false);
                }
            }
            return botIsStuck;
        }

        private float UnstuckMoveTimer = 0f;

        private NavMeshPath Path = new NavMeshPath();

        public void Reset()
        {
            Index = 0;
            Path.ClearCorners();
            FinalDestination = Vector3.zero;
            SearchMovePoint = null;
            CurrentState = ESearchMove.None;
            LastState = ESearchMove.None;
            NextState = ESearchMove.None;
            _targetMoveSpeed = 1f;
            _targetPose = 1f;
            SearchedTargetPosition = false;
        }

        private float _targetMoveSpeed = 1f;
        private float _targetPose = 1f;

        public NavMeshPathStatus CalculatePath(Vector3 point, bool MustHavePath = true, float reachDist = 0.5f)
        {
            Vector3 Start = SAIN.Position;
            if ((point - Start).sqrMagnitude <= 0.5f)
            {
                return NavMeshPathStatus.PathInvalid;
            }

            Reset();

            if (NavMesh.SamplePosition(point, out var hit, 10f, -1))
            {
                Path = new NavMeshPath();
                if (NavMesh.CalculatePath(Start, hit.position, -1, Path))
                {
                    ReachDistance = reachDist > 0 ? reachDist : 0.5f;
                    FinalDestination = hit.position;
                    int cornerLength = Path.corners.Length;

                    List<Vector3> newCorners = new List<Vector3>();
                    for (int i = 0; i < cornerLength - 1; i++)
                    {
                        if ((Path.corners[i] - Path.corners[i + 1]).sqrMagnitude > 1.5f)
                        {
                            newCorners.Add(Path.corners[i]);
                        }
                    }
                    if (cornerLength > 0)
                    {
                        newCorners.Add(Path.corners[cornerLength - 1]);
                    }

                    for (int i = 0; i < newCorners.Count - 1; i++)
                    {
                        Vector3 A = newCorners[i];
                        Vector3 ADirection = A - Start;

                        Vector3 B = newCorners[i + 1];
                        Vector3 BDirection = B - Start;
                        float BDirMagnitude = BDirection.magnitude;

                        if (Physics.Raycast(Start, BDirection.normalized, BDirMagnitude, LayerMaskClass.HighPolyWithTerrainMask))
                        {
                            Vector3 startPeekPos = GetPeekStartAndEnd(A, B, ADirection, BDirection, out var endPeekPos);

                            if (NavMesh.SamplePosition(startPeekPos, out var hit2, 2f, -1)
                                && NavMesh.SamplePosition(endPeekPos, out var hit3, 2f, -1))
                            {
                                SearchMovePoint = new MoveDangerPoint(hit2.position, hit3.position, B, A);
                                break;
                            }
                        }
                    }
                }
                return Path.status;
            }

            return NavMeshPathStatus.PathInvalid;
        }

        private Vector3 GetPeekStartAndEnd(Vector3 blindCorner, Vector3 dangerPoint, Vector3 dirToBlindCorner, Vector3 dirToBlindDest, out Vector3 peekEnd)
        {
            const float maxMagnitude = 10f;
            const float minMagnitude = 1f;
            const float OppositePointMagnitude = 5f;

            Vector3 directionToStart = BotOwner.Position - blindCorner;

            Vector3 cornerStartDir;
            if (directionToStart.magnitude > maxMagnitude)
            {
                cornerStartDir = directionToStart.normalized * maxMagnitude;
            }
            else if (directionToStart.magnitude < minMagnitude)
            {
                cornerStartDir = directionToStart.normalized * minMagnitude;
            }
            else
            {
                cornerStartDir = Vector3.zero;
            }

            Vector3 PeekStartPosition = blindCorner + cornerStartDir;
            Vector3 dirFromStart = dangerPoint - PeekStartPosition;

            // Rotate to the opposite side depending on the angle of the danger point to the start.
            float signAngle = GetSignedAngle(dirToBlindCorner.normalized, dirFromStart.normalized);
            float rotationAngle = signAngle > 0 ? -90f : 90f;
            Quaternion rotation = Quaternion.Euler(0f, rotationAngle, 0f);

            var direction = rotation * dirToBlindDest.normalized;
            direction *= OppositePointMagnitude;

            Vector3 PeekEndPosition;
            // if we hit an object on the way to our Peek FinalDestination, change the peek startPeekPos to be the resulting hit DrawPosition;
            if (CheckForObstacles(PeekStartPosition, direction, out Vector3 result))
            {
                // Shorten the direction as to not try to path directly into a wall.
                Vector3 resultDir = result - PeekStartPosition;
                resultDir *= 0.9f;
                PeekEndPosition = PeekStartPosition + resultDir;
            }
            else
            {
                // Modify the startPeekPos to be the result if no objects are in the way.
                PeekEndPosition = result;
            }

            peekEnd = PeekEndPosition;
            return PeekStartPosition;
        }

        private bool CheckForObstacles(Vector3 start, Vector3 direction, out Vector3 result)
        {
            start.y += 0.1f;
            if (Physics.Raycast(start, direction, out var hit, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
            {
                result = hit.point;
                result.y -= 0.1f;
                return true;
            }
            else
            {
                result = start + direction;
                return false;
            }
        }

        public bool BotIsAtPoint(Vector3 point, float reachDist = 1f, bool Sqr = true)
        {
            if (Sqr)
            {
                return DistanceToDestinationSqr(point) < reachDist;
            }
            return DistanceToDestination(point) < reachDist;
        }

        public float DistanceToDestinationSqr(Vector3 point)
        {
            return (point - BotOwner.Transform.position).sqrMagnitude;
        }

        public float DistanceToDestination(Vector3 point)
        {
            return (point - BotOwner.Transform.position).magnitude;
        }

        private float GetSignedAngle(Vector3 dirCenter, Vector3 dirOther, Vector3? axis = null)
        {
            Vector3 angleAxis = axis ?? Vector3.up;
            return Vector3.SignedAngle(dirCenter, dirOther, angleAxis);
        }

        public float ReachDistance { get; private set; }
    }
}