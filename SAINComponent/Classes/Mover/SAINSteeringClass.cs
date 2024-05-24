using EFT;
using HarmonyLib;
using SAIN.SAINComponent.Classes.Enemy;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using static RootMotion.FinalIK.AimPoser;
using static UnityEngine.UI.GridLayoutGroup;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINSteeringClass : SAINBase, ISAINClass
    {
        public SAINSteeringClass(Bot sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            var moveSettings = BotOwner.Settings.FileSettings.Move;
            if (SAINBot.HasEnemy)
            {
                moveSettings.BASE_ROTATE_SPEED = 240f;
                moveSettings.FIRST_TURN_SPEED = 215f;
                moveSettings.FIRST_TURN_BIG_SPEED = 320f;
            }
            else if (SAINBot.CurrentTargetPosition != null)
            {
                moveSettings.BASE_ROTATE_SPEED = 200f;
                moveSettings.FIRST_TURN_SPEED = 180f;
                moveSettings.FIRST_TURN_BIG_SPEED = 280f;
            }
            else
            {
                moveSettings.BASE_ROTATE_SPEED = 180f;
                moveSettings.FIRST_TURN_SPEED = 150f;
                moveSettings.FIRST_TURN_BIG_SPEED = 240f;
            }
            if (SAINBot.CurrentTargetPosition != null)
            {
                updateLookDirection();
            }
        }

        public void SetAimTarget(Vector3? target)
        {
            var aimData = BotOwner.AimingData;
            if (aimData == null)
            {
                return;
            }
            if (target == null)
            {
                aimData.LoseTarget();
                return;
            }
            aimData.SetTarget(target.Value); 
            aimData.NodeUpdate();
        }

        public AimStatus AimStatus
        {
            get
            {
                if (aimStatusField == null)
                {
                    aimStatusField = AccessTools.Field(Helpers.HelpersGClass.AimDataType, "aimStatus_0");
                }

                object aimStatus = aimStatusField.GetValue(BotOwner.AimingData);
                if (aimStatus == null)
                {
                    return AimStatus.NoTarget;
                }
                return (AimStatus)aimStatus;
            }
        }

        private static FieldInfo aimStatusField;

        private float updateSteerTimer;

        private SteerPriority UpdateBotSteering(bool skipTimer = false)
        {
            if (skipTimer || updateSteerTimer < Time.time)
            {
                var lastPriority = CurrentSteerPriority;
                CurrentSteerPriority = FindSteerPriority();
                if (CurrentSteerPriority != lastPriority)
                {
                    LastSteerPriority = lastPriority;
                }

                updateSteerTimer = Time.time + 0.05f;
            }
            return CurrentSteerPriority;
        }

        public void Dispose()
        {
        }

        private bool SteerRandomToggle;
        private PlaceForCheck LastHeardSound;

        public bool SteerByPriority(bool lookRandom = true)
        {
            SteerRandomToggle = lookRandom;

            var priority = UpdateBotSteering();

            HeardSoundSanityCheck();

            switch (priority)
            {
                case SteerPriority.None: 
                    if (SAINBot.HasEnemy && SAINBot.Enemy.Path.LastCornerToEnemy != null)
                    {
                        LookToPathToEnemy();
                    }
                    else if (SAINBot.CurrentTargetPosition != null)
                    {
                        LookToPoint(SAINBot.CurrentTargetPosition);
                    }
                    break;

                case SteerPriority.RunningPath:
                    // Running if handled inside the sprint coroutine
                    break;

                case SteerPriority.ManualShooting:
                    if (SAINBot.ManualShootTargetPosition != Vector3.zero)
                    {
                        LookToPoint(SAINBot.ManualShootTargetPosition);
                    }
                    break;

                case SteerPriority.Shooting:
                    Vector3? currentTarget = SAINBot.CurrentTargetPosition;
                    if (currentTarget != null)
                    {
                        Vector3 directionOfTarget = (currentTarget.Value - SAINBot.Position).normalized;
                        Vector3 lookDirection = Player.LookDirection.normalized;
                        if (Vector3.Dot(directionOfTarget, lookDirection) < 0.8f)
                        {
                            LookToPoint(currentTarget.Value);
                            break;
                        }
                    }
                    BotOwner.AimingData?.NodeUpdate();
                    // Steering is handled by aim code in eft manually, so do nothing here.
                    break;

                case SteerPriority.Enemy:
                    LookToEnemy();
                    break;

                case SteerPriority.UnderFire:
                    LookToUnderFirePos();
                    break;

                case SteerPriority.LastHit:
                    LookToLastHitPos();
                    break;

                case SteerPriority.EnemyLastKnownLocation:
                    LookToLastKnownEnemyPosition();
                    break;
                case SteerPriority.LastSeenEnemy:
                case SteerPriority.LastSeenEnemyLong:
                    LookToPathToEnemy();
                    break;

                case SteerPriority.Hear:
                    if (LastHeardSound != null)
                    {
                        LookToHearPos(LastHeardSound.Position);
                    }
                    else if (SAINPlugin.DebugMode)
                    {
                        Logger.LogError("Cannot look toward null PlaceForCheck.");
                    }
                    break;

                case SteerPriority.Sprinting:
                    LookToMovingDirection(400);
                    break;

                case SteerPriority.MoveDirection:
                    LookToMovingDirection(240f);
                    break;

                case SteerPriority.Search:
                    // Search steering is handled in the Search Layer
                    break;

                case SteerPriority.Random:
                    LookToRandomPosition();
                    break;
            }

            return CurrentSteerPriority != SteerPriority.None && CurrentSteerPriority != SteerPriority.Random;
        }

        private void HeardSoundSanityCheck()
        {
            if (CurrentSteerPriority == SteerPriority.Hear && LastHeardSound == null)
            {
                if (SAINPlugin.DebugMode)
                {
                    Logger.LogDebug("Bot was told to steer toward something they heard, but the place to check is null.");
                }
                UpdateBotSteering(true);
            }
        }

        // How long a bot will look at where they last saw an enemy instead of something they hear
        private readonly float Steer_TimeSinceLocationKnown_Threshold = 4f;
        // How long a bot will look at where they last saw an enemy instead of something they hear
        private readonly float Steer_TimeSinceSeen_Short = 4f;
        // How long a bot will look at where they last saw an enemy if they don't hear any other threats
        private readonly float Steer_TimeSinceSeen_Long = 10f;
        // How far a sound can be for them to react by looking toward it.
        private readonly float Steer_HeardSound_Dist = 80f;
        // How old a sound can be, in seconds, for them to react by looking toward it.
        private readonly float Steer_HeardSound_Age = 2f;

        public SteerPriority FindSteerPriority()
        {
            // return values are ordered by priority, so the targets get less "important" as they descend down this function.
            if (SAINBot.Mover.SprintController.Running)
            {
                //return SteerPriority.RunningPath;
            }
            if (Player.IsSprintEnabled)
            {
                return SteerPriority.Sprinting;
            }
            if (SAINBot.ManualShootReason != Bot.EShootReason.None 
                && SAINBot.ManualShootTargetPosition != Vector3.zero)
            {
                return SteerPriority.ManualShooting;
            }
            if (LookToAimTarget())
            {
                return SteerPriority.Shooting;
            }
            if (BotOwner.Memory.IsUnderFire)
            {
                return SteerPriority.UnderFire;
            }
            if (EnemyVisible())
            {
                return SteerPriority.Enemy;
            }
            var shotMeRecent = enemyShotMe();
            if (shotMeRecent != null)
            {
                return SteerPriority.LastHit;
            }
            //LastHeardSound = BotOwner.BotsGroup.YoungestFastPlace(BotOwner, 100f, 4f);
            if (LastHeardSound != null)
            {
                //return SteerPriority.Hear;
            }
            EnemyPlace lastKnownPlace = SAINBot.Enemy?.KnownPlaces?.LastKnownPlace;
            if (lastKnownPlace != null 
                && lastKnownPlace.TimeSincePositionUpdated < Steer_TimeSinceLocationKnown_Threshold)
            {
                return SteerPriority.EnemyLastKnownLocation;
            }
            if (SAINBot.Enemy?.TimeSinceSeen < Steer_TimeSinceSeen_Short && SAINBot.Enemy.Seen)
            {
                return SteerPriority.LastSeenEnemy;
            }
            LastHeardSound = BotOwner.BotsGroup.YoungestFastPlace(BotOwner, Steer_HeardSound_Dist, Steer_HeardSound_Age);
            if (LastHeardSound != null)
            {
                return SteerPriority.Hear;
            }
            if (SAINBot.Enemy?.TimeSinceSeen < Steer_TimeSinceSeen_Long && SAINBot.Enemy.Seen)
            {
                return SteerPriority.LastSeenEnemyLong;
            }
            if (SteerRandomToggle)
            {
                return SteerPriority.Random;
            }
            return SteerPriority.None;
        }

        private SAINEnemy enemyShotMe()
        {
            foreach (var enemy in SAINBot.EnemyController.Enemies.Values)
            {
                if (enemy?.IsValid == true && enemy.ShotByEnemyRecently)
                {
                    return enemy;
                }
            }
            return null;
        }


        public SteerPriority CurrentSteerPriority { get; private set; } = SteerPriority.None;
        public SteerPriority LastSteerPriority { get; private set; } = SteerPriority.None;

        public bool LookToLastKnownEnemyPosition()
        {
            SAINEnemy enemy = SAINBot.Enemy;
            if (enemy == null)
            {
                return false;
            }
            if (enemy.IsVisible)
            {
                LookToEnemy(enemy);
                return false;
            }

            EnemyPlace lastKnownPlace = enemy.KnownPlaces.LastKnownPlace;
            if (lastKnownPlace != null)
            {
                if (lastKnownPlace.PersonalClearLineOfSight(BotOwner.LookSensor._headPoint, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    LookToPoint(lastKnownPlace.Position + _weaponRootOffset);
                    return true;
                }
                Vector3? blindCornerToEnemy = enemy.Path.BlindCornerToEnemy;
                if (blindCornerToEnemy != null && (blindCornerToEnemy.Value - SAINBot.Transform.HeadPosition).sqrMagnitude > 1.5f)
                {
                    LookToPoint(blindCornerToEnemy.Value);
                    return true;
                }
                LookToPoint(lastKnownPlace.Position + _weaponRootOffset);
                return true;
            }
            return false;
        }

        private Vector3 _weaponRootOffset => BotOwner.WeaponRoot.position - SAINBot.Position;

        public void LookToMovingDirection(float rotateSpeed = 150f, bool sprint = false, bool forceSteer = false)
        {
            if (SteeringLocked && !forceSteer)
            {
                return;
            }
            if (sprint || Player.IsSprintEnabled)
            {
                BotOwner.Steering.LookToMovingDirection(500f);
            }
            else
            {
                BotOwner.Steering.LookToMovingDirection(rotateSpeed);
            }
        }

        public bool SteeringLocked => SteerLockTime > Time.time;
        public float SteerLockTime { get; private set; }

        public void LockSteering(float duration)
        {
            SteerLockTime = Time.time + duration;
        }

        public void LookToPoint(Vector3 point, float rotateSpeed = -1, bool forceSteer = false)
        {
            if (SteeringLocked && !forceSteer)
            {
                return;
            }
            Vector3 direction = point - BotOwner.WeaponRoot.position;
            if (direction.magnitude < 1f)
            {
                direction = direction.normalized;
                //direction.y = 0f;
            }
            if (rotateSpeed < 0)
            {
                rotateSpeed = SAINBot.HasEnemy ? baseTurnSpeed : baseTurnSpeedNoEnemy;
            }
            _targetLookDir = direction;
            _targetLookSpeed = rotateSpeed;
        }

        private void updateLookDirection()
        {
            if (_updateLookTime < Time.time && _targetLookDir != Vector3.zero)
            {
                //_updateLookTime = Time.time + 0.1f;
                sendLookCommand(_targetLookDir, _targetLookSpeed);
            }
        }

        private void sendLookCommand(Vector3 direction, float speed)
        {
            BotOwner.Steering.LookToDirection(direction, speed);
        }

        private Vector3 _targetLookDir;
        private float _targetLookSpeed;
        private float _updateLookTime;

        public void LookToPoint(Vector3? point, float rotateSpeed = -1)
        {
            if (point != null)
            {
                LookToPoint(point.Value, rotateSpeed);
            }
        }

        public void LookToDirection(Vector3 direction, bool flat, float rotateSpeed = -1f)
        {
            if (flat)
            {
                direction.y = 0f;
            }
            Vector3 pos = BotOwner.WeaponRoot.position + direction.normalized;
            LookToPoint(pos, rotateSpeed);
        }

        public bool LookToAimTarget()
        {
            if (BotOwner.WeaponManager.Reload?.Reloading == true)
            {
                return false;
            }
            return AimStatus != AimStatus.NoTarget;
        }

        public bool EnemyVisible()
        {
            SAINEnemy enemy = SAINBot.Enemy;

            if (enemy != null 
                && enemy.IsVisible)
            {
                return true;
            }

            if (enemy != null 
                && enemy.InLineOfSight 
                && enemy.TimeSinceSeen < 0.5f)
            {
                return true;
            }

            return false;
        }

        public void LookToEnemy(SAINEnemy enemy)
        {
            if (enemy != null)
            {
                LookToPoint(enemy.EnemyPosition + _weaponRootOffset);
            }
        }

        public void LookToEnemy()
        {
            LookToEnemy(SAINBot.Enemy);
        }

        public void LookToUnderFirePos()
        {
            var pos = SAINBot.Memory.UnderFireFromPosition;
            pos.y += 1f;
            LookToPoint(pos, baseTurnSpeed);
        }

        public void LookToHearPos(Vector3 soundPos, bool visionCheck = false)
        {
            float turnSpeed = SAINBot.HasEnemy ? baseTurnSpeed : baseTurnSpeedNoEnemy;
            if ((soundPos - SAINBot.Position).sqrMagnitude > 100f * 100f)
            {
                LookToPoint(soundPos, turnSpeed);
                return;
            }

            if (HearPath == null)
            {
                HearPath = new NavMeshPath();
            }
            if (LastSoundTimer < Time.time || (LastSoundCheckPos - soundPos).magnitude > 1f)
            {
                LastSoundTimer = Time.time + 0.5f;
                LastSoundCheckPos = soundPos;
                LastSoundHeardCorner = Vector3.zero;

                HearPath.ClearCorners();
                if (NavMesh.CalculatePath(SAINBot.Position, soundPos, -1, HearPath))
                {
                    if (HearPath.corners.Length > 2)
                    {
                        for (int i = HearPath.corners.Length - 1; i >= 0; i--)
                        {
                            Vector3 corner = HearPath.corners[i] + Vector3.up;
                            Vector3 headPos = BotOwner.LookSensor._headPoint;
                            Vector3 cornerDir = corner - headPos;
                            if (!Physics.Raycast(headPos, cornerDir.normalized, cornerDir.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                            {
                                LastSoundHeardCorner = corner;
                                break;
                            }
                        }
                    }
                }
            }

            if (LastSoundHeardCorner != Vector3.zero)
            {
                LookToPoint(LastSoundHeardCorner, turnSpeed);
            }
            else
            {
                LookToPoint(soundPos, turnSpeed);
            }
        }

        private float LastSoundTimer;
        private Vector3 LastSoundCheckPos;
        private Vector3 LastSoundHeardCorner;
        private NavMeshPath HearPath;

        public void LookToLastHitPos()
        {
            var shotMeRecent = enemyShotMe();
            if (shotMeRecent != null)
            {
                Vector3? lastKnown = shotMeRecent.LastKnownPosition;
                if (lastKnown != null)
                {
                    LookToPoint(lastKnown.Value + _weaponRootOffset);
                    return;
                }
            }
            LookToRandomPosition();
        }

        public float baseTurnSpeed = 220f;
        public float baseTurnSpeedNoEnemy = 100f;

        private bool _lookRandomToggle;

        public void LookToRandomPosition()
        {
            updateRandomLook();

            if (randomLookPosition != Vector3.zero)
            {
                LookToPoint(randomLookPosition, Random.Range(60f, 120f));
            }
            else
            {
                LookToMovingDirection();
            }
        }

        private void updateRandomLook()
        {
            if (RandomLookTimer < Time.time)
            {
                _lookRandomToggle = !_lookRandomToggle;
                Vector3 newRandomPos = findRandomLookPos();
                if (newRandomPos != Vector3.zero)
                {
                    RandomLookTimer = Time.time + 2f * Random.Range(0.66f, 1.33f);
                    randomLookPosition = newRandomPos;
                }
            }
        }

        private Vector3 findRandomLookPos()
        {
            Vector3 randomLookPosition = Vector3.zero;
            if (_lookRandomToggle)
            {
                randomLookPosition = generateRandomLookPos();
                if (randomLookPosition != Vector3.zero)
                {
                    return randomLookPosition;
                }
            }
            Vector3? blindCorner = SAINBot.Enemy?.Path.BlindCornerToEnemy;
            if (blindCorner != null)
            {
                return blindCorner.Value;
            }
            Vector3? lastCorner = SAINBot.Enemy?.Path.LastCornerToEnemy;
            if (lastCorner != null)
            {
                return lastCorner.Value;
            }
            Vector3? lastKnown = SAINBot.Enemy?.LastKnownPosition;
            if (lastCorner != null)
            {
                return lastCorner.Value;
            }
            return Vector3.zero;
        }

        private Vector3 generateRandomLookPos()
        {
            var Mask = LayerMaskClass.HighPolyWithTerrainMask;
            var headPos = SAINBot.Transform.HeadPosition;

            float pointDistance = 0f;
            Vector3 result = Vector3.zero;
            for (int i = 0; i < 5; i++)
            {
                var random = Random.onUnitSphere * 5f;
                random.y = 0f;
                if (!Physics.Raycast(headPos, random, out var hit, 8f, Mask))
                {
                    result = random + headPos;
                    break;
                }
                else if (hit.distance > pointDistance)
                {
                    pointDistance = hit.distance;
                    result = hit.point;
                }
            }
            return result;
        }

        private Vector3 _headPoint => BotOwner.LookSensor._headPoint;

        private IEnumerator generateRandomPlaceToLook()
        {
            var Mask = LayerMaskClass.HighPolyWithTerrainMask;
            yield return null;
        }

        private Vector3 randomLookPosition;

        public bool LookToPathToEnemy()
        {
            var enemy = SAINBot.Enemy;
            if (enemy != null && enemy.Path.LastCornerToEnemy != null)
            {
                LookToPoint(enemy.Path.LastCornerToEnemy.Value + _weaponRootOffset);
                return true;
            }
            return false;
        }

        private float RandomLookTimer = 0f;
    }

    public enum SteerPriority
    {
        None,
        Shooting,
        ManualShooting,
        Enemy,
        Hear,
        LastSeenEnemy,
        LastSeenEnemyLong,
        Random,
        LastHit,
        UnderFire,
        MoveDirection,
        Sprinting,
        EnemyLastKnownLocation,
        ClosestHeardEnemy,
        Search,
        RunningPath,
    }
}