using EFT;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINSteeringClass : BotBase, IBotClass
    {
        public SteerPriority CurrentSteerPriority => _steerPriorityClass.CurrentSteerPriority;
        public SteerPriority LastSteerPriority => _steerPriorityClass.LastSteerPriority;
        public EEnemySteerDir EnemySteerDir { get; private set; }
        public Vector3 WeaponRootOffset => BotOwner.WeaponRoot.position - Bot.Position + (Vector3.down * 0.1f);
        public AimStatus AimStatus => _steerPriorityClass.AimStatus;

        public bool SteerByPriority(Enemy enemy = null, bool lookRandom = true, bool ignoreRunningPath = false)
        {
            if (enemy == null)
                enemy = Bot.Enemy;

            switch (_steerPriorityClass.GetCurrentSteerPriority(lookRandom, ignoreRunningPath)) {
                case SteerPriority.RunningPath:
                case SteerPriority.Aiming:
                    return true;

                case SteerPriority.ManualShooting:
                    LookToPoint(Bot.ManualShoot.ShootPosition + Bot.Info.WeaponInfo.Recoil.CurrentRecoilOffset);
                    return true;

                case SteerPriority.EnemyVisible:
                    LookToEnemy(Bot.Enemy);
                    return true;

                case SteerPriority.UnderFire:
                    lookToUnderFirePos();
                    return true;

                case SteerPriority.LastHit:
                    lookToLastHitPos();
                    return true;

                case SteerPriority.EnemyLastKnownLong:
                case SteerPriority.EnemyLastKnown:
                    if (!LookToLastKnownEnemyPosition(enemy)) {
                        LookToRandomPosition();
                    }
                    return true;

                case SteerPriority.HeardThreat:
                    HeardSoundSteering.LookToHeardPosition();
                    return true;

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

        public bool LookToLastKnownEnemyPosition(Enemy enemy, Vector3? lastKnown = null)
        {
            Vector3? place = lastKnown ?? FindLastKnownTarget(enemy);
            if (place != null) {
                LookToPoint(place.Value);
                return true;
            }
            return false;
        }

        public void LookToMovingDirection(float rotateSpeed = 150f, bool sprint = false)
        {
            if (sprint || Player.IsSprintEnabled) {
                BotOwner.Steering.LookToMovingDirection(500f);
            }
            else {
                BotOwner.Steering.LookToMovingDirection(rotateSpeed);
            }
        }

        public void LookToPoint(Vector3 point, float minTurnSpeed = -1, float maxTurnSpeed = -1f)
        {
            Vector3 direction = point - BotOwner.WeaponRoot.position;
            if (direction.sqrMagnitude < 1f) {
                direction = direction.normalized;
            }
            float turnSpeed = calcTurnSpeed(direction, minTurnSpeed, maxTurnSpeed);
            BotOwner.Steering.LookToDirection(direction, turnSpeed);
        }

        private float calcTurnSpeed(Vector3 targetDirection, float minTurnSpeed, float maxTurnSpeed)
        {
            float minSpeed = minTurnSpeed > 0 ? minTurnSpeed : _minSpeed;
            float maxSpeed = maxTurnSpeed > 0 ? maxTurnSpeed : _maxSpeed;
            if (minSpeed >= maxSpeed) {
                return minSpeed;
            }

            float maxAngle = _maxAngle;
            Vector3 currentDir = _lookDirection;
            float angle = Vector3.Angle(currentDir, targetDirection.normalized);

            if (angle >= maxAngle) {
                return maxSpeed;
            }
            float minAngle = _minAngle;
            if (angle <= minAngle) {
                return minSpeed;
            }

            float angleDiff = maxAngle - minAngle;
            float targetDiff = angle - minAngle;
            float ratio = targetDiff / angleDiff;
            float result = Mathf.Lerp(minSpeed, maxSpeed, ratio);
            //Logger.LogDebug($"Steer Speed Calc: Result: [{result}] Angle: [{angle}]");
            return result;
        }

        private static SteeringSettings _settings => GlobalSettingsClass.Instance.Steering;
        private float _maxAngle => _settings.SteerSpeed_MaxAngle;
        private float _minAngle => _settings.SteerSpeed_MinAngle;
        private float _maxSpeed => _settings.SteerSpeed_MaxSpeed;
        private float _minSpeed => _settings.SteerSpeed_MinSpeed;

        public void LookToDirection(Vector3 direction, bool flat, float rotateSpeed = -1f)
        {
            if (flat) {
                direction.y = 0f;
            }
            Vector3 pos = BotOwner.WeaponRoot.position + direction.normalized;
            LookToPoint(pos, rotateSpeed);
        }

        public void LookToDirection(Vector2 direction, float rotateSpeed = -1f)
        {
            Vector3 vector = new Vector3(direction.x, 0, direction.y);
            Vector3 pos = BotOwner.WeaponRoot.position + vector.normalized;
            LookToPoint(pos, rotateSpeed);
        }

        public void LookToEnemy(Enemy enemy)
        {
            if (enemy != null) {
                LookToPoint(enemy.EnemyPosition + WeaponRootOffset);
            }
        }

        public void LookToRandomPosition()
        {
            Vector3? point = _randomLook.UpdateRandomLook();
            if (point != null) {
                float random = Random.Range(40f, 100f);
                LookToPoint(point.Value, random, random * 2f);
            }
        }

        public SAINSteeringClass(BotComponent sain) : base(sain)
        {
            _randomLook = new RandomLookClass(this);
            _steerPriorityClass = new SteerPriorityClass(this);
            HeardSoundSteering = new HeardSoundSteeringClass(this);
        }

        public float AngleToPointFromLookDir(Vector3 point)
        {
            Vector3 direction = (point - BotOwner.WeaponRoot.position).normalized;
            return Vector3.Angle(_lookDirection, direction);
        }

        public float AngleToDirectionFromLookDir(Vector3 direction)
        {
            return Vector3.Angle(_lookDirection, direction);
        }

        public void Init()
        {
            base.SubscribeToPreset(UpdatePresetSettings);
            HeardSoundSteering.Init();
        }

        public void Update()
        {
            HeardSoundSteering.Update();
            if (!Bot.SAINLayersActive) {
                BotOwner.Settings.FileSettings.Move.BASE_ROTATE_SPEED = 180f;
            }
            else {
                BotOwner.Settings.FileSettings.Move.BASE_ROTATE_SPEED = 250f;
            }
        }

        public void Dispose()
        {
            HeardSoundSteering.Dispose();
        }

        private Vector3 adjustLookPoint(Vector3 target)
        {
            Vector3 head = Bot.Transform.EyePosition;
            Vector3 heightOffset = head - Bot.Position;
            target.y += heightOffset.y;
            Vector3 direction = target - head;
            return target + direction.normalized;
        }

        public Vector3? EnemyLastKnown(Enemy enemy, out bool visible)
        {
            visible = false;
            EnemyPlace lastKnownPlace = enemy?.KnownPlaces.LastKnownPlace;
            if (lastKnownPlace == null) {
                return null;
            }
            visible = lastKnownPlace.CheckLineOfSight(Bot.Transform.EyePosition, LayerMaskClass.HighPolyWithTerrainMask);
            return lastKnownPlace.GroundedPosition();
        }

        public Vector3? FindLastKnownTarget(Enemy enemy)
        {
            EnemySteerDir = EEnemySteerDir.None;
            if (enemy == null) {
                return null;
            }
            if (enemy.IsVisible) {
                return adjustLookPoint(enemy.EnemyPosition);
            }

            var lastKnown = enemy.KnownPlaces.LastKnownPlace;
            if (lastKnown != null && lastKnown.CheckLineOfSight(Bot.Transform.EyePosition, LayerMaskClass.HighPolyWithTerrainMask)) {
                EnemySteerDir = EEnemySteerDir.VisibleLastKnown;
                return lastKnown.Position + WeaponRootOffset;
            }
            if (enemy.InLineOfSight && lastKnown.VisibleSourceOnLastUpdate && lastKnown.TimeSincePositionUpdated < 4f) {
                return lastKnown.Position + WeaponRootOffset;
            }

            //Vector3? lastKnown = EnemyLastKnown(enemy, out bool visible);
            //if (lastKnown != null &&
            //    visible)
            //{
            //    EnemySteerDir = EEnemySteerDir.VisibleLastKnown;
            //    return adjustLookPoint(lastKnown.Value);
            //}

            EnemyCornerDictionary corners = enemy.Path.EnemyCorners;

            Vector3? blindCorner = corners.EyeLevelPosition(ECornerType.Blind);
            if (blindCorner != null) {
                bool correctDirection = true;
                Vector3 root = Bot.Transform.WeaponRoot;
                Vector3 blindCornerDir = blindCorner.Value - root;

                //Vector3? firstCorner = corners.GroundPosition(ECornerType.First);
                //if (firstCorner != null) {
                //    Vector3 firstCornerDir = firstCorner.Value - root;
                //    if (Vector3.Angle(firstCornerDir, blindCornerDir) > 180){
                //        correctDirection = false;
                //    }
                //}

                // Bots are spinning around to look back at their blind corner after passing it, need a better solution, but this might be a decent temp fix
                if (blindCornerDir.sqrMagnitude < 0.5f * 0.5f &&
                    Vector3.Dot(blindCornerDir.normalized, enemy.EnemyDirectionNormal) < 0.33f) {
                    correctDirection = false;
                }

                if (correctDirection) {
                    EnemySteerDir = EEnemySteerDir.BlindCorner;
                    return blindCorner;
                }
            }

            //if (enemy.Path.CanSeeLastCornerToEnemy) {
            //    Vector3? lastCorner = corners.PointPastCorner(ECornerType.Last);
            //    if (lastCorner != null) {
            //        EnemySteerDir = EEnemySteerDir.LastCorner;
            //        return lastCorner;
            //    }
            //}

            //Vector3? first = corners.PointPastCorner(ECornerType.First);
            //if (first != null)
            //{
            //    EnemySteerDir = EEnemySteerDir.Path;
            //    return first;
            //}

            //Vector3? lastKnownCorner = corners.PointPastCorner(ECornerType.LastKnown);
            if (lastKnown != null) {
                EnemySteerDir = EEnemySteerDir.LastKnown;
                return lastKnown.Position + WeaponRootOffset;
            }

            return null;
        }

        private void lookToUnderFirePos()
        {
            LookToPoint(Bot.Memory.UnderFireFromPosition + WeaponRootOffset);
        }

        private void lookToLastHitPos()
        {
            var enemyWhoShotMe = _steerPriorityClass.EnemyWhoLastShotMe;
            if (enemyWhoShotMe != null) {
                Vector3? lastKnown = FindLastKnownTarget(enemyWhoShotMe);
                if (lastKnown != null) {
                    LookToPoint(lastKnown.Value);
                    return;
                }
                var lastShotPos = enemyWhoShotMe.Status.LastShotPosition;
                if (lastShotPos != null) {
                    LookToPoint(lastShotPos.Value + WeaponRootOffset);
                    return;
                }
            }
            LookToRandomPosition();
        }

        public HeardSoundSteeringClass HeardSoundSteering { get; }
        private readonly RandomLookClass _randomLook;
        private readonly SteerPriorityClass _steerPriorityClass;

        private Vector3 _lookDirection => Bot.LookDirection;

        protected void UpdatePresetSettings(SAINPresetClass preset)
        {
        }
    }
}