using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;

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

        public Vector3? LastSuccessPoint => _lastSuccessCastPoint;

        private Vector3 _position;
        private int _posCachedForFrame;
        public readonly EBodyPart BodyPart;
        public readonly List<BodyPartCollider> Colliders;
        public readonly BifacialTransform Transform;

        public float TimeSinceLastCheck => Time.time - _lastCheckTime;
        public float TimeSinceLastSuccess => Time.time - _lastSuccessTime;
        public bool LineOfSight => _lastSuccessTime + 0.25f > Time.time;

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

        public bool CheckLineOfSight(Vector3 origin, float maxRange, out Vector3? successPoint)
        {
            if (LineOfSight)
            {
                successPoint = _lastSuccessCastPoint;
                return true;
            }

            _lastCheckTime = Time.time;
            BodyPartCollider collider = getCollider();
            Vector3 castPoint = getCastPoint(origin, collider);
            //Vector3 castPoint = _lastSuccessCastPoint ?? getCastPoint(origin, collider);

            Vector3 direction = castPoint - origin;

            float maxRayDistance = direction.magnitude;
            bool lineOfSight = !Physics.Raycast(origin, direction, out var hit, maxRayDistance, LayerMaskClass.HighPolyWithTerrainMask);

            if (lineOfSight)
            {
                _lastSuccessTime = Time.time;
                _lastSuccessPart = collider;
                _lastSuccessCastPoint = castPoint;
            }
            else
            {
                _lastSuccessPart = null;
                _lastSuccessCastPoint = null;
            }

            successPoint = _lastSuccessCastPoint;

            if (SAINPlugin.DebugMode &&
                _nextdrawTime < Time.time)
            {
                _nextdrawTime = Time.time + 0.1f;
                if (lineOfSight)
                {
                    DebugGizmos.Sphere(castPoint, 0.025f, Color.red, true, 10f);
                    DebugGizmos.Sphere(origin, 0.025f, Color.red, true, 1f);
                    DebugGizmos.Line(castPoint, origin, Color.red, 0.005f, true, 0.5f);
                    Logger.LogDebug($"{BodyPart} : {maxRayDistance} : {castPoint}");
                }
                else
                {
                    DebugGizmos.Sphere(castPoint, 0.025f, Color.white, true, 10f);
                    DebugGizmos.Sphere(origin, 0.025f, Color.white, true, 1f);
                    DebugGizmos.Line(castPoint, hit.point, Color.white, 0.005f, true, 0.5f);
                }
            }
            return lineOfSight;
        }

        private float _nextdrawTime;

        private float _lastSuccessTime;

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

        private float _lastCheckTime;
        private BodyPartCollider _lastSuccessPart;
        private Vector3? _lastSuccessCastPoint;
    }
}