using EFT;
using EFT.InventoryLogic;
using SAIN.Components.BotController;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Info;
using System;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;

namespace SAIN.Components.PlayerComponentSpace.Classes.Equipment
{
    public class SAINEquipmentClass : PlayerComponentBase
    {
        public SAINEquipmentClass(PlayerComponent playerComponent) : base(playerComponent)
        {
            EquipmentClass = playerComponent.Player.Equipment;
            GearInfo = new GearInfo(this);

            getAllWeapons();
            updateAllWeapons();
            ReCalcPowerOfEquipment();
        }

        public void InitBot(BotOwner botOwner)
        {
            botOwner.WeaponManager.Selector.OnActiveEquipmentSlotChanged += slotSelected;
        }


        public void DisposeBot()
        {
            BotOwner botOwner = PlayerComponent.BotOwner;
            if (botOwner != null)
            {
                botOwner.WeaponManager.Selector.OnActiveEquipmentSlotChanged -= slotSelected;
            }
        }

        private void slotSelected(EquipmentSlot slot)
        {
            addWeaponFromSlot(slot);
            if (WeaponInfos.TryGetValue(slot, out var weaponInfo))
            {
                CurrentWeapon = weaponInfo;
                ReCalcPowerOfEquipment();
            }
        }

        public EquipmentClass EquipmentClass { get; private set; }

        private void ReCalcPowerOfEquipment()
        {
            if (SAINPlugin.LoadedPreset.GlobalSettings.PowerCalc.CalcPower(Player, out float power))
            {
                float oldPower = Player.AIData.PowerOfEquipment;
                if (oldPower != power)
                {
                    Player.AIData.PowerOfEquipment = power;
                    OnPowerRecalced?.Invoke(power);
                }
            }
        }

        public Action<float> OnPowerRecalced { get; set; }

        public bool PlayAIShootSound()
        {
            var weapon = CurrentWeapon;
            if (weapon == null)
            {
                Logger.LogWarning("CurrentWeapon Null");
                return false;
            }

            if (_nextPlaySoundTime < Time.time)
            {
                _nextPlaySoundTime = Time.time + (PlayerComponent.IsAI ? 0.5f : 0.1f);
                SAINSoundType sainType = weapon.AISoundType == AISoundType.gun ? SAINSoundType.Gunshot : SAINSoundType.SuppressedGunShot;

                float range = weapon.CalculatedAudibleRange;

                var weather = SAINWeatherClass.Instance;
                if (weather != null)
                {
                    range *= weather.RainSoundModifier;
                }

                SAINPlugin.BotController?.PlayAISound(Player, sainType, Player.WeaponRoot.position, range);
            }
            return true;
        }

        private float _nextPlaySoundTime;

        public void Update()
        {
            GearInfo.Update();
            updateAllWeapons();
        }

        private float _nextUpdateTime;

        private void getAllWeapons()
        {
            foreach (EquipmentSlot slot in _weaponSlots)
            {
                addWeaponFromSlot(slot);
            }
        }

        private void addWeaponFromSlot(EquipmentSlot slot)
        {
            Item item = EquipmentClass.GetSlot(slot).ContainedItem;
            if (item != null && item is Weapon weapon)
            {
                if (!WeaponInfos.ContainsKey(slot))
                {
                    WeaponInfos.Add(slot, new WeaponInfo(weapon));
                }
                else if (WeaponInfos.TryGetValue(slot, out WeaponInfo info) &&
                    info.Weapon != weapon)
                {
                    WeaponInfos[slot] = new WeaponInfo(weapon);
                }
            }
        }

        private void updateAllWeapons()
        {
            if (_nextUpdateTime < Time.time)
            {
                _nextUpdateTime = Time.time + 1f;

                foreach (var info in WeaponInfos.Values)
                {
                    info?.Update();
                }
            }
        }

        private static readonly EquipmentSlot[] _weaponSlots = new EquipmentSlot[]
        {
            EquipmentSlot.FirstPrimaryWeapon,
            EquipmentSlot.SecondPrimaryWeapon,
            EquipmentSlot.Holster,
        };

        public GearInfo GearInfo { get; private set; }

        public WeaponInfo CurrentWeapon
        {
            get
            {
                if (PlayerComponent.IsAI)
                {
                    return _currentWeapon;
                }

                if (Player.HandsController.Item is Weapon weapon)
                {
                    if (_currentWeapon?.Weapon == weapon)
                    {
                        return _currentWeapon;
                    }

                    foreach (var weaponInfo in WeaponInfos.Values)
                    {
                        if (weapon == weaponInfo.Weapon)
                        {
                            _currentWeapon = weaponInfo;
                            ReCalcPowerOfEquipment();
                            break;
                        }
                    }
                }
                return _currentWeapon;
            }
            private set
            {
                _currentWeapon = value;
            }
        }

        private WeaponInfo _currentWeapon;

        public Dictionary<EquipmentSlot, WeaponInfo> WeaponInfos { get; private set; } = new Dictionary<EquipmentSlot, WeaponInfo>();
    }
}