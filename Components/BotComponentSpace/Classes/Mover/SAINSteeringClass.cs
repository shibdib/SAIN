using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.SAINComponent.Classes.WeaponFunction;
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
        private enum ESteerSpeed
        {
            None = 0,
            Enemy = 1,
            Target = 2,
        }

        static SAINSteeringClass()
        {
            aimStatusField = AccessTools.Field(Helpers.HelpersGClass.AimDataType, "aimStatus_0");
        }

        public SAINSteeringClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            getDefaultSpeeds();
        }

        public void Update()
        {
            handleRotateSpeed();
        }

        private void handleRotateSpeed()
        {
            if (Bot.HasEnemy)
            {
                if (_steerStatus != ESteerSpeed.Enemy)
                {
                    _steerStatus = ESteerSpeed.Enemy;
                    var moveSettings = BotOwner.Settings.FileSettings.Move;
                    moveSettings.BASE_ROTATE_SPEED = 240f;
                    moveSettings.FIRST_TURN_SPEED = 215f;
                    moveSettings.FIRST_TURN_BIG_SPEED = 320f;
                }
            }
            else if (Bot.CurrentTargetPosition != null)
            {
                if (_steerStatus != ESteerSpeed.Target)
                {
                    _steerStatus = ESteerSpeed.Target;
                    var moveSettings = BotOwner.Settings.FileSettings.Move;
                    moveSettings.BASE_ROTATE_SPEED = 200f;
                    moveSettings.FIRST_TURN_SPEED = 180f;
                    moveSettings.FIRST_TURN_BIG_SPEED = 280f;
                }
            }
            else
            {
                if (_steerStatus != ESteerSpeed.None)
                {
                    _steerStatus = ESteerSpeed.None;
                    var moveSettings = BotOwner.Settings.FileSettings.Move;
                    moveSettings.BASE_ROTATE_SPEED = _baseSpeed;
                    moveSettings.FIRST_TURN_SPEED = _firstTurnSpeed;
                    moveSettings.FIRST_TURN_BIG_SPEED = _firstTurnBigSpeed;
                }
            }
        }

        private ESteerSpeed _steerStatus;

        private void getDefaultSpeeds()
        {
            var moveSettings = BotOwner.Settings.FileSettings.Move;
            _baseSpeed = moveSettings.BASE_ROTATE_SPEED;
            _firstTurnSpeed = moveSettings.FIRST_TURN_SPEED;
            _firstTurnBigSpeed = moveSettings.FIRST_TURN_BIG_SPEED;
        }

        private float _baseSpeed;
        private float _firstTurnSpeed;
        private float _firstTurnBigSpeed;

        public void SetAimTarget(Vector3? target)
        {
            var aimData = BotOwner.AimingData;
            if (aimData != null)
            {
                if (target == null)
                {
                    aimData.LoseTarget();
                }
                else
                {
                    aimData.SetTarget(target.Value);
                    aimData.NodeUpdate();
                }
            }
        }

        public AimStatus AimStatus
        {
            get
            {
                object aimStatus = aimStatusField.GetValue(BotOwner.AimingData);
                if (aimStatus == null)
                {
                    return AimStatus.NoTarget;
                }
                return (AimStatus)aimStatus;
            }
        }

        private static FieldInfo aimStatusField;

        public void Dispose()
        {
        }

        private float dotProductFromLookDir(Vector3 toPoint)
        {
            Vector3 directionOfTarget = (toPoint - Bot.Position).normalized;
            Vector3 lookDirection = Player.LookDirection.normalized;
            return Vector3.Dot(directionOfTarget, lookDirection);
        }

        private float angleFromLookDir(Vector3 toPoint)
        {
            Vector3 directionOfTarget = (toPoint - Bot.Position).normalized;
            Vector3 lookDirection = Player.LookDirection.normalized;
            return Vector3.Angle(directionOfTarget, lookDirection);
        }

        private bool SteerRandomToggle;
        private PlaceForCheck LastHeardSound;

        private void lookToAimTarget()
        {
            Vector3? target = BotOwner.AimingData?.EndTargetPoint;
            if (target == null)
            {
                target = BotOwner.AimingData?.RealTargetPoint;
            }
            if (target != null)
            {
                if (angleFromLookDir(target.Value) < 10)
                {
                    //BotOwner.AimingData?.NodeUpdate();
                }
                else
                {
                    //LookToPoint(target.Value);
                }
            }
        }

        public bool SteerByPriority(bool lookRandom = true)
        {
            SteerRandomToggle = lookRandom;

            var lastPriority = CurrentSteerPriority;
            CurrentSteerPriority = FindSteerPriority();

            if (CurrentSteerPriority != lastPriority)
            {
                LastSteerPriority = lastPriority;
            }

            switch (CurrentSteerPriority)
            {
                case SteerPriority.RunningPath:
                    // Running if handled inside the sprint coroutine
                    break;

                case SteerPriority.ManualShooting:
                    Vector3 shootPos = Bot.ManualShoot.ShootPosition;
                    if (shootPos != Vector3.zero)
                    {
                        LookToPoint(shootPos);
                    }
                    break;

                case SteerPriority.Aiming:
                case SteerPriority.Shooting:
                    lookToAimTarget();
                    break;

                case SteerPriority.EnemyVisible:
                    LookToEnemy();
                    break;

                case SteerPriority.UnderFire:
                    LookToUnderFirePos();
                    break;

                case SteerPriority.LastHit:
                    LookToLastHitPos();
                    break;

                case SteerPriority.LastSeenEnemy:
                case SteerPriority.LastSeenEnemyLong:
                case SteerPriority.EnemyLastKnownLocation:
                    LookToLastKnownEnemyPosition(Bot.Enemy);
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

                case SteerPriority.Random:
                    LookToRandomPosition();
                    break;

                default:
                    break;
            }

            return CurrentSteerPriority != SteerPriority.None;
        }

        private void HeardSoundSanityCheck()
        {
            if (CurrentSteerPriority == SteerPriority.Hear && LastHeardSound == null)
            {
                if (SAINPlugin.DebugMode)
                {
                    Logger.LogDebug("Bot was told to steer toward something they heard, but the place to check is null.");
                }
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
            if (Bot.Mover.SprintController.Running)
            {
                return SteerPriority.RunningPath;
            }
            if (Player.IsSprintEnabled)
            {
                return SteerPriority.Sprinting;
            }
            if (Bot.ManualShoot.Reason != EShootReason.None 
                && Bot.ManualShoot.ShootPosition != Vector3.zero)
            {
                return SteerPriority.ManualShooting;
            }
            if (LookToAimTarget())
            {
                return SteerPriority.Aiming;
            }
            if (BotOwner.Memory.IsUnderFire)
            {
                return SteerPriority.UnderFire;
            }
            if (EnemyVisible())
            {
                return SteerPriority.EnemyVisible;
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
            EnemyPlace lastKnownPlace = Bot.Enemy?.KnownPlaces?.LastKnownPlace;
            if (lastKnownPlace != null 
                && lastKnownPlace.TimeSincePositionUpdated < Steer_TimeSinceLocationKnown_Threshold)
            {
                return SteerPriority.EnemyLastKnownLocation;
            }
            if (Bot.Enemy?.TimeSinceSeen < Steer_TimeSinceSeen_Short && Bot.Enemy.Seen)
            {
                return SteerPriority.LastSeenEnemy;
            }
            LastHeardSound = BotOwner.BotsGroup.YoungestFastPlace(BotOwner, Steer_HeardSound_Dist, Steer_HeardSound_Age);
            if (LastHeardSound != null)
            {
                return SteerPriority.Hear;
            }
            if (Bot.Enemy?.TimeSinceSeen < Steer_TimeSinceSeen_Long && Bot.Enemy.Seen)
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
            foreach (var enemy in Bot.EnemyController.Enemies.Values)
            {
                if (enemy?.IsValid == true && 
                    (enemy.EnemyStatus.ShotByEnemyRecently || enemy.EnemyStatus.ShotAtMeRecently))
                {
                    return enemy;
                }
            }
            return null;
        }


        public SteerPriority CurrentSteerPriority { get; private set; } = SteerPriority.None;
        public SteerPriority LastSteerPriority { get; private set; } = SteerPriority.None;

        public bool LookToLastKnownEnemyPosition(SAINEnemy enemy)
        {
            if (enemy != null)
            {
                if (enemy.IsVisible)
                {
                    LookToEnemy(enemy);
                    return true;
                }

                EnemyPlace lastKnownPlace = enemy.KnownPlaces.LastKnownPlace;
                if (lastKnownPlace?.PersonalClearLineOfSight(BotOwner.LookSensor._headPoint, LayerMaskClass.HighPolyWithTerrainMask) == true)
                {
                    LookToPoint(lastKnownPlace.Position + _weaponRootOffset);
                    return true;
                }

                Vector3? blindCornerToEnemy = enemy.Path.BlindCornerToEnemy;
                if (blindCornerToEnemy != null &&
                    (blindCornerToEnemy.Value - Bot.Transform.HeadPosition).sqrMagnitude > 1.5f)
                {
                    LookToPoint(blindCornerToEnemy.Value);
                    return true;
                }

                Vector3? lastCorner = enemy.Path.LastCornerToEnemy;
                if (lastCorner != null &&
                    enemy.CanSeeLastCornerToEnemy)
                {
                    LookToPoint(lastCorner.Value + _weaponRootOffset);
                    return true;
                }

                var enemyPath = enemy.Path.PathToEnemy;
                if (enemyPath != null && enemyPath.corners.Length > 2)
                {
                    Vector3 point = enemyPath.corners[1];
                    LookToPoint(point + _weaponRootOffset);
                    return true;
                }

                if (lastKnownPlace != null)
                {
                    LookToPoint(lastKnownPlace.Position + _weaponRootOffset);
                    return true;
                }
            }

            return false;
        }

        private Vector3 _weaponRootOffset => BotOwner.WeaponRoot.position - Bot.Position;

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
                rotateSpeed = Bot.HasEnemy ? baseTurnSpeed : baseTurnSpeedNoEnemy;
            }
            BotOwner.Steering.LookToDirection(direction, rotateSpeed);
        }

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
            SAINEnemy enemy = Bot.Enemy;

            if (enemy != null)
            {
                if (enemy.IsVisible)
                {
                    return true;
                }

                if (enemy.Seen &&
                    enemy.InLineOfSight &&
                    enemy.TimeSinceSeen < 0.5f)
                {
                    return true;
                }
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
            LookToEnemy(Bot.Enemy);
        }

        public void LookToUnderFirePos()
        {
            LookToPoint(Bot.Memory.UnderFireFromPosition + _weaponRootOffset);
        }

        public void LookToHearPos(Vector3 soundPos, bool visionCheck = false)
        {
            float turnSpeed = Bot.HasEnemy ? baseTurnSpeed : baseTurnSpeedNoEnemy;
            if ((soundPos - Bot.Position).sqrMagnitude > 125f.Sqr())
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
                if (NavMesh.CalculatePath(Bot.Position, soundPos, -1, HearPath))
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
                Vector3 newRandomPos = findRandomLookPos(out bool isRandom);
                if (newRandomPos != Vector3.zero)
                {
                    float baseTime = isRandom ? 2f : 4f;
                    RandomLookTimer = Time.time + baseTime * Random.Range(0.66f, 1.33f);
                    randomLookPosition = newRandomPos;
                }
            }
        }

        private Vector3 findRandomLookPos(out bool isRandomLook)
        {
            isRandomLook = false;
            Vector3 randomLookPosition = Vector3.zero;
            if (_lookRandomToggle)
            {
                randomLookPosition = generateRandomLookPos();
                if (randomLookPosition != Vector3.zero)
                {
                    isRandomLook = true;
                    return randomLookPosition;
                }
            }
            Vector3? blindCorner = Bot.Enemy?.Path.BlindCornerToEnemy;
            if (blindCorner != null)
            {
                return blindCorner.Value;
            }
            Vector3? lastCorner = Bot.Enemy?.Path.LastCornerToEnemy;
            if (lastCorner != null)
            {
                return lastCorner.Value;
            }
            Vector3? lastKnown = Bot.Enemy?.LastKnownPosition;
            if (lastCorner != null)
            {
                return lastCorner.Value;
            }
            return Vector3.zero;
        }

        private Vector3 generateRandomLookPos()
        {
            var Mask = LayerMaskClass.HighPolyWithTerrainMask;
            var headPos = Bot.Transform.HeadPosition;

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
            var enemy = Bot.Enemy;
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
        Aiming,
        EnemyVisible,
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