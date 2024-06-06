using EFT;
using EFT.InventoryLogic;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Info;
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
        }

        public EquipmentClass EquipmentClass { get; private set; }

        public void PlayShootSound(float range, AISoundType soundType)
        {
            if (Player != null &&
                Player.WeaponRoot != null)
            {
                var weapon = CurrentWeapon;
                if (weapon != null)
                {
                    SAINSoundType sainType = weapon.AISoundType == AISoundType.gun ? SAINSoundType.Gunshot : SAINSoundType.SuppressedGunShot;
                    SAINPlugin.BotController?.PlayAISound(Player, sainType, Player.WeaponRoot.position, weapon.CalculatedAudibleRange);
                }
            }
        }

        public void Update()
        {
            GearInfo.Update();
            updateWeapons();
        }

        private void updateWeapons()
        {
            if (_nextGetWeaponsTime < Time.time)
            {
                _nextGetWeaponsTime = Time.time + 10f;
                getAllWeapons();
            }
            if (_nextUpdateTime < Time.time)
            {
                _nextUpdateTime = Time.time + 1f;
                updateAllWeapons();
            }
        }

        private float _nextGetWeaponsTime;
        private float _nextUpdateTime;

        private void getAllWeapons()
        {
            var equipment = InventoryController?.Inventory?.Equipment;
            if (equipment == null)
            {
                return;
            }

            foreach (EquipmentSlot slot in _weaponSlots)
            {
                Item item = equipment.GetSlot(slot).ContainedItem;
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
        }

        private void updateAllWeapons()
        {
            foreach (var info in WeaponInfos.Values)
            {
                info?.Update();
            }
        }

        private static readonly EquipmentSlot[] _weaponSlots = new EquipmentSlot[]
        {
            EquipmentSlot.FirstPrimaryWeapon,
            EquipmentSlot.SecondPrimaryWeapon,
            EquipmentSlot.Holster,
        };

        public InventoryControllerClass InventoryController { get; private set; }

        public GearInfo GearInfo { get; private set; }

        public WeaponInfo CurrentWeapon
        {
            get
            {
                var firearmController = Player.HandsController as FirearmController;
                if (firearmController != null &&
                    firearmController.Item is Weapon weapon)
                {
                    Weapon currWeapon = _currentWeapon?.Weapon;
                    if (currWeapon != null && currWeapon == weapon)
                    {
                        return _currentWeapon;
                    }

                    foreach (var weaponInfo in WeaponInfos.Values)
                    {
                        if (weapon == weaponInfo.Weapon)
                        {
                            _currentWeapon = weaponInfo;
                            break;
                        }
                    }
                }
                return _currentWeapon;
            }
        }

        private WeaponInfo _currentWeapon;

        public Dictionary<EquipmentSlot, WeaponInfo> WeaponInfos { get; private set; } = new Dictionary<EquipmentSlot, WeaponInfo>();
    }
}