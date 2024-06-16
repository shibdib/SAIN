using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.SAINComponent.Classes.WeaponFunction;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINSteeringClass : SAINBase, ISAINClass
    {
        public SteerPriority CurrentSteerPriority { get; private set; } = SteerPriority.None;
        public SteerPriority LastSteerPriority { get; private set; } = SteerPriority.None;

        // How long a bot will look at where they last saw an enemy instead of something they hear
        private readonly float Steer_TimeSinceLocationKnown_Threshold = 3f;

        // How long a bot will look at where they last saw an enemy if they don't hear any other threats
        private readonly float Steer_TimeSinceSeen_Long = 60f;

        // How far a sound can be for them to react by looking toward it.
        private readonly float Steer_HeardSound_Dist = 50f;

        // How old a sound can be, in seconds, for them to react by looking toward it.
        private readonly float Steer_HeardSound_Age = 3f;

        private readonly float baseTurnSpeed = 240f;

        private readonly float baseTurnSpeedNoEnemy = 150f;

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

        public bool SteerByPriority(bool lookRandom = true)
        {
            var lastPriority = CurrentSteerPriority;
            CurrentSteerPriority = findSteerPriority(lookRandom);

            if (CurrentSteerPriority != lastPriority)
                LastSteerPriority = lastPriority;

            switch (CurrentSteerPriority)
            {
                case SteerPriority.RunningPath:
                case SteerPriority.Aiming:
                    return true;

                case SteerPriority.ManualShooting:
                    LookToPoint(Bot.ManualShoot.ShootPosition);
                    return true;

                case SteerPriority.EnemyVisible:
                    lookToEnemy();
                    return true;

                case SteerPriority.UnderFire:
                    lookToUnderFirePos();
                    return true;

                case SteerPriority.LastHit:
                    lookToLastHitPos();
                    return true;

                case SteerPriority.EnemyLastKnownLong:
                case SteerPriority.EnemyLastKnown:
                    if (!LookToLastKnownEnemyPosition(Bot.Enemy))
                    {
                        LookToRandomPosition();
                    }
                    return true;

                case SteerPriority.HeardThreat:
                    if (_lastHeardSound != null)
                    {
                        lookToHearPos(_lastHeardSound.Position);
                        return true;
                    }
                    Logger.LogError("Cannot look toward null PlaceForCheck.");
                    return false;

                case SteerPriority.Sprinting:
                    LookToMovingDirection(400);
                    return true;

                case SteerPriority.RandomLook:
                    LookToRandomPosition();
                    return true;

                default:
                    return false;
            }
        }

        public bool LookToLastKnownEnemyPosition(SAINEnemy enemy)
        {
            Vector3? place = findLastKnown(enemy);
            if (place != null)
            {
                LookToPoint(place.Value);
                return true;
            }
            return false;
        }

        public void LookToMovingDirection(float rotateSpeed = 150f, bool sprint = false)
        {
            if (sprint || Player.IsSprintEnabled)
            {
                BotOwner.Steering.LookToMovingDirection(500f);
            }
            else
            {
                BotOwner.Steering.LookToMovingDirection(rotateSpeed);
            }
        }

        public void LookToPoint(Vector3 point, float rotateSpeed = -1)
        {
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

        public void LookToDirection(Vector3 direction, bool flat, float rotateSpeed = -1f)
        {
            if (flat)
            {
                direction.y = 0f;
            }
            Vector3 pos = BotOwner.WeaponRoot.position + direction.normalized;
            LookToPoint(pos, rotateSpeed);
        }

        public void LookToEnemy(SAINEnemy enemy)
        {
            if (enemy != null)
            {
                LookToPoint(enemy.EnemyPosition + _weaponRootOffset);
            }
        }

        public void LookToRandomPosition()
        {
            updateRandomLook();
            if (_randomLookPosition != Vector3.zero)
            {
                LookToPoint(_randomLookPosition, Random.Range(90f, 150f));
            }
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

        private void getDefaultSpeeds()
        {
            var moveSettings = BotOwner.Settings.FileSettings.Move;
            _baseSpeed = moveSettings.BASE_ROTATE_SPEED;
            _firstTurnSpeed = moveSettings.FIRST_TURN_SPEED;
            _firstTurnBigSpeed = moveSettings.FIRST_TURN_BIG_SPEED;
        }

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

        public void Dispose()
        {
        }

        private SteerPriority findSteerPriority(bool lookRandom)
        {
            SteerPriority result = strickChecks();

            if (result != SteerPriority.None)
            {
                return result;
            }

            result = reactiveSteering();

            if (result != SteerPriority.None)
            {
                return result;
            }

            result = senseSteering();

            if (result != SteerPriority.None)
            {
                return result;
            }

            if (lookRandom)
            {
                return SteerPriority.RandomLook;
            }
            return SteerPriority.None;
        }

        private SteerPriority strickChecks()
        {
            if (Bot.Mover.SprintController.Running)
                return SteerPriority.RunningPath;

            if (Player.IsSprintEnabled)
                return SteerPriority.Sprinting;

            if (lookToAimTarget())
                return SteerPriority.Aiming;

            if (Bot.ManualShoot.Reason != EShootReason.None
                && Bot.ManualShoot.ShootPosition != Vector3.zero)
                return SteerPriority.ManualShooting;

            if (enemyVisible())
                return SteerPriority.EnemyVisible;

            return SteerPriority.None;
        }

        private SteerPriority reactiveSteering()
        {
            SAINEnemy enemyWhoShotMe = enemyShotMe();
            if (enemyWhoShotMe != null && !enemyWhoShotMe.IsCurrentEnemy)
                return SteerPriority.LastHit;

            if (BotOwner.Memory.IsUnderFire && !Bot.Memory.LastUnderFireEnemy.IsCurrentEnemy)
                return SteerPriority.UnderFire;

            return SteerPriority.None;
        }

        private SteerPriority senseSteering()
        {
            EnemyPlace lastKnownPlace = Bot.Enemy?.KnownPlaces?.LastKnownPlace;

            if (lastKnownPlace != null && lastKnownPlace.TimeSincePositionUpdated < Steer_TimeSinceLocationKnown_Threshold)
                return SteerPriority.EnemyLastKnown;

            if (heardThreat(out PlaceForCheck placeForCheck))
            {
                _lastHeardSound = placeForCheck;
                return SteerPriority.HeardThreat;
            }

            _lastHeardSound = null;

            if (lastKnownPlace != null && lastKnownPlace.TimeSincePositionUpdated < Steer_TimeSinceSeen_Long)
                return SteerPriority.EnemyLastKnownLong;

            return SteerPriority.None;
        }

        private bool heardThreat(out PlaceForCheck placeForCheck)
        {
            placeForCheck = BotOwner.BotsGroup.YoungestFastPlace(BotOwner, Steer_HeardSound_Dist, Steer_HeardSound_Age);
            if (placeForCheck != null)
            {
                SAINEnemy enemy = Bot.Enemy;
                if (enemy == null)
                {
                    return true;
                }
                if (Bot.Squad.SquadInfo?.PlayerPlaceChecks.TryGetValue(enemy.EnemyProfileId, out PlaceForCheck enemyPlace) == true &&
                    enemyPlace != placeForCheck)
                {
                    return true;
                }
            }
            return false;
        }

        private SAINEnemy enemyShotMe()
        {
            if (Bot.BotHitReaction.TimeSinceShot < 3f)
                return Bot.BotHitReaction.EnemyWhoLastShotMe;

            return null;
        }

        private Vector3 adjustLookPoint(Vector3 target)
        {
            Vector3 head = Bot.Transform.EyePosition;
            Vector3 direction = target - head;
            direction.y = 0f;
            direction = direction.normalized;
            return target + direction;
        }

        private Vector3? enemyLastKnown(SAINEnemy enemy, out bool visible)
        {
            visible = false;
            EnemyPlace lastKnownPlace = enemy?.KnownPlaces.LastKnownPlace;
            if (lastKnownPlace == null)
            {
                return null;
            }
            visible = lastKnownPlace.PersonalClearLineOfSight(Bot.Transform.EyePosition, LayerMaskClass.HighPolyWithTerrainMask);
            return adjustLookPoint(lastKnownPlace.Position) + _weaponRootOffset;
        }

        private Vector3? findLastKnown(SAINEnemy enemy)
        {
            if (enemy == null)
            {
                return null;
            }
            if (enemy.IsVisible)
            {
                return enemy.EnemyPosition;
            }
            Vector3? lastKnown = enemyLastKnown(enemy, out bool visible);
            if (lastKnown != null &&
                visible)
            {
                return lastKnown;
            }
            Vector3? blindCornerToEnemy = enemy.Path.BlindCornerToEnemy;
            if (blindCornerToEnemy != null)
            {
                return adjustLookPoint(blindCornerToEnemy.Value);
            }
            Vector3? lastCorner = enemy.Path.LastCornerToEnemy;
            if (lastCorner != null &&
                enemy.CanSeeLastCornerToEnemy)
            {
                return adjustLookPoint(lastCorner.Value) + _weaponRootOffset;
            }
            var enemyPath = enemy.Path.PathToEnemy;
            if (enemyPath != null &&
                enemyPath.corners.Length > 2)
            {
                return adjustLookPoint(enemyPath.corners[1]) + _weaponRootOffset;
            }
            if (lastKnown != null)
            {
                return lastKnown.Value + _weaponRootOffset;
            }
            return null;
        }

        private bool lookToAimTarget()
        {
            if (BotOwner.WeaponManager.Reload?.Reloading == true)
            {
                return false;
            }
            return AimStatus != AimStatus.NoTarget;
        }

        private bool enemyVisible()
        {
            SAINEnemy enemy = Bot.Enemy;

            if (enemy != null)
            {
                if (enemy.IsVisible)
                {
                    return true;
                }

                if (enemy.Seen &&
                    enemy.TimeSinceSeen < 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        private void lookToEnemy()
        {
            LookToEnemy(Bot.Enemy);
        }

        private void lookToUnderFirePos()
        {
            LookToPoint(Bot.Memory.UnderFireFromPosition + _weaponRootOffset);
        }

        private void lookToHearPos(Vector3 soundPos, bool visionCheck = false)
        {
            float turnSpeed = Bot.HasEnemy ? baseTurnSpeed : baseTurnSpeedNoEnemy;
            if ((soundPos - Bot.Position).sqrMagnitude > 125f.Sqr())
            {
                LookToPoint(soundPos, turnSpeed);
                return;
            }

            if (_path == null)
            {
                _path = new NavMeshPath();
            }
            if (_lastHeardSoundTimer < Time.time || (_lastHeardSoundCheckedPos - soundPos).magnitude > 1f)
            {
                _lastHeardSoundTimer = Time.time + 0.5f;
                _lastHeardSoundCheckedPos = soundPos;
                _lastHeardSoundCorner = Vector3.zero;

                _path.ClearCorners();
                if (NavMesh.CalculatePath(Bot.Position, soundPos, -1, _path))
                {
                    if (_path.corners.Length > 2)
                    {
                        for (int i = _path.corners.Length - 1; i >= 0; i--)
                        {
                            Vector3 corner = _path.corners[i] + Vector3.up;
                            Vector3 headPos = BotOwner.LookSensor._headPoint;
                            Vector3 cornerDir = corner - headPos;
                            if (!Physics.Raycast(headPos, cornerDir.normalized, cornerDir.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                            {
                                _lastHeardSoundCorner = corner;
                                break;
                            }
                        }
                    }
                }
            }

            if (_lastHeardSoundCorner != Vector3.zero)
            {
                LookToPoint(_lastHeardSoundCorner, turnSpeed);
            }
            else
            {
                LookToPoint(soundPos, turnSpeed);
            }
        }

        private void lookToLastHitPos()
        {
            var shotMeRecent = enemyShotMe();
            if (shotMeRecent != null)
            {
                Vector3? lastKnown = findLastKnown(shotMeRecent);
                if (lastKnown != null)
                {
                    LookToPoint(lastKnown.Value);
                    return;
                }
                var lastShotPos = shotMeRecent.EnemyStatus.LastShotPosition;
                if (lastShotPos != null)
                {
                    LookToPoint(lastShotPos.Value + _weaponRootOffset);
                    return;
                }
            }
            LookToRandomPosition();
        }

        private void updateRandomLook()
        {
            if (_randomLookTime < Time.time)
            {
                _lookRandomToggle = !_lookRandomToggle;
                Vector3 newRandomPos = findRandomLookPos(out bool isRandom);
                if (newRandomPos != Vector3.zero)
                {
                    float baseTime = isRandom ? 2f : 4f;
                    _randomLookTime = Time.time + baseTime * Random.Range(0.66f, 1.33f);
                    _randomLookPosition = newRandomPos;
                    if (!isRandom)
                    {
                        _randomLookPosition += _weaponRootOffset;
                    }
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
            return findLastKnown(Bot.Enemy) ?? Vector3.zero;
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

        private ESteerSpeed _steerStatus;
        private float _baseSpeed;
        private float _firstTurnSpeed;
        private float _firstTurnBigSpeed;
        private Vector3 _randomLookPosition;
        private float _randomLookTime = 0f;
        private PlaceForCheck _lastHeardSound;
        private float _lastHeardSoundTimer;
        private Vector3 _lastHeardSoundCheckedPos;
        private Vector3 _lastHeardSoundCorner;
        private NavMeshPath _path;
        private bool _lookRandomToggle;
        private Vector3 _weaponRootOffset => BotOwner.WeaponRoot.position - Bot.Position;

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

        private static FieldInfo aimStatusField;
    }

    public enum SteerPriority
    {
        None,
        Shooting,
        ManualShooting,
        Aiming,
        EnemyVisible,
        HeardThreat,
        EnemyLastKnownLong,
        RandomLook,
        LastHit,
        UnderFire,
        MoveDirection,
        Sprinting,
        EnemyLastKnown,
        Search,
        RunningPath,
    }
}