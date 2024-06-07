using EFT;
using EFT.InventoryLogic;
using SAIN.Components.BotController;
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

            getAllWeapons();
            updateAllWeapons();
        }

        public EquipmentClass EquipmentClass { get; private set; }

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
            updateWeapons();
        }

        private void updateWeapons()
        {
            if (_nextGetWeaponsTime < Time.time)
            {
                getAllWeapons();
            }
            if (_nextUpdateTime < Time.time)
            {
                updateAllWeapons();
            }
        }

        private float _nextGetWeaponsTime;
        private float _nextUpdateTime;

        private void getAllWeapons()
        {
            var equipment = EquipmentClass;
            if (equipment == null)
            {
                return;
            }

            _nextGetWeaponsTime = Time.time + 10f;

            foreach (EquipmentSlot slot in _weaponSlots)
            {
                Item item = equipment.GetSlot(slot).ContainedItem;
                if (item != null && item is Weapon weapon)
                {
                    Logger.LogDebug("Found Weapon");

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
                else
                {
                    Logger.LogDebug($"No Weapon In Slot {slot}");
                }
            }
        }

        private void updateAllWeapons()
        {
            _nextUpdateTime = Time.time + 1f;
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

        public GearInfo GearInfo { get; private set; }

        public WeaponInfo CurrentWeapon
        {
            get
            {
                if (Player.HandsController.Item is Weapon weapon)
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
                            Player.AIData?.CalcPower();
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