using EFT;
using EFT.InventoryLogic;
using SAIN.Components.BotController;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Info;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.Classes.Equipment
{
    public class SAINEquipmentClass : PlayerComponentBase
    {
        public SAINEquipmentClass(PlayerComponent playerComponent) : base(playerComponent)
        {
            EquipmentClass = playerComponent.Player.Equipment;
            GearInfo = new GearInfo(this);
        }

        public void Init()
        {
            getAllWeapons();
            updateAllWeapons();
            ReCalcPowerOfEquipment();
        }

        public void Dispose()
        {
            foreach (var weapon in WeaponInfos.Values) {
                weapon?.Dispose();
            }
            WeaponInfos.Clear();
        }

        public InventoryEquipment EquipmentClass { get; private set; }

        private void ReCalcPowerOfEquipment()
        {
            float oldPower = Player.AIData.PowerOfEquipment;
            if (SAINPlugin.LoadedPreset.GlobalSettings.PowerCalc.CalcPower(PlayerComponent, out float power) &&
                oldPower != power) {
                OnPowerRecalced?.Invoke(power);
            }
        }

        public Action<float> OnPowerRecalced { get; set; }

        public bool PlayAIShootSound()
        {
            var weapon = CurrentWeapon;
            if (weapon == null) {
                //Logger.LogWarning("CurrentWeapon Null");
                return false;
            }

            if (_nextPlaySoundTime < Time.time) {
                _nextPlaySoundTime = Time.time + (PlayerComponent.IsAI ? 0.5f : 0.1f);
                SAINBotController.Instance.BotHearing.PlayAISound(PlayerComponent, weapon.SoundType, PlayerComponent.Transform.WeaponFirePort, weapon.CalculatedAudibleRange, 1f, false);
            }
            return true;
        }

        private float _nextPlaySoundTime;

        public void Update()
        {
            CurrentWeapon = getCurrentWeapon();
            GearInfo.Update();
            updateAllWeapons();
        }

        private void getAllWeapons()
        {
            foreach (EquipmentSlot slot in _weaponSlots) {
                addWeaponFromSlot(slot);
            }
        }

        private void addWeaponFromSlot(EquipmentSlot slot)
        {
            Item item = EquipmentClass.GetSlot(slot).ContainedItem;
            if (item != null && item is Weapon weapon) {
                if (!WeaponInfos.ContainsKey(slot)) {
                    WeaponInfos.Add(slot, new WeaponInfo(weapon));
                }
                else if (WeaponInfos.TryGetValue(slot, out WeaponInfo info) &&
                    info.Weapon != weapon) {
                    info.Dispose();
                    WeaponInfos[slot] = new WeaponInfo(weapon);
                }
            }
        }

        private void updateAllWeapons()
        {
            if (_nextUpdateWeapTime < Time.time) {
                _nextUpdateWeapTime = Time.time + 1f;

                foreach (var info in WeaponInfos.Values) {
                    if (info?.Update() == true)
                        return;
                }
            }
        }

        private float _nextUpdateWeapTime;

        private static readonly EquipmentSlot[] _weaponSlots = new EquipmentSlot[]
        {
            EquipmentSlot.FirstPrimaryWeapon,
            EquipmentSlot.SecondPrimaryWeapon,
            EquipmentSlot.Holster,
        };

        public GearInfo GearInfo { get; private set; }

        public WeaponInfo CurrentWeapon { get; private set; }

        public WeaponInfo WeaponInInventory => PrimaryWeapon ?? SecondaryWeapon ?? HolsterWeapon;

        private WeaponInfo getCurrentWeapon()
        {
            Item item = Player.HandsController.Item;
            if (item != null) {
                _currentWeapon = getInfoFromItem(item);
            }
            return _currentWeapon;
        }

        private WeaponInfo getInfoFromItem(Item item)
        {
            if (item is Weapon weapon) {
                if (_currentWeapon?.Weapon == weapon) {
                    return _currentWeapon;
                }
                var weaponInfo = getInfoFromWeapon(weapon);
                if (weaponInfo == null) {
                    getAllWeapons();
                    weaponInfo = getInfoFromWeapon(weapon);
                }
                return weaponInfo;
            }
            return null;
        }

        private WeaponInfo getInfoFromWeapon(Weapon weapon)
        {
            foreach (var weaponInfo in WeaponInfos.Values) {
                if (weapon == weaponInfo.Weapon) {
                    _currentWeapon = weaponInfo;
                    ReCalcPowerOfEquipment();
                    return weaponInfo;
                }
            }
            return null;
        }

        public WeaponInfo GetWeaponInfo(EquipmentSlot slot)
        {
            if (WeaponInfos.TryGetValue(slot, out WeaponInfo weaponInfo))
                return weaponInfo;
            return null;
        }

        public WeaponInfo PrimaryWeapon => GetWeaponInfo(EquipmentSlot.FirstPrimaryWeapon);
        public WeaponInfo SecondaryWeapon => GetWeaponInfo(EquipmentSlot.SecondPrimaryWeapon);
        public WeaponInfo HolsterWeapon => GetWeaponInfo(EquipmentSlot.Holster);

        private WeaponInfo _currentWeapon;

        public Dictionary<EquipmentSlot, WeaponInfo> WeaponInfos { get; } = new Dictionary<EquipmentSlot, WeaponInfo>();
    }
}