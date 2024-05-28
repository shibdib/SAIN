using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Enemy;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using static EFT.SpeedTree.TreeWind;
using static UnityEngine.Rendering.PostProcessing.HistogramMonitor;

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
        public SAINSearchClass(BotComponent sain) : base(sain)
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
            if (SAINBot.Decision.CurrentSoloDecision != SoloDecision.Search
                && _nextRecalcSearchTime < Time.time)
            {
                _nextRecalcSearchTime = Time.time + 30f;
                SAINBot.Info.CalcTimeBeforeSearch();
            }
            destination = Vector3.zero;

            return WantToSearch() && HasPathToSearchTarget(out destination, mustHaveTarget);
        }

        private float _nextRecalcSearchTime;

        public bool WantToSearch()
        {
            bool wantToSearch = false;
            float timeBeforeSearch = SAINBot.Info.TimeBeforeSearch;

            SAINEnemy enemy = SAINBot.Enemy;

            if ((enemy != null && enemy.Seen) || 
                enemy != null && enemy.Heard && SAINBot.Info.PersonalitySettings.Search.WillSearchFromAudio)
            {
                wantToSearch = shallSearch(enemy, timeBeforeSearch);
            }
            //else if (SAINBot.Info.PersonalitySettings.Search.WillSearchFromAudio)
            //{
            //    Vector3? target = SAINBot.CurrentTargetPosition;
            //    if (target != null && SAINBot.TimeSinceTargetFound > timeBeforeSearch)
            //    {
            //        wantToSearch = true;
            //    }
            //}
            return wantToSearch;
        }

        private bool shallSearch(SAINEnemy enemy, float timeBeforeSearch)
        {
            if (shallBeStealthyDuringSearch(enemy) && 
                SAINBot.Decision.EnemyDecisions.UnFreezeTime > Time.time &&
                enemy.TimeSinceLastKnownUpdated > 10f)
            {
                return true;
            }
            if (shallSearchCauseLooting(enemy))
            {
                return true;
            }
            if (enemy.EnemyStatus.SearchStarted)
            {
                return shallContinueSearch(enemy, timeBeforeSearch);
            }
            else
            {
                return shallBeginSearch(enemy, timeBeforeSearch);
            }
        }

        private bool shallBeStealthyDuringSearch(SAINEnemy enemy)
        {
            return enemy.EnemyHeardFromPeace;
        }

        private bool shallSearchCauseLooting(SAINEnemy enemy)
        {
            if (enemy.EnemyStatus.EnemyIsLooting)
            {
                if (!enemy.EnemyStatus.SearchStarted && _nextCheckLootTime < Time.time)
                {
                    _nextCheckLootTime = Time.time + _checkLootFreq;
                    if (EFTMath.RandomBool(_searchLootChance))
                    {
                        return true;
                    }
                }
                if (enemy.EnemyStatus.SearchStarted)
                {
                    return true;
                }
            }
            return false;
        }

        private float _nextCheckLootTime;
        private float _checkLootFreq = 1f;
        private float _searchLootChance = 40f;

        private bool shallBeginSearch(SAINEnemy enemy, float timeBeforeSearch)
        {
            var searchSettings = SAINBot.Info.PersonalitySettings.Search;
            if (searchSettings.WillSearchForEnemy
                && !SAINBot.Suppression.IsSuppressed
                && !enemy.IsVisible
                && !BotOwner.Memory.IsUnderFire)
            {
                float myPower = SAINBot.Info.PowerLevel;
                if (enemy.EnemyPlayer.AIData.PowerOfEquipment < myPower * 0.5f)
                {
                    return true;
                }
                if (enemy.Seen && enemy.TimeSinceSeen >= timeBeforeSearch)
                {
                    return true;
                }
                else 
                if (enemy.Heard &&
                    searchSettings.WillSearchFromAudio &&
                    enemy.TimeSinceHeard >= timeBeforeSearch)
                {
                    return true;
                }
            }
            return false;
        }

        private bool shallContinueSearch(SAINEnemy enemy, float timeBeforeSearch)
        {
            var searchSettings = SAINBot.Info.PersonalitySettings.Search;
            if (searchSettings.WillSearchForEnemy
                && !SAINBot.Suppression.IsSuppressed
                && !enemy.IsVisible
                && !BotOwner.Memory.IsUnderFire)
            {
                timeBeforeSearch = Mathf.Clamp(timeBeforeSearch / 3f, 0f, 60f);

                if (enemy.Seen && enemy.TimeSinceSeen >= timeBeforeSearch)
                {
                    return true;
                }
                else
                if (enemy.Heard &&
                    searchSettings.WillSearchFromAudio &&
                    enemy.TimeSinceHeard >= timeBeforeSearch)
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
                _nextCheckSearchTime = Time.time + 0.25f;
                Vector3? destination = SearchMovePos(out bool hasTarget);
                if (destination == null)
                {
                    _canStartSearch = false;
                }
                else if (needTarget && !hasTarget)
                {
                    _canStartSearch = false;
                }
                else if (CalculatePath(destination.Value) != NavMeshPathStatus.PathInvalid)
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
            if (SAINBot.Memory.Location.BotZoneCollider != null)
            {
                Vector3 closestPointInZone = SAINBot.Memory.Location.BotZoneCollider.ClosestPointOnBounds(target);
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
            if (!SearchedTargetPosition && (FinalDestination - SAINBot.Position).sqrMagnitude < 1f)
            {
                SearchedTargetPosition = true;
            }

            if (SearchedTargetPosition || _finishedSearchPath)
            {
                Vector3? newTarget = SearchMovePos(out bool hasTarget, true);
                
                if (newTarget != null
                    && CalculatePath(newTarget.Value) != NavMeshPathStatus.PathInvalid)
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
                    && (newTarget.Value - FinalDestination).sqrMagnitude > 2f * 2f
                    && CalculatePath(newTarget.Value) != NavMeshPathStatus.PathInvalid)
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

        public SAINEnemy SearchTarget { get; set; }

        private float _nextCheckPosTime;

        public bool SearchedTargetPosition { get; private set; }

        public Vector3? SearchMovePos(out bool hasTarget, bool randomSearch = false)
        {
            var enemy = SAINBot.Enemy;
            if (enemy != null && (enemy.Seen || enemy.Heard))
            {
                if (enemy.IsVisible)
                {
                    hasTarget = true;
                    return enemy.EnemyPosition;
                }
                else
                {
                    var knownPlaces = enemy.KnownPlaces.AllEnemyPlaces;
                    for (int i = 0; i < knownPlaces.Count; i++)
                    {
                        EnemyPlace enemyPlace = knownPlaces[i];
                        if (enemyPlace != null 
                            && !enemyPlace.HasArrivedPersonal)
                        {
                            hasTarget = true;
                            return enemyPlace.Position;
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
                var Target = BotOwner?.Memory.GoalTarget;
                if (Target?.Position != null)
                {
                    hasTarget = true;
                    return Target.Position.Value;
                }
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
                var personalitySettings = SAINBot.Info.PersonalitySettings;
                if (personalitySettings != null)
                {
                    baseTime /= personalitySettings.Search.SearchWaitMultiplier;
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

            if (!shallSprint && SAINBot.Mover.SprintController.Running)
            {
                SAINBot.Mover.SprintController.CancelRun();
            }

            _Running = false;
            if (shallSprint 
                && SAINBot.Mover.SprintController.RunToPoint(destination))
            {
                _Running = true;
                return true;
            }
            else
            {
                SAINBot.Mover.Sprint(false);
                return SAINBot.Mover.GoToPoint(destination, out _);
            }
        }

        private bool _Running;
        private bool _setMaxSpeedPose;

        private void SwitchSearchModes(bool shallSprint)
        {
            var persSettings = SAINBot.Info.PersonalitySettings;
            float speed = 1f;
            float pose = 1f;
            _setMaxSpeedPose = false;
            bool shallBeStealthy = SAINBot.Enemy != null && shallBeStealthyDuringSearch(SAINBot.Enemy);
            // Environment id of 0 means a bot is outside.
            if (shallSprint || Player.IsSprintEnabled || _Running)
            {
                _setMaxSpeedPose = true;
                speed = 1f;
                pose = 1f;
            }
            else if (!SAINBot.Memory.Location.IsIndoors)
            {
                if (persSettings.Search.Sneaky && SAINBot.Cover.CoverPoints.Count > 2 && Time.time - BotOwner.Memory.UnderFireTime > 30f)
                {
                    speed = 0.33f;
                    pose = 1f;
                }
                else if (shallBeStealthy)
                {
                    speed = 0.33f;
                    pose = 0.7f;
                }
                else
                {
                    speed = 1f;
                    pose = 1f;
                }
            }
            else if (persSettings.Search.Sneaky)
            {
                speed = persSettings.Search.SneakySpeed;
                pose = persSettings.Search.SneakyPose;
            }
            else if (shallBeStealthy)
            {
                speed = 0f;
                pose = 0f;
            }

            if (!shallSprint && SAINBot.Mover.SprintController.Running)
            {
                SAINBot.Mover.SprintController.CancelRun();
            }

            LastState = CurrentState;
            switch (LastState)
            {
                case ESearchMove.None:
                    if (_finishedPeek && HasPathToSearchTarget(out Vector3 finalDestination, false))
                    {
                        _finishedPeek = false;
                        FinalDestination = finalDestination;
                    }
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
                        NextState = ESearchMove.MoveToEndPeek;
                    }
                    break;

                case ESearchMove.DirectMove:

                    if (_setMaxSpeedPose)
                    {
                        SAINBot.Mover.SetTargetMoveSpeed(1f);
                        SAINBot.Mover.SetTargetPose(1f);
                    }
                    else
                    {
                        SAINBot.Mover.SetTargetMoveSpeed(speed);
                        SAINBot.Mover.SetTargetPose(pose);
                    }

                    MoveToPoint(FinalDestination, shallSprint);
                    //if (BotIsAtPoint(FinalDestination))
                    //{
                    //    SearchedTargetPosition = true;
                    //} else
                    if (ShallRecalcPath())
                    {
                        //MoveToPoint(FinalDestination, shallSprint);
                    }
                    break;

                case ESearchMove.MoveToStartPeek:

                    if (_setMaxSpeedPose)
                    {
                        SAINBot.Mover.SetTargetMoveSpeed(1f);
                        SAINBot.Mover.SetTargetPose(1f);
                    }
                    else
                    {
                        SAINBot.Mover.SetTargetMoveSpeed(speed);
                        SAINBot.Mover.SetTargetPose(pose);
                    }

                    if (BotIsAtPoint(ActiveDestination) 
                        && MoveToPoint(SearchMovePoint.EndPeekPosition, shallSprint))
                    {
                        ActiveDestination = SearchMovePoint.EndPeekPosition;
                        CurrentState = NextState;
                        NextState = ESearchMove.Wait;
                    }
                    else if (ShallRecalcPath())
                    {
                        MoveToPoint(ActiveDestination, shallSprint);
                    }
                    break;

                case ESearchMove.MoveToEndPeek:

                    if (_setMaxSpeedPose)
                    {
                        SAINBot.Mover.SetTargetMoveSpeed(1f);
                        SAINBot.Mover.SetTargetPose(1f);
                    }
                    else
                    {
                        SAINBot.Mover.SetTargetMoveSpeed(0.1f);
                        SAINBot.Mover.SetTargetPose(pose);
                    }

                    if (BotIsAtPoint(ActiveDestination))
                    {
                        ActiveDestination = SearchMovePoint.DangerPoint;
                        CurrentState = NextState;
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
                        SAINBot.Mover.SetTargetMoveSpeed(1f);
                        SAINBot.Mover.SetTargetPose(1f);
                    }
                    else
                    {
                        SAINBot.Mover.SetTargetMoveSpeed((speed / 2f).Round100());
                        SAINBot.Mover.SetTargetPose(pose);
                    }

                    if (BotIsAtPoint(ActiveDestination))
                    {
                        CurrentState = ESearchMove.None;
                        NextState = ESearchMove.None;
                        _finishedPeek = true;
                        return;
                    }
                    else if (ShallRecalcPath())
                    {
                        MoveToPoint(ActiveDestination, shallSprint);
                    }
                    break;

                case ESearchMove.Wait:
                    SAINBot.Mover.SetTargetMoveSpeed(0f);
                    SAINBot.Mover.SetTargetPose(0.75f);
                    if (!WaitAtPoint())
                    {
                        CurrentState = NextState;
                        NextState = ESearchMove.None;
                    }
                    break;
            }
        }

        private bool _finishedPeek;
        private bool _finishedSearchPath;

        private bool CheckIfStuck()
        {
            bool botIsStuck = 
                (!SAINBot.BotStuck.BotHasChangedPosition && SAINBot.BotStuck.TimeSpentNotMoving > 3f) 
                || SAINBot.BotStuck.BotIsStuck;

            if (botIsStuck && UnstuckMoveTimer < Time.time)
            {
                UnstuckMoveTimer = Time.time + 2f;

                var TargetPosition = SAINBot.CurrentTargetPosition;
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
            Vector3 Start = SAINBot.Position;
            if ((point - Start).sqrMagnitude <= 0.5f)
            {
                return NavMeshPathStatus.PathInvalid;
            }

            Reset();

            if (NavMesh.SamplePosition(point, out var hit, 10f, -1) && NavMesh.SamplePosition(Start, out var hit2, 1f, -1))
            {
                Path = new NavMeshPath();
                if (NavMesh.CalculatePath(hit2.position, hit.position, -1, Path))
                {
                    ReachDistance = reachDist > 0 ? reachDist : 0.5f;
                    FinalDestination = hit.position;
                    /*
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
                    */
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

        public bool BotIsAtPoint(Vector3 point, float reachDist = 0.5f, bool Sqr = true)
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