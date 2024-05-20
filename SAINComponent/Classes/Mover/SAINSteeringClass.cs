using EFT;
using HarmonyLib;
using SAIN.SAINComponent.Classes.Enemy;
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
        public SAINSteeringClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            var moveSettings = BotOwner.Settings.FileSettings.Move;
            if (SAIN.HasEnemy)
            {
                moveSettings.BASE_ROTATE_SPEED = 240f;
                moveSettings.FIRST_TURN_SPEED = 215f;
                moveSettings.FIRST_TURN_BIG_SPEED = 320f;
            }
            else if (SAIN.CurrentTargetPosition != null)
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

            UpdateBotSteering();

            HeardSoundSanityCheck();

            switch (CurrentSteerPriority)
            {
                case SteerPriority.None: 
                    if (SAIN.HasEnemy && SAIN.Enemy.Path.LastCornerToEnemy != null)
                    {
                        LookToPathToEnemy();
                    }
                    else if (SAIN.CurrentTargetPosition != null)
                    {
                        LookToPoint(SAIN.CurrentTargetPosition);
                    }
                    break;

                case SteerPriority.ManualShooting:
                    if (SAIN.ManualShootTargetPosition != Vector3.zero)
                    {
                        LookToPoint(SAIN.ManualShootTargetPosition);
                    }
                    break;

                case SteerPriority.Shooting:
                    Vector3? currentTarget = SAIN.CurrentTargetPosition;
                    if (currentTarget != null)
                    {
                        Vector3 directionOfTarget = (currentTarget.Value - SAIN.Position).normalized;
                        Vector3 lookDirection = Player.LookDirection.normalized;
                        if (Vector3.Dot(directionOfTarget, lookDirection) < 0.8f)
                        {
                            LookToDirection(directionOfTarget, false);
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

        // How long a bot will look in the direction they were shot from instead of other places
        private readonly float Steer_LastHitTime = 1f;
        // How long a bot will look at where they last saw an enemy instead of something they hear
        private readonly float Steer_TimeSinceLocationKnown_Threshold = 8f;
        // How long a bot will look at where they last saw an enemy instead of something they hear
        private readonly float Steer_TimeSinceSeen_Short = 6f;
        // How long a bot will look at where they last saw an enemy if they don't hear any other threats
        private readonly float Steer_TimeSinceSeen_Long = 30f;
        // How far a sound can be for them to react by looking toward it.
        private readonly float Steer_HeardSound_Dist = 500f;
        // How old a sound can be, in seconds, for them to react by looking toward it.
        private readonly float Steer_HeardSound_Age = 8f;

        public SteerPriority FindSteerPriority()
        {
            // return values are ordered by priority, so the targets get less "important" as they descend down this function.
            if (SAIN.Mover.IsSprinting || Player.IsSprintEnabled)
            {
                return SteerPriority.Sprinting;
            }
            if (SAIN.ManualShootReason != SAINComponentClass.EShootReason.None 
                && SAIN.ManualShootTargetPosition != Vector3.zero)
            {
                return SteerPriority.ManualShooting;
            }
            if (LookToAimTarget())
            {
                return SteerPriority.Shooting;
            }
            if (EnemyVisible())
            {
                return SteerPriority.Enemy;
            }
            if (BotOwner.Memory.IsUnderFire)
            {
                return SteerPriority.UnderFire;
            }
            if (Time.time - BotOwner.Memory.LastTimeHit < Steer_LastHitTime)
            {
                return SteerPriority.LastHit;
            }
            LastHeardSound = BotOwner.BotsGroup.YoungestFastPlace(BotOwner, 100f, 4f);
            if (LastHeardSound != null)
            {
                return SteerPriority.Hear;
            }
            EnemyPlace lastKnownPlace = SAIN.Enemy?.KnownPlaces?.LastKnownPlace;
            if (lastKnownPlace != null 
                && lastKnownPlace.TimeSincePositionUpdated < Steer_TimeSinceLocationKnown_Threshold)
            {
                return SteerPriority.EnemyLastKnownLocation;
            }
            if (SAIN.Enemy?.TimeSinceSeen < Steer_TimeSinceSeen_Short && SAIN.Enemy.Seen)
            {
                return SteerPriority.LastSeenEnemy;
            }
            LastHeardSound = BotOwner.BotsGroup.YoungestFastPlace(BotOwner, Steer_HeardSound_Dist, Steer_HeardSound_Age);
            if (LastHeardSound != null)
            {
                return SteerPriority.Hear;
            }
            if (SAIN.Enemy?.TimeSinceSeen < Steer_TimeSinceSeen_Long && SAIN.Enemy.Seen)
            {
                return SteerPriority.LastSeenEnemyLong;
            }
            if (SteerRandomToggle)
            {
                return SteerPriority.Random;
            }
            return SteerPriority.None;
        }


        public SteerPriority CurrentSteerPriority { get; private set; } = SteerPriority.None;
        public SteerPriority LastSteerPriority { get; private set; } = SteerPriority.None;

        public bool LookToLastKnownEnemyPosition()
        {
            SAINEnemy enemy = SAIN.Enemy;
            if (enemy == null || enemy.IsVisible)
            {
                return false;
            }

            EnemyPlace lastKnownPlace = enemy.KnownPlaces.LastKnownPlace;
            if (lastKnownPlace != null)
            {
                Vector3? blindCornerToEnemy = enemy.Path.BlindCornerToEnemy;
                if (blindCornerToEnemy != null && (blindCornerToEnemy.Value - SAIN.Transform.HeadPosition).sqrMagnitude > 1f)
                {
                    LookToPoint(blindCornerToEnemy.Value);
                    return true;
                }
                LookToPoint(lastKnownPlace.Position);
                return true;
            }
            return false;
        }

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
            if (rotateSpeed < 0)
            {
                BotOwner.Steering.LookToPoint(point, SAIN.HasEnemy ? baseTurnSpeed : baseTurnSpeedNoEnemy);
            }
            else
            {
                BotOwner.Steering.LookToPoint(point, rotateSpeed);
            }
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
            Vector3 pos = SAIN.Transform.HeadPosition + direction;
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
            SAINEnemy enemy = SAIN.Enemy;

            if (enemy != null 
                && enemy.IsVisible)
            {
                return true;
            }

            if (enemy != null 
                && enemy.InLineOfSight 
                && enemy.TimeSinceSeen < 1f)
            {
                return true;
            }

            return false;
        }

        public void LookToEnemy(SAINEnemy enemy)
        {
            if (enemy != null)
            {
                LookToPoint(enemy.EnemyPosition + Vector3.up);
            }
        }

        public void LookToEnemy()
        {
            LookToEnemy(SAIN.Enemy);
        }

        public void LookToUnderFirePos()
        {
            var pos = SAIN.Memory.UnderFireFromPosition;
            pos.y += 1f;
            LookToPoint(pos, baseTurnSpeed);
        }

        public void LookToHearPos(Vector3 soundPos, bool visionCheck = false)
        {
            float turnSpeed = SAIN.HasEnemy ? baseTurnSpeed : baseTurnSpeedNoEnemy;
            if ((soundPos - SAIN.Position).sqrMagnitude > 100f * 100f)
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
                LastSoundTimer = Time.time + 1f;
                LastSoundCheckPos = soundPos;
                LastSoundHeardCorner = Vector3.zero;

                HearPath.ClearCorners();
                if (NavMesh.CalculatePath(SAIN.Position, soundPos, -1, HearPath))
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
            var pos = BotOwner.Memory.LastHitPos + Vector3.up;
            LookToPoint(pos, baseTurnSpeed);
        }

        public float baseTurnSpeed = 220f;
        public float baseTurnSpeedNoEnemy = 100f;

        private bool LookRandom;

        public void LookToRandomPosition()
        {
            if (RandomLookTimer < Time.time)
            {
                RandomLookTimer = Time.time + 3f * Random.Range(0.66f, 1.33f);
                randomLookPosition = Vector3.zero;

                LookRandom = !LookRandom;
                if (LookRandom)
                {
                    var Mask = LayerMaskClass.HighPolyWithTerrainMask;
                    var headPos = SAIN.Transform.HeadPosition;
                    float pointDistance = 0f;
                    for (int i = 0; i < 10; i++)
                    {
                        var random = Random.onUnitSphere * 5f;
                        random.y = 0f;
                        if (!Physics.Raycast(headPos, random, out var hit, 10f, Mask))
                        {
                            randomLookPosition = random + headPos;
                            break;
                        }
                        else
                        {
                            if (hit.distance > pointDistance)
                            {
                                pointDistance = hit.distance;
                                randomLookPosition = hit.point;
                            }
                        }
                    }
                }
                else
                {
                    Vector3? targetPos = SAIN.CurrentTargetPosition;
                    if (targetPos != null)
                    {
                        randomLookPosition = targetPos.Value;
                    }
                }

                if (randomLookPosition != Vector3.zero)
                {
                    LookToPoint(randomLookPosition, Random.Range(60f, 90f));
                }
                else
                {
                    LookToMovingDirection();
                }
            }
        }

        private Vector3 randomLookPosition;

        public bool LookToPathToEnemy()
        {
            var enemy = SAIN.Enemy;
            if (enemy != null && enemy.Path.LastCornerToEnemy != null)
            {
                LookToPoint(enemy.Path.LastCornerToEnemy.Value + Vector3.up);
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
        Search
    }
}