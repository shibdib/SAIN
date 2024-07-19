using EFT;
using SAIN.Components;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyPartDataClass
    {
        public EnemyPartDataClass(EBodyPart bodyPart, BifacialTransform transform, List<BodyPartCollider> colliders)
        {
            BodyPart = bodyPart;
            Transform = transform;
            Colliders = colliders;
            _indexMax = colliders.Count - 1;
        }

        public Vector3 Position {
            get
            {
                if (Time.frameCount != this._posCachedForFrame &&
                    Transform != null &&
                    Transform.Original != null) {
                    this._position = this.Transform.position;
                    this._posCachedForFrame = Time.frameCount;
                }
                return this._position;
            }
        }

        public void SetLineOfSight(BodyPartRaycast result)
        {
        }

        public BodyPartRaycast GetRaycast(Vector3 origin, float maxRange)
        {
            BodyPartCollider collider = getCollider();
            Vector3 castPoint = getCastPoint(origin, collider);

            return new BodyPartRaycast {
                RaycastHit = new RaycastHit(),
                CastPoint = castPoint,
                PartData = this,
                PartType = collider.BodyPartColliderType,
                MaxRange = maxRange
            };
        }

        public Vector3? LastSuccessShootPoint { get; private set; }
        public Vector3? LastSuccessPoint { get; private set; }

        private Vector3 _position;
        private int _posCachedForFrame;
        public readonly EBodyPart BodyPart;
        public readonly List<BodyPartCollider> Colliders;
        public readonly BifacialTransform Transform;

        public float TimeSinceLastVisionCheck => Time.time - _lastCheckTime;
        public float TimeSinceLastVisionSuccess => Time.time - _lastVisionSuccessTime;
        public bool LineOfSight => _lastVisionSuccessTime + 0.25f > Time.time;
        public bool CanShoot => _lastShootSucessTime + 0.25f > Time.time;

        private BodyPartCollider getCollider()
        {
            if (_lastSuccessPart != null) {
                return _lastSuccessPart;
            }

            BodyPartCollider collider = Colliders[_index];
            _index++;
            if (_index > _indexMax) {
                _index = 0;
            }
            return collider;
        }

        private int _index;
        private readonly int _indexMax;

        public bool CheckLineOfSight(Vector3 origin, float maxRange, out Vector3? successPoint)
        {
            if (LineOfSight) {
                successPoint = LastSuccessPoint;
                return true;
            }

            _lastCheckTime = Time.time;
            BodyPartCollider collider = getCollider();
            Vector3 castPoint = getCastPoint(origin, collider);
            //Vector3 castPoint = _lastSuccessCastPoint ?? getCastPoint(origin, collider);

            Vector3 direction = castPoint - origin;

            float maxRayDistance = direction.magnitude;
            bool lineOfSight = !Physics.Raycast(origin, direction, out var hit, maxRayDistance, LayerMaskClass.HighPolyWithTerrainMask);

            if (lineOfSight) {
                _lastVisionSuccessTime = Time.time;
                _lastSuccessPart = collider;
                LastSuccessPoint = castPoint;
            }
            else {
                _lastSuccessPart = null;
            }

            successPoint = LastSuccessPoint;

            if (SAINPlugin.DebugMode &&
                _nextdrawTime < Time.time) {
                //_nextdrawTime = Time.time + 0.1f;
                if (lineOfSight) {
                    DebugGizmos.Sphere(castPoint, 0.025f, Color.red, true, 10f);
                    DebugGizmos.Sphere(origin, 0.025f, Color.red, true, 1f);
                    DebugGizmos.Line(castPoint, origin, Color.red, 0.005f, true, 0.5f);
                    //Logger.LogDebug($"{BodyPart} : {maxRayDistance} : {castPoint}");
                }
                else {
                    //DebugGizmos.Sphere(castPoint, 0.025f, Color.white, true, 10f);
                    //DebugGizmos.Sphere(origin, 0.025f, Color.white, true, 1f);
                    //DebugGizmos.Line(castPoint, hit.point, Color.white, 0.005f, true, 0.5f);
                }
            }
            return lineOfSight;
        }

        private const float CHECK_SHOOT_FREQ = 0.1f;
        private const float CHECK_SHOOT_FREQ_AI = 0.2f;

        public bool CheckCanShoot(Vector3 firePort, bool isAI)
        {
            if (!LineOfSight) {
                return false;
            }
            if (LastSuccessPoint == null) {
                return false;
            }
            if (_nextCheckShootTime > Time.time) {
                return CanShoot;
            }
            _nextCheckShootTime = Time.time + (isAI ? CHECK_SHOOT_FREQ_AI : CHECK_SHOOT_FREQ);

            Vector3 point = LastSuccessPoint.Value;
            bool canShoot = canShootToTarget(point, firePort);

            if (!canShoot &&
                !isAI) {
                BodyPartCollider part = getCollider();
                point = getCastPoint(firePort, part);
                canShoot = canShootToTarget(point, firePort);
            }

            if (canShoot) {
                _lastShootSucessTime = Time.time;
                LastSuccessShootPoint = point;
            }
            return canShoot;
        }

        private bool canShootToTarget(Vector3 pointToCheck, Vector3 firePort)
        {
            Vector3 direction = pointToCheck - firePort;
            float distance = direction.magnitude;
            bool canShoot = !Physics.Raycast(firePort, direction, distance, LayerMaskClass.HighPolyWithTerrainMask);
            return canShoot;
        }

        private float _nextCheckShootTime;

        private float _nextdrawTime;

        private float _lastVisionSuccessTime;

        private Vector3 getCastPoint(Vector3 origin, BodyPartCollider collider)
        {
            float size = getColliderMinSize(collider);
            //Logger.LogInfo(size);
            Vector3 random = UnityEngine.Random.insideUnitSphere * size;
            Vector3 result = collider.Collider.ClosestPoint(collider.transform.position + random);
            return result;
        }

        private float getColliderMinSize(BodyPartCollider collider)
        {
            if (collider.Collider == null) {
                return 0f;
            }
            Vector3 bounds = collider.Collider.bounds.size;
            float lowest = bounds.x;
            if (bounds.y < lowest) {
                lowest = bounds.y;
            }
            if (bounds.z < lowest) {
                lowest = bounds.z;
            }
            return lowest;
        }

        private float _lastShootSucessTime;
        private float _lastCheckTime;
        private BodyPartCollider _lastSuccessPart;
    }
}