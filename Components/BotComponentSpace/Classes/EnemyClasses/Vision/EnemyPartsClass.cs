using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyPartsClass : EnemyBase
    {
        private const float LINEOFSIGHT_TIME = 0.25f;

        public EnemyPartsClass(Enemy enemy) : base(enemy)
        {
            IsYourPlayer = enemy.Player.IsYourPlayer;
            createPartDatas(enemy.Player.PlayerBones);
            _indexMax = Parts.Count;
        }

        public bool CanShoot => Enemy.Events.OnEnemyCanShootChanged.Value;
        public bool LineOfSight => TimeSinceInLineOfSight < LINEOFSIGHT_TIME;
        public float TimeSinceInLineOfSight => Time.time - _timeLastInSight;
        public Vector3 LastSuccessPosition { get; private set; }
        public Dictionary<EBodyPart, EnemyPartDataClass> Parts { get; } = new Dictionary<EBodyPart, EnemyPartDataClass>();

        public void Update()
        {
            var visiblePart = findPartInLOS();
            if (visiblePart != null) {
                _timeLastInSight = Time.time;
                if (visiblePart.LastSuccessPoint != null)
                    LastSuccessPosition = visiblePart.LastSuccessPoint.Value;

                return;
            }
        }

        private EnemyPartDataClass findPartInLOS()
        {
            foreach (var part in Parts.Values)
                if (part.LineOfSight)
                    return part;
            return null;
        }

        public bool CheckCanShoot(bool canShootHead)
        {
            bool canShoot = false;
            bool isAI = Enemy.IsAI;
            Vector3 firePort = Enemy.Bot.Transform.WeaponData.FirePort;

            foreach (var part in Parts.Values) {
                if (!canShootHead && part.BodyPart == EBodyPart.Head) {
                    continue;
                }
                if (part.CheckCanShoot(firePort, isAI))
                    canShoot = true;
            }
            return canShoot;
        }

        public bool CheckBodyLineOfSight(Vector3 origin, float maxRange, out Vector3? successPoint)
        {
            EnemyPartDataClass checkingPart = Parts[EBodyPart.Chest];
            if (checkingPart.CheckLineOfSight(origin, maxRange, out successPoint)) {
                if (successPoint != null)
                    LastSuccessPosition = successPoint.Value;

                _lastSuccessTime = Time.time;
                return true;
            }
            successPoint = null;
            return false;
        }

        public bool CheckHeadLineOfSight(Vector3 origin, float maxRange, out Vector3? successPoint)
        {
            EnemyPartDataClass checkingPart = Parts[EBodyPart.Head];
            if (checkingPart.CheckLineOfSight(origin, maxRange, out successPoint)) {
                if (successPoint != null)
                    LastSuccessPosition = successPoint.Value;

                _lastSuccessTime = Time.time;
                return true;
            }
            successPoint = null;
            return false;
        }

        public bool CheckRandomPartLineOfSight(Vector3 origin, float maxRange, out Vector3? successPoint)
        {
            bool inSight = false;

            if (_lastCheckSuccessPart != null) {
                if (_lastCheckSuccessPart.CheckLineOfSight(origin, maxRange, out successPoint)) {
                    if (successPoint != null)
                        LastSuccessPosition = successPoint.Value;

                    _lastSuccessTime = Time.time;
                    inSight = true;
                }
                else {
                    _lastCheckSuccessPart = null;
                }
            }

            EnemyPartDataClass checkingPart = GetNextPart();

            if (checkingPart == _lastCheckSuccessPart) {
                successPoint = LastSuccessPosition;
                return inSight;
            }

            if (checkingPart.CheckLineOfSight(origin, maxRange, out successPoint)) {
                if (successPoint != null)
                    LastSuccessPosition = successPoint.Value;

                _lastCheckSuccessPart = checkingPart;
                _lastSuccessTime = Time.time;
                inSight = true;
            }
            else {
                successPoint = null;
                _lastSuccessTime = 0f;
            }

            return inSight;
        }

        public EnemyPartDataClass GetNextPart()
        {
            EnemyPartDataClass result = null;
            EBodyPart epart = (EBodyPart)_index;
            if (!Parts.TryGetValue(epart, out result)) {
                _index = 0;
                result = Parts[EBodyPart.Chest];
            }

            _index++;
            if (_index > _indexMax) {
                _index = 0;
            }

            if (result == null) {
                result = Parts.PickRandom().Value;
            }
            return result;
        }

        private void createPartDatas(PlayerBones bones)
        {
            var parts = Enemy.EnemyPlayerComponent.BodyParts.Parts;
            foreach (var bodyPart in parts) {
                Parts.Add(bodyPart.Key, new EnemyPartDataClass(bodyPart.Key, bodyPart.Value.Transform, bodyPart.Value.Colliders));
            }
        }

        private void findParts(EBodyPart bodyPart, out BifacialTransform transform, List<BodyPartCollider> partColliders, PlayerBones bones)
        {
            switch (bodyPart) {
                default:
                    transform = bones.BifacialTransforms[PlayerBoneType.Spine];
                    if (transform == null) {
                        Logger.LogError($"Transform Null {PlayerBoneType.Spine}");
                        transform = bones.BifacialTransforms[PlayerBoneType.Body];
                        if (transform == null) {
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

        private bool IsYourPlayer;
        private float _lastSuccessTime;
        private float _timeLastInSight;
        private int _index;
        private readonly int _indexMax;
        private EnemyPartDataClass _lastCheckSuccessPart;
    }
}