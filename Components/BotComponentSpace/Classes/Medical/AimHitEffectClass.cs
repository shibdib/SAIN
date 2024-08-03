using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class AimHitEffectClass : BotMedicalBase, IBotClass
    {
        private Vector3 _affectVector = Vector3.zero;
        private float _affectAmount;
        private float _finishDelay = 1f;
        private bool _affectActive;
        private float _timeFinished;

        private float EFFECT_MIN_ANGLE => _settings.DAMAGE_BASE_MIN_ANGLE;
        private float EFFECT_MAX_ANGLE => _settings.DAMAGE_BASE_MAX_ANGLE;
        private float DAMAGE_BASELINE => _settings.DAMAGE_BASELINE;
        private float DAMAGE_MIN_MOD => _settings.DAMAGE_MIN_MOD;
        private float DAMAGE_MAX_MOD => _settings.DAMAGE_MAX_MOD;
        private float DAMAGE_MANUAL_MODIFIER => _settings.DAMAGE_MANUAL_MODIFIER;

        private HitEffectSettings _settings => GlobalSettingsClass.Instance.Aiming.HitEffects;

        public AimHitEffectClass(SAINBotMedicalClass medical) : base(medical)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public Vector3 ApplyEffect(Vector3 dir)
        {
            if (this._affectActive) {
                this.decayAffect();
                Vector3 affect = this._affectVector * this._affectAmount;
                Vector3 result = dir.normalized + affect;
                DebugGizmos.Ray(Bot.Transform.WeaponRoot, result, Color.yellow, 1f, 0.05f, true, 5f);
                return result;
            }
            return dir;
        }

        public void GetHit(float damageNumber)
        {
            float mod = damageNumber / DAMAGE_BASELINE;
            mod = Mathf.Clamp(mod, DAMAGE_MIN_MOD, DAMAGE_MAX_MOD) * DAMAGE_MANUAL_MODIFIER;

            var aimSettings = BotOwner.Settings.FileSettings.Aiming;
            float minAngle = Mathf.Clamp(EFFECT_MIN_ANGLE * mod, 0f, 90f);
            float maxAngle = Mathf.Clamp(EFFECT_MAX_ANGLE * mod, 0f, 90f);
            //float minAngle = aimSettings.BASE_HIT_AFFECTION_MIN_ANG * mod;
            //float maxAngle = aimSettings.BASE_HIT_AFFECTION_MAX_ANG * mod;
            float x = UnityEngine.Random.Range(-minAngle, -maxAngle);
            float y = (float)EFTMath.RandomSing() * UnityEngine.Random.Range(minAngle, maxAngle);
            Vector3 lookDir = Bot.Transform.LookDirection;
            this._affectVector = Vector.Rotate(_affectVector + lookDir, x, y, y) - lookDir;
            DebugGizmos.Ray(Bot.Transform.WeaponRoot, _affectVector + lookDir, Color.green, 1f, 0.15f, true, 10f);
            DebugGizmos.Ray(Bot.Transform.WeaponRoot, lookDir, Color.white, 1f, 0.15f, true, 10f);
            this._affectActive = true;
            this._finishDelay = aimSettings.BASE_HIT_AFFECTION_DELAY_SEC * mod * UnityEngine.Random.Range(0.8f, 1.2f);
            this._timeFinished = Time.time + this._finishDelay;
        }

        public void decayAffect()
        {
            if (this._affectActive) {
                float timeRemaining = this._timeFinished - Time.time;
                if (timeRemaining <= 0f) {
                    this._affectActive = false;
                    _affectVector = Vector3.zero;
                    return;
                }
                this._affectAmount = timeRemaining / this._finishDelay;
            }
        }
    }
}