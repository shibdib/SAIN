using EFT;
using EFT.InventoryLogic;
using Mono.WebBrowser;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.WeaponFunction;
using System.Linq;
using UnityEngine;
using static EFT.InventoryLogic.Weapon;

namespace SAIN.SAINComponent.Classes.Info
{
    public class WeaponInfoClass : SAINBase, ISAINClass
    {
        public WeaponInfoClass(Bot sain) : base(sain)
        {
            Recoil = new Recoil(sain);
            Firerate = new Firerate(sain);
            Firemode = new Firemode(sain);
            PresetHandler.PresetsUpdated += forceRecheckWeapon;
        }

        private void forceRecheckWeapon()
        {
            forceNewCheck = true;
        }

        public void Init()
        {
            Recoil.Init();
            Firerate.Init();
            Firemode.Init();
        }

        public void Update()
        {
            checkCalcWeaponInfo();
            Recoil.Update();
            Firerate.Update();
            Firemode.Update();
        }

        private Weapon LastCheckedWeapon;

        private float _nextRecalcTime;
        private const float _recalcFreq = 10f;
        private float _nextCheckWeapTime;
        private const float _checkWeapFreq = 0.25f;
        private bool forceNewCheck = false;

        public void checkCalcWeaponInfo()
        {
            if (_nextCheckWeapTime < Time.time)
            {
                Weapon currentWeapon = CurrentWeapon;
                if (currentWeapon != null)
                {
                    _nextCheckWeapTime = Time.time + _checkWeapFreq;
                    bool doCalculation = forceNewCheck || _nextRecalcTime < Time.time || LastCheckedWeapon == null || LastCheckedWeapon != currentWeapon;
                    if (doCalculation)
                    {
                        if (forceNewCheck)
                            forceNewCheck = false;

                        _nextRecalcTime = Time.time + _recalcFreq;
                        LastCheckedWeapon = currentWeapon;
                        calculateCurrentWeapon(currentWeapon);
                    }
                }
            }
        }

        private void calculateCurrentWeapon(Weapon weapon)
        {
            IWeaponClass = EnumValues.ParseWeaponClass(weapon.Template.weapClass);
            ICaliber = EnumValues.ParseCaliber(weapon.CurrentAmmoTemplate.Caliber);
            CalculateShootModifier();
            SwapToSemiDist = GetWeaponSwapToSemiDist(ICaliber, IWeaponClass);
            SwapToAutoDist = GetWeaponSwapToFullAutoDist(ICaliber, IWeaponClass);
        }

        public IWeaponClass IWeaponClass { get; private set; }
        public ICaliber ICaliber { get; private set; }

        private static ShootSettings ShootSettings => SAINPlugin.LoadedPreset.GlobalSettings.Shoot;

        private static float GetAmmoShootability(ICaliber caliber)
        {
            if (ShootSettings.AmmoCaliberShootability.TryGetValue(caliber, out var ammo))
            {
                return ammo;
            }
            return 0.5f;
        }

        private static float GetWeaponShootability(IWeaponClass weaponClass)
        {
            if (ShootSettings.WeaponClassShootability.TryGetValue(weaponClass, out var weap))
            {
                return weap;
            }
            return 0.5f;
        }

        private static float GetWeaponSwapToSemiDist(ICaliber caliber, IWeaponClass weaponClass)
        {
            if (ShootSettings.AmmoCaliberFullAutoMaxDistances.TryGetValue(caliber, out var caliberDist))
            {
                if (weaponClass == IWeaponClass.machinegun)
                {
                    return caliberDist * 1.5f;
                }
                return caliberDist;
            }
            return 55f;
        }

        private static float GetWeaponSwapToFullAutoDist(ICaliber caliber, IWeaponClass weaponClass)
        {
            return GetWeaponSwapToSemiDist(caliber, weaponClass) * 0.85f;
        }

        private void CalculateShootModifier()
        {
            var weapInfo = SAINBot.Info.WeaponInfo;

            float AmmoCaliberModifier =
                GetAmmoShootability(ICaliber)
                .Scale0to1(ShootSettings.AmmoCaliberScaling)
                .Round100();

            float WeaponClassModifier =
                GetWeaponShootability(IWeaponClass)
                .Scale0to1(ShootSettings.WeaponClassScaling)
                .Round100();

            float ProficiencyModifier =
                SAINBot.Info.FileSettings.Mind.WeaponProficiency
                .Scale0to1(ShootSettings.WeaponProficiencyScaling)
                .Round100();

            var weapon = weapInfo.CurrentWeapon;
            float ErgoModifier =
                Mathf.Clamp(1f - weapon.ErgonomicsTotal / 100f, 0.01f, 1f)
                .Scale0to1(ShootSettings.ErgoScaling)
                .Round100();

            float RecoilModifier = ((weapon.RecoilTotal / weapon.RecoilBase) + (weapon.CurrentAmmoTemplate.ammoRec / 200f))
                .Scale0to1(ShootSettings.RecoilScaling)
                .Round100();

            float DifficultyModifier =
                SAINBot.Info.Profile.DifficultyModifier
                .Scale0to1(ShootSettings.DifficultyScaling)
                .Round100();

            FinalModifier = (WeaponClassModifier * RecoilModifier * ErgoModifier * AmmoCaliberModifier * ProficiencyModifier * DifficultyModifier)
                .Round100();
        }

        public void Dispose()
        {
            Recoil.Dispose();
            Firerate.Dispose();
            Firemode.Dispose();
            PresetHandler.PresetsUpdated -= forceRecheckWeapon;
        }

        public float SwapToSemiDist { get; private set; } = 50f;
        public float SwapToAutoDist { get; private set; } = 45f;
        public Recoil Recoil { get; private set; }
        public Firerate Firerate { get; private set; }
        public Firemode Firemode { get; private set; }
        public float FinalModifier { get; private set; }

        public float EffectiveWeaponDistance
        {
            get
            {
                if (ICaliber == ICaliber.Caliber9x39)
                {
                    return 125f;
                }
                if (GlobalSettings.Shoot.EngagementDistance.TryGetValue(IWeaponClass, out float engagementDist))
                {
                    return engagementDist;
                }
                return 125f;
            }
        }

        public float PreferedShootDistance
        {
            get
            {
                return EffectiveWeaponDistance * 0.66f;
            }
        }

        public bool IsFireModeSet(EFireMode mode)
        {
            return SelectedFireMode == mode;
        }

        public bool HasFullAuto()
        {
            return HasFireMode(EFireMode.fullauto);
        }

        public bool HasBurst()
        {
            return HasFireMode(EFireMode.burst);
        }

        public bool HasSemi()
        {
            return HasFireMode(EFireMode.single);
        }

        public bool HasDoubleAction()
        {
            return HasFireMode(EFireMode.doubleaction);
        }

        public bool HasFireMode(EFireMode fireMode)
        {
            var modes = CurrentWeapon?.WeapFireType;
            if (modes == null) return false;
            return modes.Contains(fireMode);
        }

        public EFireMode SelectedFireMode
        {
            get
            {
                if (CurrentWeapon != null)
                {
                    return CurrentWeapon.SelectedFireMode;
                }
                return EFireMode.fullauto;
            }
        }

        public float RecoilForceUp
        {
            get
            {
                var template = CurrentWeapon?.Template;
                if (template != null)
                {
                    return template.RecoilForceUp;
                }
                else
                {
                    return 150f;
                }
            }
        }

        public float RecoilForceBack
        {
            get
            {
                var template = CurrentWeapon?.Template;
                if (template != null)
                {
                    return template.RecoilForceBack;
                }
                else
                {
                    return 150f;
                }
            }
        }

        public WeaponInfoClass WeaponInfo => SAINBot.Info?.WeaponInfo;

        public string WeaponClass => CurrentWeapon.Template.weapClass;

        public string AmmoCaliber => CurrentWeapon.CurrentAmmoTemplate.Caliber;

        public Weapon CurrentWeapon
        {
            get
            {
                BotWeaponManager weaponManager = BotOwner?.WeaponManager;
                if (weaponManager.Selector?.IsWeaponReady == true)
                {
                    return weaponManager.CurrentWeapon;
                }
                return null;
            }
        }
    }
}