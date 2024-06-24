using EFT;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyVision : EnemyBase, ISAINEnemyClass
    {
        public event Action<Enemy, bool> OnVisionChange;
        public event Action<Enemy> OnFirstSeen;

        public SAINEnemyVision(Enemy enemy) : base(enemy)
        {
            GainSight = new GainSightClass(enemy);
            VisionDist = new EnemyVisionDistanceClass(enemy);
            EnemyVisionChecker = new EnemyVisionChecker(enemy);
        }

        public void Init()
        {
            Enemy.OnEnemyForgotten += onEnemyForgotten;
            Enemy.OnEnemyKnown += onEnemyKnown;
        }

        public void onEnemyForgotten(Enemy enemy)
        {
        }

        public void onEnemyKnown(Enemy enemy)
        {

        }

        public void Update()
        {
            getAngles();
            UpdateVisibleState(false);
            UpdateCanShootState(false);
        }

        public void Dispose()
        {
            Enemy.OnEnemyForgotten -= onEnemyForgotten;
            Enemy.OnEnemyKnown -= onEnemyKnown;
        }

        public float EnemyVelocity => EnemyTransform.PlayerVelocity;

        public bool FirstContactOccured { get; private set; }

        public bool ShallReportRepeatContact { get; set; }

        public bool ShallReportLostVisual { get; set; }

        private const float _repeatContactMinSeenTime = 12f;

        private const float _lostContactMinSeenTime = 12f;

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

        public float MaxVisionAngle { get; private set; }
        public float AngleToEnemy { get; private set; }
        public float AngleToEnemyHorizontal { get; private set; }

        private void getAngles()
        {
            if (_calcAngleTime < Time.time)
            {
                MaxVisionAngle = Enemy.Bot.Info.FileSettings.Core.VisibleAngle / 2f;
                _calcAngleTime = Time.time + _calcAngleFreq;
                AngleToEnemy = getAngleToEnemy(false);
                AngleToEnemyHorizontal = getAngleToEnemy(true);
            }
        }

        private float _calcAngleTime;
        private float _calcAngleFreq = 1f / ANGLE_CALC_PERSECOND;
        private const float ANGLE_CALC_PERSECOND = 30f;

        public void UpdateVisibleState(bool forceOff)
        {
            bool wasVisible = IsVisible;
            bool lineOfSight = InLineOfSight;

            if (forceOff)
            {
                IsVisible = false;
            }
            else
            {
                IsVisible =
                    EnemyInfo.IsVisible &&
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
                    OnFirstSeen?.Invoke(Enemy);
                }

                TimeLastSeen = Time.time;
                Enemy.UpdateCurrentEnemyPos(EnemyTransform.Position);
            }

            if (!IsVisible)
            {
                if (wasVisible)
                {
                    Enemy.UpdateLastSeenPosition(EnemyTransform.Position);
                }
                if (Seen && 
                    TimeSinceSeen > _lostContactMinSeenTime && 
                    _nextReportLostVisualTime < Time.time)
                {
                    _nextReportLostVisualTime = Time.time + 20f;
                    ShallReportLostVisual = true;
                }
                VisibleStartTime = -1f;
            }

            if (IsVisible != wasVisible)
            {
                OnVisionChange?.Invoke(Enemy, IsVisible);
                LastChangeVisionTime = Time.time;
            }
        }

        private float _nextReportLostVisualTime;

        public void UpdateCanShootState(bool forceOff)
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
        public EnemyVisionChecker(Enemy enemy) : base(enemy)
        {
            EnemyParts = new SAINEnemyParts(enemy.EnemyPlayer.PlayerBones, enemy.Player.IsYourPlayer);
            _transform = enemy.Bot.Transform;
        }

        private PersonTransformClass _transform;

        public void CheckVision()
        {
            if (EnemyParts.CheckBodyLineOfSight(_transform.EyePosition))
            {
                return;
            }
            if (EnemyParts.CheckRandomPartLineOfSight(_transform.EyePosition))
            {
                return;
            }
            // Do an extra check if the bot has this enemy as their active primary enemy or the enemy is not AI
            if (Enemy.IsCurrentEnemy && !Enemy.IsAI && 
                EnemyParts.CheckRandomPartLineOfSight(_transform.EyePosition))
            {
                return;
            }
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
            _indexMax = Parts.Count;
        }

        private bool IsYourPlayer;

        public bool LineOfSight
        {
            get
            {
                if (_lastSuccessTime + 0.2f > Time.time)
                {
                    return true;
                }
                foreach (var part in Parts.Values)
                {
                    if (part.LineOfSight)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private float _lastSuccessTime;

        public bool CheckBodyLineOfSight(Vector3 origin)
        {
            if (LineOfSight)
            {
                return true;
            }

            SAINEnemyPartData checkingPart = Parts[EBodyPart.Chest];
            if (checkingPart.CheckLineOfSight(origin))
            {
                _lastSuccessTime = Time.time;
                return true;
            }

            return false;
        }

        public bool CheckRandomPartLineOfSight(Vector3 origin)
        {
            if (LineOfSight)
            {
                return true;
            }

            if (_lastCheckSuccessPart != null)
            {
                if (_lastCheckSuccessPart.CheckLineOfSight(origin))
                {
                    _lastSuccessTime = Time.time;
                    return true;
                }
                _lastCheckSuccessPart = null;
            }

            SAINEnemyPartData checkingPart = getNextPart();
            if (checkingPart.CheckLineOfSight(origin))
            {
                _lastCheckSuccessPart = checkingPart;
                _lastSuccessTime = Time.time;
                return true;
            }

            _lastSuccessTime = 0f;
            return false;
        }

        private SAINEnemyPartData getNextPart()
        {
            SAINEnemyPartData result = null;
            EBodyPart epart = (EBodyPart)_index;
            if (!Parts.TryGetValue(epart, out result))
            {
                _index = 0;
                result = Parts[EBodyPart.Chest];
            }

            _index++;
            if (_index > _indexMax)
            {
                _index = 0;
            }

            return result;
        }

        private int _index;
        private readonly int _indexMax;

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
                    transform = bones.BifacialTransforms[PlayerBoneType.Spine];
                    if (transform == null)
                    {
                        Logger.LogError($"Transform Null {PlayerBoneType.Spine}");
                        transform = bones.BifacialTransforms[PlayerBoneType.Body];
                        if (transform == null)
                        {
                            Logger.LogError($"Transform Null {PlayerBoneType.Body}");
                        }
                    }

                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.SpineDown]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.SpineTop]);
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
            _indexMax = colliders.Count - 1;
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

        private BodyPartCollider getCollider()
        {
            if (_lastSuccessPart != null)
            {
                return _lastSuccessPart;
            }

            BodyPartCollider collider = Colliders[_index];
            _index++;
            if (_index > _indexMax)
            {
                _index = 0;
            }
            return collider;
        }

        private int _index;
        private readonly int _indexMax;

        public bool CheckLineOfSight(Vector3 origin)
        {
            if (LineOfSight)
            {
                return true;
            }

            if (_nextCheckTime > Time.time)
            {
                return false;
            }
            _nextCheckTime = Time.time + 0.1f;

            BodyPartCollider collider = getCollider();
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