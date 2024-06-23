using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Search
{
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

        public bool ShallStartSearch(out Vector3 destination, bool mustHaveTarget = true)
        {
            if (Bot.Decision.CurrentSoloDecision != SoloDecision.Search
                && _nextRecalcSearchTime < Time.time)
            {
                _nextRecalcSearchTime = Time.time + 30f;
                Bot.Info.CalcTimeBeforeSearch();
            }
            destination = Vector3.zero;

            return WantToSearch() && HasPathToSearchTarget(out destination, mustHaveTarget);
        }

        private float _nextRecalcSearchTime;

        public bool WantToSearch()
        {
            Enemy enemy = Bot.Enemy;
            if (enemy == null)
            {
                return false;
            }
            if (!enemy.Seen && !enemy.Heard)
            {
                return false;
            }
            if (!enemy.Seen && !Bot.Info.PersonalitySettings.Search.WillSearchFromAudio)
            {
                return false;
            }

            return shallSearch(enemy, Bot.Info.TimeBeforeSearch);
        }

        private bool shallSearch(Enemy enemy, float timeBeforeSearch)
        {
            if (shallBeStealthyDuringSearch(enemy) &&
                Bot.Decision.EnemyDecisions.UnFreezeTime > Time.time &&
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

        private bool shallBeStealthyDuringSearch(Enemy enemy)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Mind.SneakyBots)
            {
                return false;
            }
            if (SAINPlugin.LoadedPreset.GlobalSettings.Mind.OnlySneakyPersonalitiesSneaky && 
                !Bot.Info.PersonalitySettings.Search.Sneaky)
            {
                return false;
            }

            return enemy.EnemyHeardFromPeace &&
                (FinalDestination - Bot.Position).sqrMagnitude < SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaximumDistanceToBeSneaky.Sqr();
        }

        private bool shallSearchCauseLooting(Enemy enemy)
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

        private bool shallBeginSearch(Enemy enemy, float timeBeforeSearch)
        {
            var searchSettings = Bot.Info.PersonalitySettings.Search;
            if (searchSettings.WillSearchForEnemy
                && !Bot.Suppression.IsHeavySuppressed
                && !enemy.IsVisible)
            {
                float myPower = Bot.Info.PowerLevel;
                if (enemy.EnemyPlayer.AIData.PowerOfEquipment < myPower * 0.5f)
                {
                    return true;
                }
                if (enemy.Seen && enemy.TimeSinceSeen >= timeBeforeSearch)
                {
                    return true;
                }
                else if (enemy.Heard &&
                    searchSettings.WillSearchFromAudio &&
                    enemy.TimeSinceHeard >= timeBeforeSearch)
                {
                    return true;
                }
            }
            return false;
        }

        private bool shallContinueSearch(Enemy enemy, float timeBeforeSearch)
        {
            var searchSettings = Bot.Info.PersonalitySettings.Search;
            if (searchSettings.WillSearchForEnemy
                && !Bot.Suppression.IsHeavySuppressed
                && !enemy.IsVisible)
            {
                timeBeforeSearch = Mathf.Clamp(timeBeforeSearch / 3f, 0f, 60f);

                if (enemy.Seen && enemy.TimeSinceSeen >= timeBeforeSearch)
                {
                    return true;
                }
                else if (enemy.Heard &&
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
                    _finishedPeek = false;
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
            if (Bot.Memory.Location.BotZoneCollider != null)
            {
                Vector3 closestPointInZone = Bot.Memory.Location.BotZoneCollider.ClosestPointOnBounds(target);
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
            if (!SearchedTargetPosition && (FinalDestination - Bot.Position).sqrMagnitude < 1f)
            {
                SearchedTargetPosition = true;
            }

            if (_nextCheckPosTime < Time.time || SearchedTargetPosition)
            {
                _nextCheckPosTime = Time.time + 4f;

                Vector3? newTarget = SearchMovePos(out bool hasTarget);

                if (newTarget != null
                    && hasTarget
                    && (newTarget.Value - FinalDestination).sqrMagnitude > 2f
                    && CalculatePath(newTarget.Value) != NavMeshPathStatus.PathInvalid)
                {
                    SearchedTargetPosition = false;
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
            //CheckIfStuck();
            SwitchSearchModes(shallSprint);
            SearchMovePoint?.DrawDebug();
        }

        public Enemy SearchTarget { get; set; }

        private float _nextCheckPosTime;

        public bool SearchedTargetPosition { get; private set; }

        public Vector3? SearchMovePos(out bool hasTarget, bool randomSearch = false)
        {
            hasTarget = false;
            var enemy = Bot.Enemy;
            if (enemy != null && (enemy.Seen || enemy.Heard))
            {
                if (enemy.IsVisible)
                {
                    hasTarget = true;
                    return enemy.EnemyPosition;
                }

                var knownPlaces = enemy.KnownPlaces.AllEnemyPlaces;
                for (int i = 0; i < knownPlaces.Count; i++)
                {
                    EnemyPlace enemyPlace = knownPlaces[i];
                    if (enemyPlace != null && 
                        !enemyPlace.HasArrivedPersonal && 
                        !enemyPlace.HasArrivedSquad)
                    {
                        hasTarget = true;
                        return enemyPlace.Position;
                    }
                }
                if (randomSearch)
                {
                    return RandomSearch();
                }
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
            Vector3 start = Bot.Position;
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

        private bool WaitAtPoint()
        {
            if (WaitPointTimer < 0)
            {
                float baseTime = 3;
                var personalitySettings = Bot.Info.PersonalitySettings;
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
                BotOwner.Mover.MovementResume();
                WaitPointTimer = -1;
                return false;
            }
            return true;
        }

        private float WaitPointTimer = -1;

        public Vector3 FinalDestination { get; private set; } = Vector3.zero;

        private bool moveToPoint(Vector3 destination, bool shallSprint)
        {
            if (!shallSprint)
            {
                Bot.Mover.SprintController.CancelRun();
            }

            _Running = false;
            if (shallSprint
                && Bot.Mover.SprintController.RunToPoint(destination, Mover.ESprintUrgency.Middle))
            {
                _Running = true;
                return true;
            }
            else
            {
                Bot.Mover.Sprint(false);
                return Bot.Mover.GoToPoint(destination, out _);
            }
        }

        private bool _Running;
        private bool _setMaxSpeedPose;

        private void handleLight()
        {
            if (_Running || Bot.Mover.SprintController.Running)
            {
                return;
            }
            if (BotOwner.Mover?.IsMoving == true)
            {
                Bot.BotLight.HandleLightForSearch(BotOwner.Mover.DirCurPoint.magnitude);
            }
        }

        private void SwitchSearchModes(bool shallSprint)
        {
            var persSettings = Bot.Info.PersonalitySettings;
            float speed = 1f;
            float pose = 1f;
            _setMaxSpeedPose = false;
            bool shallBeStealthy = Bot.Enemy != null && shallBeStealthyDuringSearch(Bot.Enemy);
            // Environment id of 0 means a bot is outside.
            if (shallSprint || Player.IsSprintEnabled || _Running)
            {
                _setMaxSpeedPose = true;
                speed = 1f;
                pose = 1f;
            }
            else if (!Bot.Memory.Location.IsIndoors)
            {
                if (persSettings.Search.Sneaky && Bot.Cover.CoverPoints.Count > 2 && Time.time - BotOwner.Memory.UnderFireTime > 30f)
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

            if (shallBeStealthy || persSettings.Search.Sneaky)
            {
                Bot.BotLight.ToggleLight(false);
            }
            else
            {
                handleLight();
            }

            if (BotOwner.WeaponManager?.Reload?.Reloading == true && CurrentState != ESearchMove.Wait)
            {
                NextState = CurrentState;
                CurrentState = ESearchMove.Wait;
            }

            if (LastState != CurrentState)
            {
                LastState = CurrentState;
            }

            switch (CurrentState)
            {
                case ESearchMove.None:
                    if (_finishedPeek &&
                        HasPathToSearchTarget(out Vector3 finalDestination, true))
                    {
                        _finishedPeek = false;
                        FinalDestination = finalDestination;
                    }

                    if (!shallSprint && 
                        SearchMovePoint != null && 
                        moveToPoint(SearchMovePoint.PeekStart.Point, shallSprint))
                    {
                        CurrentState = ESearchMove.MoveToStartPeek;
                        return;
                    }

                    if ((shallSprint || 
                        SearchMovePoint == null || 
                        (SearchMovePoint.PeekStart.Used && SearchMovePoint.PeekEnd.Used) || 
                        _finishedPeek)
                        && moveToPoint(FinalDestination, shallSprint))
                    {
                        CurrentState = ESearchMove.DirectMove;
                        return;
                    }

                    return;

                case ESearchMove.DirectMove:

                    setSpeedPose(speed, pose);
                    moveToPoint(FinalDestination, shallSprint);
                    return;

                case ESearchMove.Advance:

                    if (_advanceTime < 0)
                    {
                        _advanceTime = Time.time + 5f;
                    }
                    if (_advanceTime < Time.time)
                    {
                        _advanceTime = -1f;
                        CurrentState = ESearchMove.None;
                        return;
                    }

                    setSpeedPose(speed, pose);
                    moveToPoint(FinalDestination, shallSprint);
                    return;

                case ESearchMove.MoveToStartPeek:

                    PeekPosition start = SearchMovePoint.PeekStart;

                    if (botIsAtPoint(start.Point))
                    {
                        start.Used = true;
                        CurrentState = ESearchMove.MoveToEndPeek;
                        return;
                    }

                    setSpeedPose(speed, pose);
                    moveToPoint(start.Point, shallSprint);
                    return;

                case ESearchMove.MoveToEndPeek:

                    PeekPosition end = SearchMovePoint.PeekEnd;

                    if (botIsAtPoint(end.Point))
                    {
                        end.Used = true;
                        CurrentState = ESearchMove.Wait;
                        NextState = ESearchMove.MoveToDangerPoint;
                        return;
                    }

                    setSpeedPose(0.5f, pose);
                    moveToPoint(end.Point, shallSprint);
                    return;

                case ESearchMove.MoveToDangerPoint:

                    Vector3 danger = SearchMovePoint.DangerPoint;
                    if (botIsAtPoint(danger))
                    {
                        _finishedPeek = true;
                        CurrentState = ESearchMove.Advance;
                        return;
                    }

                    setSpeedPose(speed, pose);
                    moveToPoint(danger, shallSprint);
                    return;

                case ESearchMove.Wait:
                    if (!WaitAtPoint())
                    {
                        CurrentState = NextState;
                        return;
                    }
                    Bot.Mover.SetTargetMoveSpeed(0f);
                    Bot.Mover.SetTargetPose(0.75f);
                    return;
            }
        }

        private void setSpeedPose(float speed, float pose)
        {
            if (_setMaxSpeedPose)
            {
                Bot.Mover.SetTargetMoveSpeed(1f);
                Bot.Mover.SetTargetPose(1f);
            }
            else
            {
                Bot.Mover.SetTargetMoveSpeed(speed);
                Bot.Mover.SetTargetPose(pose);
            }
        }

        private float _advanceTime;
        private bool _finishedPeek;

        private bool CheckIfStuck()
        {
            bool botIsStuck =
                !Bot.BotStuck.BotHasChangedPosition && Bot.BotStuck.TimeSpentNotMoving > 3f
                || Bot.BotStuck.BotIsStuck;

            if (botIsStuck && UnstuckMoveTimer < Time.time)
            {
                UnstuckMoveTimer = Time.time + 2f;

                var TargetPosition = Bot.CurrentTargetPosition;
                if (TargetPosition != null)
                {
                    CalculatePath(TargetPosition.Value, false);
                }
            }
            return botIsStuck;
        }

        public void Reset()
        {
            _finishedPeek = false;
            Path?.ClearCorners();
            FinalDestination = Vector3.zero;
            SearchMovePoint?.DisposeDebug();
            SearchMovePoint = null;
            CurrentState = ESearchMove.None;
            LastState = ESearchMove.None;
            NextState = ESearchMove.None;
            SearchedTargetPosition = false;
        }

        public NavMeshPathStatus CalculatePath(Vector3 point, bool MustHavePath = true, float reachDist = 0.5f)
        {
            Vector3 Start = Bot.Position;
            if ((point - Start).sqrMagnitude <= 0.5f)
            {
                return NavMeshPathStatus.PathInvalid;
            }

            Reset();

            if (NavMesh.SamplePosition(point, out var hit, 5f, -1) && 
                NavMesh.SamplePosition(Start, out var hit2, 1f, -1))
            {
                Path = new NavMeshPath();
                if (NavMesh.CalculatePath(hit2.position, hit.position, -1, Path))
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

                        Vector3 startPeekPos = GetPeekStartAndEnd(A, B, ADirection, BDirection, out var endPeekPos);

                        if (NavMesh.SamplePosition(startPeekPos, out var hit3, 5f, -1))
                        {
                            SearchMovePoint = new MoveDangerPoint(hit3.position, endPeekPos, B, A);
                            break;
                        }
                    }
                }
                return Path.status;
            }

            return NavMeshPathStatus.PathInvalid;
        }

        private Vector3 GetPeekStartAndEnd(Vector3 blindCorner, Vector3 dangerPoint, Vector3 dirToBlindCorner, Vector3 dirToBlindDest, out Vector3 peekEnd)
        {
            const float maxMagnitude = 6f;
            const float minMagnitude = 1f;
            const float OppositePointMagnitude = 3f;

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

            CheckForObstacles(PeekStartPosition, direction, out Vector3 result);
            peekEnd = result;
            return PeekStartPosition;
        }

        private void CheckForObstacles(Vector3 start, Vector3 direction, out Vector3 result)
        {
            if (!NavMesh.SamplePosition(start, out var startHit, 5f, -1))
            {
                result = start + direction;
                return;
            }
            direction.y = 0f;
            if (!NavMesh.Raycast(startHit.position, direction, out var rayHit, -1))
            {
                result = startHit.position + direction;
                if (NavMesh.SamplePosition(result, out var endHit, 5f, -1))
                {
                    result = endHit.position;
                }
                return;
            }
            result = rayHit.position;
        }

        public bool botIsAtPoint(Vector3 point, float reachDist = 0.5f)
        {
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
        private float UnstuckMoveTimer = 0f;

        private NavMeshPath Path = new NavMeshPath();
    }
}