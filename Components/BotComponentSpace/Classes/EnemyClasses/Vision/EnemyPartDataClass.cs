﻿using EFT;
using SAIN.Components;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyPartDataClass
    {
        public readonly Dictionary<ERaycastCheck, RaycastResult> RaycastResults = new Dictionary<ERaycastCheck, RaycastResult>();

        public float TimeSeen { get; private set; }
        public float TimeSinceSeen => IsVisible ? Time.time - TimeSeen : -1f;

        public void Update(Enemy enemy)
        {
            IsVisible =
                enemy.Vision.Angles.CanBeSeen &&
                RaycastResults[ERaycastCheck.Vision].InSight &&
                RaycastResults[ERaycastCheck.LineofSight].InSight;

            if (!IsVisible) {
                TimeSeen = 0f;
                return;
            }
            if (TimeSeen <= 0f) {
                TimeSeen = Time.time;
            }
        }

        public bool IsVisible { get; private set; }

        public EnemyPartDataClass(EBodyPart bodyPart, BifacialTransform transform, List<BodyPartCollider> colliders)
        {
            BodyPart = bodyPart;
            Transform = transform;
            Colliders = colliders;
            _indexMax = colliders.Count - 1;
            foreach (BodyPartCollider collider in colliders) {
                if (!_colliderDictionary.ContainsKey(collider.BodyPartColliderType)) {
                    _colliderDictionary.Add(collider.BodyPartColliderType, collider);
                }
            }
            RaycastResults.Add(ERaycastCheck.LineofSight, new RaycastResult());
            RaycastResults.Add(ERaycastCheck.Shoot, new RaycastResult());
            RaycastResults.Add(ERaycastCheck.Vision, new RaycastResult());

            //var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //sphere.GetComponent<Renderer>().material.color = Color.red;
            //sphere.GetComponent<Collider>().enabled = false;
            //sphere.name = $"los_line_{_debugCount++}";
            //sphere.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            //_debugLine = sphere;
            //
            //_debugLineRenderer = sphere.AddComponent<LineRenderer>();
            //// Modify the color and width of the line
            //_debugLineRenderer.material.color = Color.white;
            //_debugLineRenderer.startWidth = 0.02f;
            //_debugLineRenderer.endWidth = 0.01f;
        }

        public void UpdateDebugGizmos(Vector3 eyePosition)
        {
            _debugLineRenderer.SetPosition(0, eyePosition);
            if (!LineOfSight) {
                _debugLineRenderer.material.color = Color.white;

                return;
            }

            _debugLineRenderer.material.color = Color.red;
            if (LastSuccessPoint != null) {
                _debugLine.transform.position = LastSuccessPoint.Value;
                _debugLineRenderer.SetPosition(1, LastSuccessPoint.Value);
            }
        }

        private GameObject _debugLine;
        private LineRenderer _debugLineRenderer;

        private readonly Dictionary<EBodyPartColliderType, BodyPartCollider> _colliderDictionary = new Dictionary<EBodyPartColliderType, BodyPartCollider>();

        public void SetLineOfSight(Vector3 castPoint, EBodyPartColliderType colliderType, RaycastHit raycastHit, ERaycastCheck type, float time)
        {
            RaycastResults[type].Update(castPoint, _colliderDictionary[colliderType], raycastHit, time);
        }

        public BodyPartRaycast GetRaycast(Vector3 origin, float maxRange)
        {
            BodyPartCollider collider = getCollider();
            Vector3 castPoint = getCastPoint(origin, collider);

            return new BodyPartRaycast {
                CastPoint = castPoint,
                PartType = BodyPart,
                ColliderType = collider.BodyPartColliderType
            };
        }

        public Vector3? LastSuccessShootPoint { get; private set; }
        public Vector3? LastSuccessPoint { get; private set; }

        public readonly EBodyPart BodyPart;
        public readonly List<BodyPartCollider> Colliders;
        public readonly BifacialTransform Transform;

        public float TimeSinceLastVisionCheck => RaycastResults[ERaycastCheck.LineofSight].TimeSinceChecked;
        public float TimeSinceLastVisionSuccess => RaycastResults[ERaycastCheck.LineofSight].TimeSinceSuccess;
        public bool LineOfSight => RaycastResults[ERaycastCheck.LineofSight].InSight;
        public bool CanShoot => RaycastResults[ERaycastCheck.Shoot].InSight;

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

        private BodyPartCollider _lastSuccessPart;
    }
}