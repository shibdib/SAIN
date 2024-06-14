using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class SAINEnemyVision : EnemyBase
    {
        public SAINEnemyVision(SAINEnemy enemy) : base(enemy)
        {
            GainSight = new GainSightClass(enemy);
            VisionDist = new EnemyVisionDistanceClass(enemy);
            EnemyVisionChecker = new EnemyVisionChecker(enemy);
            enemy.Bot.OnBotDisabled += stopVisionCheck;
        }

        public void Dispose()
        {
            Enemy.Bot.OnBotDisabled -= stopVisionCheck;
        }

        public void Update(bool isCurrentEnemy)
        {
            UpdateVisible(false);
            UpdateCanShoot(false);
        }

        public void startCheckingVision()
        {
            if (_visionCoroutine == null)
            {
                _visionCoroutine = Enemy.Bot.StartCoroutine(checkVision());
            }
        }

        private void stopVisionCheck()
        {
            if (_visionCoroutine != null)
            {
                Enemy.Bot.StopCoroutine(_visionCoroutine);
                _visionCoroutine = null;
            }
        }

        private Coroutine _visionCoroutine;

        private IEnumerator checkVision()
        {
            while (true)
            {
                EnemyVisionChecker.CheckVision();
                yield return null;
            }
        }

        public float EnemyVelocity
        {
            get
            {
                const float min = 0.5f * 0.5f;
                const float max = 5f * 5f;

                float rawVelocity = EnemyPlayer.Velocity.sqrMagnitude;
                if (rawVelocity <= min)
                {
                    return 0f;
                }
                if (rawVelocity >= max)
                {
                    return 1f;
                }

                float num = max - min;
                float num2 = rawVelocity - min;
                return num2 / num;
            }
        }

        public bool FirstContactOccured { get; private set; }

        public bool ShallReportRepeatContact { get; set; }

        public bool ShallReportLostVisual { get; set; }

        private const float _repeatContactMinSeenTime = 12f;

        private const float _lostContactMinSeenTime = 12f;

        private float _realLostVisionTime;

        private bool isEnemyInVisibleSector()
        {
            return AngleToEnemy <= MaxVisionAngle;
        }

        private float getAngleToEnemy(bool setYto0)
        {
            Vector3 direction = EnemyPosition - Enemy.Bot.Position;
            Vector3 lookDir = Bot.LookDirection;
            if (setYto0)
            {
                direction.y = 0;
                lookDir.y = 0;
            }
            return Vector3.Angle(direction, lookDir);
        }

        public float MaxVisionAngle => Enemy.Bot.Info.FileSettings.Core.VisibleAngle / 2f;

        public float AngleToEnemy
        {
            get
            {
                getAngles();
                return _angleToEnemy;
            }
        }

        public float AngleToEnemyHorizontal
        {
            get
            {
                getAngles();
                return _angleToEnemyHoriz;
            }
        }

        private void getAngles()
        {
            if (_calcAngleTime < Time.time)
            {
                _calcAngleTime = Time.time + _calcAngleFreq;
                _angleToEnemy = getAngleToEnemy(false);
                _angleToEnemyHoriz = getAngleToEnemy(true);
            }
        }

        private float _angleToEnemyHoriz;
        private float _angleToEnemy;
        private float _calcAngleTime;
        private float _calcAngleFreq = 0.1f;

        public void UpdateVisible(bool forceOff)
        {
            bool wasVisible = IsVisible;
            bool lineOfSight = InLineOfSight || Bot.Memory.VisiblePlayers.Contains(EnemyPlayer);

            if (forceOff)
            {
                IsVisible = false;
            }
            else
            {
                IsVisible =
                    EnemyInfo?.IsVisible == true &&
                    lineOfSight &&
                    isEnemyInVisibleSector();
            }

            if (IsVisible)
            {
                if (!wasVisible)
                {
                    VisibleStartTime = Time.time;
                    if (Seen && TimeSinceSeen >= _repeatContactMinSeenTime)
                    {
                        ShallReportRepeatContact = true;
                    }
                }
                if (!Seen)
                {
                    FirstContactOccured = true;
                    TimeFirstSeen = Time.time;
                    Seen = true;
                }
                _realLostVisionTime = Time.time;
                TimeLastSeen = Time.time;
                LastSeenPosition = EnemyPerson.Position;
            }

            if (Time.time - _realLostVisionTime < 1f)
            {
                Enemy.UpdateSeenPosition(EnemyPerson.Position);
            }

            if (!IsVisible)
            {
                if (Seen
                    && TimeSinceSeen > _lostContactMinSeenTime
                    && _nextReportLostVisualTime < Time.time)
                {
                    _nextReportLostVisualTime = Time.time + 20f;
                    ShallReportLostVisual = true;
                }
                VisibleStartTime = -1f;
            }

            if (IsVisible != wasVisible)
            {
                LastChangeVisionTime = Time.time;
            }
        }

        private float _nextReportLostVisualTime;

        public void UpdateCanShoot(bool forceOff)
        {
            if (forceOff)
            {
                CanShoot = false;
                return;
            }
            CanShoot = EnemyInfo?.CanShoot == true;
        }

        public bool InLineOfSight => EnemyVisionChecker.LineOfSight;
        public bool IsVisible { get; private set; }
        public bool CanShoot { get; private set; }
        public Vector3? LastSeenPosition { get; set; }
        public float VisibleStartTime { get; private set; }
        public float TimeSinceSeen => Seen ? Time.time - TimeLastSeen : -1f;
        public bool Seen { get; private set; }
        public float TimeFirstSeen { get; private set; }
        public float TimeLastSeen { get; private set; }
        public float LastChangeVisionTime { get; private set; }
        public float LastGainSightResult { get; set; }
        public float GainSightCoef => GainSight.GainSightCoef;
        public float VisionDistance => VisionDist.VisionDistance;

        private readonly GainSightClass GainSight;
        private readonly EnemyVisionDistanceClass VisionDist;
        public EnemyVisionChecker EnemyVisionChecker { get; private set; }
    }

    public class EnemyVisionChecker : EnemyBase
    {
        public EnemyVisionChecker(SAINEnemy enemy) : base(enemy)
        {
            EnemyParts = new SAINEnemyParts(enemy.EnemyPlayer.PlayerBones, enemy.Player.IsYourPlayer);
            _transform = enemy.Bot.Transform;
        }

        private PersonTransformClass _transform;

        public void CheckVision()
        {
            if (EnemyParts.CheckLineOfSight(_transform.EyePosition))
            {
                return;
            }

            // Do an extra check if the bot has this enemy as their active primary enemy or the enemy is not AI
            // if (Enemy.IsCurrentEnemy && !Enemy.IsAI && 
            //     EnemyParts.CheckLineOfSight(_transform.EyePosition))
            // {
            //     return;
            // }
        }

        public bool LineOfSight => EnemyParts.LineOfSight;

        public SAINEnemyParts EnemyParts { get; private set; }
    }

    public class SAINEnemyParts
    {
        public SAINEnemyParts(PlayerBones bones, bool isYourPlayer)
        {
            IsYourPlayer = isYourPlayer;
            createPartDatas(bones);
        }

        private bool IsYourPlayer;

        public bool LineOfSight => _lastSuccessTime + 0.1f > Time.time;

        private float _lastSuccessTime;

        public bool CheckLineOfSight(Vector3 origin)
        {
            if (_lastCheckSuccessPart != null)
            {
                if (_lastCheckSuccessPart.CheckLineOfSight(origin))
                {
                    _lastSuccessTime = Time.time;
                    return true;
                }
                _lastCheckSuccessPart = null;
            }

            SAINEnemyPartData checkingPart = Parts.Values.PickRandom();
            if (checkingPart.CheckLineOfSight(origin))
            {
                _lastCheckSuccessPart = checkingPart;
                _lastSuccessTime = Time.time;
                return true;
            }

            _lastSuccessTime = 0f;
            return false;
        }

        private SAINEnemyPartData _lastCheckSuccessPart;

        private void createPartDatas(PlayerBones bones)
        {
            foreach (EBodyPart bodyPart in EnumValues.GetEnum<EBodyPart>())
            {
                List<BodyPartCollider> colliders = new List<BodyPartCollider>();
                findParts(bodyPart, out BifacialTransform transform, colliders, bones);
                Parts.Add(bodyPart, new SAINEnemyPartData(bodyPart, transform, colliders, IsYourPlayer));
            }
        }

        private void findParts(EBodyPart bodyPart, out BifacialTransform transform, List<BodyPartCollider> partColliders, PlayerBones bones)
        {
            switch (bodyPart)
            {
                default:
                    transform = bones.BifacialTransforms[PlayerBoneType.Body];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RibcageUp]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RibcageLow]);
                    break;

                case EBodyPart.Head:
                    transform = bones.BifacialTransforms[PlayerBoneType.Head];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.ParietalHead]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.HeadCommon]);
                    break;

                case EBodyPart.LeftArm:
                    transform = bones.BifacialTransforms[PlayerBoneType.LeftShoulder];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.LeftForearm]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.LeftUpperArm]);
                    break;

                case EBodyPart.RightArm:
                    transform = bones.BifacialTransforms[PlayerBoneType.RightShoulder];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RightForearm]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RightUpperArm]);
                    break;

                case EBodyPart.LeftLeg:
                    transform = bones.BifacialTransforms[PlayerBoneType.LeftThigh1];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.LeftThigh]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.LeftCalf]);
                    break;

                case EBodyPart.RightLeg:
                    transform = bones.BifacialTransforms[PlayerBoneType.RightThigh1];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RightThigh]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RightCalf]);
                    break;
            }
        }

        public Dictionary<EBodyPart, SAINEnemyPartData> Parts = new Dictionary<EBodyPart, SAINEnemyPartData>();
    }

    public class SAINEnemyPartData
    {
        public SAINEnemyPartData(EBodyPart bodyPart, BifacialTransform transform, List<BodyPartCollider> colliders, bool isYourPlayer)
        {
            BodyPart = bodyPart;
            Transform = transform;
            Colliders = colliders;
            IsYourPlayer = isYourPlayer;
        }

        public Vector3 Position
        {
            get
            {
                if (Time.frameCount != this._posCachedForFrame && 
                    Transform != null && 
                    Transform.Original != null)
                {
                    this._position = this.Transform.position;
                    this._posCachedForFrame = Time.frameCount;
                }
                return this._position;
            }
        }

        private bool IsYourPlayer;
        private Vector3 _position;

        private int _posCachedForFrame;

        public readonly EBodyPart BodyPart;
        public readonly List<BodyPartCollider> Colliders;
        public readonly BifacialTransform Transform;

        public bool LineOfSight => _lastSuccessTime + 0.2f > Time.time;

        private float _nextCheckTime;

        public bool CheckLineOfSight(Vector3 origin)
        {
            if (_nextCheckTime > Time.time)
            {
                return LineOfSight;
            }
            _nextCheckTime = Time.time + 0.1f;

            BodyPartCollider collider = _lastSuccessPart ?? Colliders.PickRandom();
            Vector3 castPoint = _lastSuccessCastPoint ?? getCastPoint(origin, collider);

            Vector3 direction = castPoint - origin;
            bool lineOfSight = !Physics.Raycast(origin, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);

            if (lineOfSight)
            {
                _lastSuccessTime = Time.time;
                _lastSuccessPart = collider;
                _lastSuccessCastPoint = castPoint;
            }
            else
            {
                _lastSuccessTime = 0f;
                _lastSuccessPart = null;
                _lastSuccessCastPoint = null;
            }

            if (SAINPlugin.DebugMode &&
                IsYourPlayer &&
                _nextdrawTime < Time.time)
            {
                _nextdrawTime = Time.time + 0.1f;
                if (lineOfSight)
                {
                    DebugGizmos.Sphere(castPoint, 0.025f, Color.red, true, 10f);
                    DebugGizmos.Sphere(origin, 0.025f, Color.red, true, 1f);
                    DebugGizmos.Line(castPoint, origin, Color.red, 0.005f, true, 0.5f);
                    Logger.LogDebug($"{BodyPart} : {direction.magnitude} : {castPoint} : Is Sphere? {_isSphereCollider}");
                }
                else
                {
                    DebugGizmos.Sphere(castPoint, 0.025f, Color.white, true, 10f);
                    DebugGizmos.Sphere(origin, 0.025f, Color.white, true, 1f);
                    DebugGizmos.Line(castPoint, origin, Color.white, 0.005f, true, 0.5f);
                }
            }

            return lineOfSight;
        }

        private float _nextdrawTime;

        private float _lastSuccessTime;

        private Vector3 getCastPoint(Vector3 origin, BodyPartCollider collider)
        {
            SphereCollider sphere;
            _isSphereCollider = collider.Collider != null && 
                (sphere = collider.Collider as SphereCollider) != null;

            if (_isSphereCollider)
            {
                return collider.GetRandomPointToCastLocal(origin);
            }
            return Position;
        }

        private bool _isSphereCollider;

        private BodyPartCollider _lastSuccessPart;
        private Vector3? _lastSuccessCastPoint;
    }
}