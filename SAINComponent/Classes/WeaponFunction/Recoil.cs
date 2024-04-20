using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using UnityEngine;
using static EFT.InventoryLogic.Weapon;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class Recoil : SAINBase, ISAINClass
    {
        public Recoil(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            RecoilOffset = CalculateDecay(RecoilOffset);
        }

        public void Dispose()
        {
        }

        public void WeaponShot()
        {
            RecoilOffset = CalculateRecoil(RecoilOffset);
        }

        public Vector3 RecoilOffset { get; private set; } = Vector3.zero;

        public Vector3 CalculateRecoil(Vector3 currentRecoil)
        {
            float distance = SAIN.DistanceToAimTarget;

            // Reduces scatter recoil at very close range. Clamps Distance between 3 and 20 then scale to 0.25 to 1.
            // So if a target is 3m or less Distance, their recoil scaling will be 25% its original value
            distance = Mathf.Clamp(distance, 3f, 20f);
            distance /= 20f;
            distance = distance * 0.75f + 0.25f;

            float weaponhorizrecoil = CalcHorizRecoil(SAIN.Info.WeaponInfo.RecoilForceUp);
            float weaponvertrecoil = CalcVertRecoil(SAIN.Info.WeaponInfo.RecoilForceBack);

            float addRecoil = SAINPlugin.LoadedPreset.GlobalSettings.Shoot.AddRecoil;
            float horizRecoil = (1f * (weaponhorizrecoil + addRecoil));
            float vertRecoil = (1f * (weaponvertrecoil + addRecoil));

            float maxrecoil = SAINPlugin.LoadedPreset.GlobalSettings.Shoot.MaxRecoil;

            float randomHorizRecoil = Random.Range(-horizRecoil, horizRecoil);
            float randomvertRecoil = Random.Range(-vertRecoil, vertRecoil);
            Vector3 newRecoil = new Vector3(randomHorizRecoil, randomvertRecoil, randomHorizRecoil);
            newRecoil = MathHelpers.VectorClamp(newRecoil, -maxrecoil, maxrecoil) * RecoilMultiplier;

            Vector3 vector = newRecoil + currentRecoil;
            return vector;
        }

        private float RecoilMultiplier => Mathf.Round(SAIN.Info.FileSettings.Shoot.RecoilMultiplier * GlobalSettings.Shoot.GlobalRecoilMultiplier * 100f) / 100f;

        float CalcVertRecoil(float recoilVal)
        {
            float result = recoilVal / 100;
            result *= SAIN.Info.WeaponInfo.FinalModifier;
            result *= UnityEngine.Random.Range(0.66f, 1.33f);
            return result;
        }

        float CalcHorizRecoil(float recoilVal)
        {
            float result = recoilVal / 200;
            result *= SAIN.Info.WeaponInfo.FinalModifier;
            result *= UnityEngine.Random.Range(0.66f, 1.33f);
            return result;
        }

        public Vector3 CalculateDecay(Vector3 oldVector)
        {
            if (oldVector == Vector3.zero) return oldVector;

            Vector3 decayed = Vector3.Lerp(Vector3.zero, oldVector, SAINPlugin.LoadedPreset.GlobalSettings.Shoot.RecoilDecay);
            if ((decayed - Vector3.zero).sqrMagnitude < 0.01f)
            {
                decayed = Vector3.zero;
            }
            return decayed;
        }

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
