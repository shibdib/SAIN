using Comfort.Common;
using EFT;
using EFT.Interactive;
using SAIN.Components.PlayerComponentSpace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components
{
    public class LightTrigger : MonoBehaviour, IPhysicsTrigger, IPhysicsTriggerWithStay
    {
        public bool LightActive {  get; private set; }
        public string Description { get; private set; }

        private LightComponent _lightComponent;

        private void Awake()
        {
            _lightComponent = this.GetComponent<LightComponent>();

            _lightAngle = _light.spotAngle;
            _lightType = _light.type;
            _lightRange = Mathf.Clamp(_light.range * 1f, 0f, 100f);
            _lightRange_SQR = _lightRange * _lightRange;

            this.transform.rotation = _light.transform.rotation;
            this.transform.position = _light.transform.position + (this.transform.forward * 0.33f);

            _collider = this.gameObject.AddComponent<SphereCollider>();
            _collider.enabled = true;
            _collider.isTrigger = true;
            _collider.radius = _lightRange;
            _collider.transform.position = this.transform.position;


            //_lineRenderer = this.gameObject.AddComponent<LineRenderer>();
            //_lineRenderer.material.color = Color.white;
            //_lineRenderer.startWidth = 0.05f;
            //_lineRenderer.endWidth = 0.025f;
            //_lineRenderer.SetPosition(0, _light.transform.position);
            //_lineRenderer.SetPosition(1, _light.transform.position + (this.transform.forward * _lightRange));
        }

        public void OnTriggerEnter(Collider other)
        {
        }

        public void OnTriggerStay(Collider other)
        {
            var layer = other.gameObject.layer;
            if (layer != LayerMaskClass.PlayerLayer && layer != LayerMaskClass.PlayerMask) {
                return;
            }
            if (_light == null) {
                return;
            }
            Player player = Singleton<GameWorld>.Instance.GetPlayerByCollider(other);
            if (player == null || player.IsAI) {
                return;
            }
            Vector3 bodyPosition = player.MainParts[BodyPartType.body].Position;
            if (inRangeOfLight(player, out float illuminationLevel, out float sqrMag)) {

                float intensity = _light.intensity / 5f;
                intensity = Mathf.Clamp(intensity, 0.1f, 1f);
                illuminationLevel *= intensity;
                GameWorldComponent.Instance?.PlayerTracker.GetPlayerComponent(player.ProfileId)?.Illumination.SetIllumination(true, illuminationLevel, this, sqrMag);
                return;
            }
            //resetLine();
        }

        private bool inRangeOfLight(Player player, out float illuminationLevel, out float sqrMagnitude)
        {
            Vector3 bodyPosition = player.MainParts[BodyPartType.body].Position;
            Vector3 playerDirection = bodyPosition - this.transform.position;

            if (!withinAngle(player, playerDirection)) {
                illuminationLevel = 0;
                sqrMagnitude = float.MaxValue;
                return false;
            }

            sqrMagnitude = Mathf.Clamp(playerDirection.sqrMagnitude, 0f, _lightRange_SQR);
            float minDist = MAX_ILLUM_START_RATIO * _lightRange_SQR;

            if (sqrMagnitude <= minDist) {
                illuminationLevel = 1f;
                return true;
            }

            float num = _lightRange_SQR - minDist;
            float num2 = sqrMagnitude - minDist;
            illuminationLevel = 1f - (num2 / num);
            return true;
        }

        private const float MAX_ILLUM_START_RATIO = 0.5f;

        private bool withinAngle(Player player, Vector3 playerDirection)
        {
            if (_lightType != LightType.Spot) {
                return true;
            }
            if (Vector3.Angle(playerDirection, this.transform.forward) <= _lightAngle) {
                return true;
            }
            return false;
        }

        public void OnTriggerExit(Collider other)
        {
            var layer = other.gameObject.layer;
            if (layer != LayerMaskClass.PlayerLayer && layer != LayerMaskClass.PlayerMask) {
                return;
            }
            if (_light == null) {
                return;
            }
            Player player = Singleton<GameWorld>.Instance.GetPlayerByCollider(other);
            if (player == null || player.IsAI) {
                return;
            }
            GameWorldComponent.Instance?.PlayerTracker.GetPlayerComponent(player.ProfileId)?.Illumination.SetIllumination(false, 0f, this, float.MaxValue);
            //resetLine();
        }

        private void resetLine()
        {
            _lineRenderer.material.color = Color.white;
            _lineRenderer.SetPosition(1, _light.transform.position + (this.transform.forward * _lightRange));
        }

        private void Update()
        {
            if (_nextCheckActiveTime < Time.time) {
                _nextCheckActiveTime = Time.time + CHECK_ACTIVE_FREQ;

                bool lightOn = _lightComponent.LightActive;
                if (lightOn && !_collider.enabled) {
                    LightActive = true;
                    _collider.enabled = true;
                }
                if (!lightOn && _collider.enabled) {
                    LightActive = false;
                    _collider.enabled = false;
                }
            }
        }

        private const float CHECK_ACTIVE_FREQ = 0.2f;
        private float _nextCheckActiveTime;

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
            Destroy(_debugSphere);
        }

        private SphereCollider _collider;
        private GameObject _debugSphere;
        private LineRenderer _lineRenderer;
        private LightType _lightType;
        private float _lightAngle;
        private float _lightRange;
        private float _lightRange_SQR;
        private Light _light => _lightComponent.Light;
    }

    public class LightComponent : MonoBehaviour
    {
        public bool LightActive => LampController?.Enabled == true || VolumetricLight?.enabled == true || Light?.enabled == true;
        public LampController LampController {  get; private set; }
        public VolumetricLight VolumetricLight { get; private set; }
        public Light Light { get; private set; }
        public LightTrigger LightTrigger { get; private set; }

        public void Init(Light light)
        {
            Light = light;
            VolumetricLight = light.GetComponent<VolumetricLight>();
            LightTrigger = this.gameObject.AddComponent<LightTrigger>();
        }

        public void Init(LampController lampController)
        {
            LampController = lampController;
        }

        private void Update()
        {
        }

        private void OnDestroy()
        {
        }

    }
}