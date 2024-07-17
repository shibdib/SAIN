using Comfort.Common;
using EFT;
using EFT.UI.Ragfair;
using EFT.Visual;
using SAIN.Helpers;
using UnityEngine;

namespace SAIN.Components
{
    public class LightTrigger : MonoBehaviour
    {
        private void Awake()
        {
            _light = this.GetComponent<Light>();
            _sphereCollider = this.gameObject.AddComponent<SphereCollider>();
            _sphereCollider.enabled = true;

            _lightAngle = _light.spotAngle;
            _lightType = _light.type;
            _lightRange = Mathf.Clamp(_light.range * 1.25f, 0f, 100f);
            _sphereCollider.radius = _lightRange;
            
            this.transform.rotation = _light.transform.rotation;
            this.transform.position = _light.transform.position;
            _sphereCollider.transform.position = _light.transform.position;
            //_sphereCollider.transform.localScale = _light.transform.localScale;
            //_sphereCollider.transform.localPosition = _light.transform.localPosition;
            _sphereCollider.radius = _lightRange;
            _sphereCollider.isTrigger = true;

            _lineRenderer = this.gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material.color = Color.white;
            _lineRenderer.startWidth = 0.1f;
            _lineRenderer.endWidth = 0.1f;
            _lineRenderer.SetPosition(0, _sphereCollider.transform.position);
            _lineRenderer.SetPosition(1, _sphereCollider.transform.position + (this.transform.forward * _lightRange));
        }

        public void OnTriggerEnter(Collider other)
        {
            var layer = other.gameObject.layer;
            if (layer != LayerMaskClass.PlayerLayer && layer != LayerMaskClass.PlayerMask) {
                return;
            }
            Player player = Singleton<GameWorld>.Instance.GetPlayerByCollider(other);
            if (player == null || player.IsAI) {
                Logger.LogDebug($"playernull or ai : name {other.gameObject?.name}");
                return;
            }
            //_lineRenderer.material.color = Color.blue;
        }

        public void OnTriggerStay(Collider other)
        {
            var layer = other.gameObject.layer;
            if (layer != LayerMaskClass.PlayerLayer && layer != LayerMaskClass.PlayerMask) {
                return;
            }
            Logger.LogDebug($"OnTriggerStay: {other?.gameObject?.name}");
            _lineRenderer.SetPosition(1, other.transform.position);
        }

        public void OnTriggerExit(Collider other)
        {
            var layer = other.gameObject.layer;
            if (layer != LayerMaskClass.PlayerLayer && layer != LayerMaskClass.PlayerMask) {
                return;
            }
            Player player = Singleton<GameWorld>.Instance.GetPlayerByCollider(other);
            if (player == null || player.IsAI) {
                return;
            }
            Logger.LogDebug($"OnTriggerExit: {other.gameObject?.name}");
            //_lineRenderer.material.color = Color.white;
        }

        private void Update()
        {
        }

        private void setPosition()
        {
            this.transform.rotation = _light.transform.rotation;
            this.transform.position = _light.transform.position;
            _sphereCollider.transform.position = _light.transform.position;
            //_sphereCollider.transform.localScale = _light.transform.localScale;
            //_sphereCollider.transform.localPosition = _light.transform.localPosition;
            _sphereCollider.radius = _lightRange;
        }

        private void OnEnable()
        {
            if (_light == null) return;
            setPosition();
            if (_debugSphere == null) {
                //_debugSphere = DebugGizmos.Sphere(_sphereCollider.transform.position, _sphereCollider.radius, DebugGizmos.RandomColor, false, -1f);
            }
            if (_debugSphere != null) {
                _debugSphere.SmartEnable();
                _debugSphere.transform.position = _sphereCollider.transform.position;
            }
        }

        private void OnDisable()
        {
            _debugSphere?.SmartDisable();
        }

        private void OnDestroy()
        {
            Destroy(_debugSphere);
        }

        private Vector3 _lineEnd()
        {
            return _sphereCollider.transform.position + (this.transform.forward * _lightRange);
        }

        private GameObject _debugSphere;
        private LineRenderer _lineRenderer;
        private LightType _lightType;
        private float _lightAngle;
        private float _lightRange;
        private Light _light;
        private SphereCollider _sphereCollider;
    }

    public class LightComponent : MonoBehaviour
    {
        public LightTrigger LightTrigger { get; private set; }

        private void Awake()
        {
            _light = this.GetComponent<Light>();
            LightTrigger = this.gameObject.AddComponent<LightTrigger>();
        }

        private void Update()
        {
        }

        private void OnDestroy()
        {
        }

        private Light _light;
        private static int _count;
    }
}