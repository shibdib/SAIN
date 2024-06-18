using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System.Collections;
using System.Text;
using UnityEngine;
using static EFT.InventoryLogic.Weapon;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class Recoil : SAINBase, ISAINClass
    {
        public Vector3 CurrentRecoilOffset { get; private set; } = Vector3.zero;
        private Vector3 _lookDir => Player.LookDirection * 10f;

        public float ArmInjuryModifier => calcModFromInjury(Bot.BotHitReaction.LeftArmInjury) * calcModFromInjury(Bot.BotHitReaction.RightArmInjury);

        public Recoil(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            Bot.OnBotDisabled += stopLoop;
        }

        private float calcModFromInjury(EInjurySeverity severity)
        {
            switch (severity)
            {
                default:
                    return 1f;

                case EInjurySeverity.Injury:
                    return 1.15f;

                case EInjurySeverity.HeavyInjury:
                    return 1.35f;

                case EInjurySeverity.Destroyed:
                    return 1.65f;
            }
        }

        public void Update()
        {
            if (_recoilFinished)
            {
                stopLoop();
                _recoilFinished = false;
                _shotRegistered = false;
                _barrelRising = false;
                return;
            }
        }

        private void checkStartLoop()
        {
            if (_recoilCoroutine == null)
            {
                _recoilCoroutine = Bot.StartCoroutine(RecoilLoop());
            }
        }

        private void stopLoop()
        {
            if (_recoilCoroutine != null)
            {
                Bot.StopCoroutine(_recoilCoroutine);
                _recoilCoroutine = null;
            }
        }

        private IEnumerator RecoilLoop()
        {
            StringBuilder stringBuilder = SAINPlugin.DebugSettings.DebugRecoilCalculations ? new StringBuilder() : null;
            while (true)
            {
                applyShot(stringBuilder);
                calcBarrelRise(stringBuilder);
                calcDecay(stringBuilder);

                if (_recoilFinished)
                {
                    CurrentRecoilOffset = Vector3.zero;
                    break;
                }

                yield return null;
            }

            if (stringBuilder != null)
                Logger.LogDebug(stringBuilder.ToString());
        }

        private void applyShot(StringBuilder stringBuilder)
        {
            if (_shotRegistered)
            {
                _shotRegistered = false;
                _barrelRising = true;
                _recoilFinished = false;
                calculateRecoil(stringBuilder);
            }
        }

        private void calcBarrelRise(StringBuilder stringBuilder)
        {
            if (!_barrelRising)
            {
                return;
            }

            float riseTime = Time.deltaTime * _barrelRiseCoef;
            _barrelRiseTime += riseTime;
            stringBuilder?.AppendLine($"Barrel Rise Progress [{_barrelRiseTime}] Rise Step: {riseTime}");

            if (_barrelRiseTime > 1)
            {
                _barrelRising = false;
                _barrelRiseTime = 1f;
            }

            CurrentRecoilOffset = Vector3.Lerp(CurrentRecoilOffset, _recoilOffsetTarget, _barrelRiseTime);

            if (!_barrelRising)
            {
                _barrelRiseTime = 0f;
                _recoilOffsetTarget = Vector3.zero;
            }
        }

        private static float _barrelRiseCoef => SAINPlugin.LoadedPreset.GlobalSettings.Shoot.RecoilRiseCoef;
        private float _barrelRiseTime;

        private void calcDecay(StringBuilder stringBuilder)
        {
            if (_recoilFinished)
            {
                return;
            }

            float decayTime = Time.deltaTime * _recoilDecayCoef;
            _barrelRecoveryTime += decayTime;
            stringBuilder?.AppendLine($"Recoil Decay Progress [{_barrelRecoveryTime}] Decay Step: {decayTime}");

            if (_barrelRecoveryTime > 1)
            {
                _barrelRecoveryTime = 1;
                _recoilFinished = true;
            }

            CurrentRecoilOffset = Vector3.Lerp(CurrentRecoilOffset, Vector3.zero, _barrelRecoveryTime);
            if (_barrelRising )
            {
                _recoilOffsetTarget = Vector3.Lerp(_recoilOffsetTarget, Vector3.zero, _barrelRecoveryTime);
            }

            if (_recoilFinished)
            {
                _barrelRecoveryTime = 0;
            }
        }

        private static float _recoilDecayCoef => SAINPlugin.LoadedPreset.GlobalSettings.Shoot.RecoilDecayCoef;
        private float _barrelRecoveryTime;

        public void Dispose()
        {
            Bot.OnBotDisabled -= stopLoop;
        }

        public void WeaponShot()
        {
            _shotRegistered = true; 
            checkStartLoop();
        }

        /*
        public Vector3 CalculateRecoil()
        {
            float weaponhorizrecoil = CalcHorizRecoil(Bot.Info.WeaponInfo.RecoilForceUp);
            float weaponvertrecoil = CalcVertRecoil(Bot.Info.WeaponInfo.RecoilForceBack);

            float addRecoil = SAINPlugin.LoadedPreset.GlobalSettings.Shoot.AddRecoil;
            float horizRecoil = (1f * (weaponhorizrecoil + addRecoil));
            float vertRecoil = (1f * (weaponvertrecoil + addRecoil));

            float maxrecoil = SAINPlugin.LoadedPreset.GlobalSettings.Shoot.MaxRecoil;

            float randomHorizRecoil = Random.Range(-horizRecoil, horizRecoil);
            float randomvertRecoil = Random.Range(-vertRecoil, vertRecoil);
            Vector3 newRecoil = new Vector3(randomHorizRecoil, randomvertRecoil, randomHorizRecoil);
            newRecoil = MathHelpers.VectorClamp(newRecoil, -maxrecoil, maxrecoil) * RecoilMultiplier;

            if (Player.IsInPronePose)
            {
                newRecoil *= 0.8f;
            }
            var shootController = BotOwner.WeaponManager.ShootController;
            if (shootController != null && shootController.IsAiming == true)
            {
                newRecoil *= 0.8f;
            }
            if (_armsInjured)
            {
                newRecoil *= Mathf.Sqrt(ArmInjuryModifier);
            }

            Vector3 totalRecoil = newRecoil + currentRecoil;
            return totalRecoil;
        }
        */

        private void calculateRecoil(StringBuilder stringBuilder)
        {
            float addRecoil = SAINPlugin.LoadedPreset.GlobalSettings.Shoot.AddRecoil;
            float recoilMod = calcRecoilMod();
            float recoilTotal = Bot.Info.WeaponInfo.CurrentWeapon.RecoilTotal;

            float vertRecoil = (CalcVertRecoil(recoilTotal) + addRecoil) * recoilMod;
            float randomvertRecoil = Random.Range(vertRecoil / 2f, vertRecoil) * randomSign();

            float horizRecoil = (CalcHorizRecoil(recoilTotal) + addRecoil) * recoilMod;
            float randomHorizRecoil = Random.Range(horizRecoil / 2f, horizRecoil) * randomSign();

            Vector3 result = Vector.Rotate(_lookDir, randomHorizRecoil, randomvertRecoil, randomHorizRecoil);
            result -= _lookDir;
            _recoilOffsetTarget += result;

            stringBuilder?.AppendLine($"Recoil! New Recoil: [{result.magnitude}] " +
                $"Current Total Recoil Magnitude: [{_recoilOffsetTarget.magnitude}] " +
                $"Vertical: [ [{vertRecoil}] : Randomized [{randomvertRecoil}] ]" +
                $" Horizontal: [ [{horizRecoil}] : Randomized [{randomHorizRecoil}] ] " +
                $"Modifiers [ Add: [{addRecoil}] Multi: [{recoilMod}] ]");
        }

        private float randomSign()
        {
            return EFTMath.RandomBool() ? -1 : 1;
        }

        private float calcRecoilMod()
        {
            float recoilMod = 1f * RecoilMultiplier;

            if (Player.IsInPronePose)
            {
                recoilMod *= 0.6f;
            }
            else if (Player.Pose == EPlayerPose.Duck)
            {
                recoilMod *= 0.8f;
            }

            if (BotOwner.WeaponManager?.ShootController?.IsAiming == true)
            {
                recoilMod *= 0.85f;
            }
            if (Player.Velocity.magnitude < 0.5f)
            {
                recoilMod *= 0.85f;
            }
            if (_armsInjured)
            {
                recoilMod *= Mathf.Sqrt(ArmInjuryModifier);
            }

            return recoilMod;
        }

        float CalcVertRecoil(float recoilVal)
        {
            float result = recoilVal / 100;
            if (ModDetection.RealismLoaded)
            {
                result = recoilVal / 150;
            }
            result *= Bot.Info.WeaponInfo.FinalModifier;
            result *= UnityEngine.Random.Range(0.8f, 1.2f);
            return result;
        }

        float CalcHorizRecoil(float recoilVal)
        {
            float result = recoilVal / 100;
            if (ModDetection.RealismLoaded)
            {
                result = recoilVal / 150;
            }
            result *= Bot.Info.WeaponInfo.FinalModifier;
            result *= UnityEngine.Random.Range(0.8f, 1.2f);
            return result;
        }

        private bool _shotRegistered;
        private bool _barrelRising;
        private Vector3 _recoilOffsetTarget;
        private bool _recoilFinished;
        private Coroutine _recoilCoroutine;
        private bool _armsInjured => Bot.BotHitReaction.ArmsInjured;
        private float RecoilMultiplier => Mathf.Round(Bot.Info.FileSettings.Shoot.RecoilMultiplier * GlobalSettings.Shoot.RecoilMultiplier * 100f) / 100f;

        private float RecoilBaseline
        {
            get
            {
                if (ModDetection.RealismLoaded)
                {
                    return 225f;
                }
                else
                {
                    return 112f;
                }
            }
        }
    }
}
