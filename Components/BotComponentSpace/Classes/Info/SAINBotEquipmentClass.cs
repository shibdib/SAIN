using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Helpers;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Info
{
    public class SAINBotEquipmentClass : SAINBase, ISAINClass
    {
        static SAINBotEquipmentClass()
        {
            InventoryControllerProp = AccessTools.Field(typeof(Player), "_inventoryController");
        }

        public static readonly FieldInfo InventoryControllerProp;

        public SAINBotEquipmentClass(BotComponent sain) : base(sain)
        {
            InventoryController = Reflection.GetValue<InventoryControllerClass>(Player, InventoryControllerProp);
            GearInfo = new GearInfo(Player, InventoryController);
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (_updateEquipTime < Time.time)
            {
                _updateEquipTime = Time.time + 60f;
                GearInfo.Update();
                UpdateWeapons();
            }
            if (_checkCurrentWeaponTime < Time.time)
            {
                _checkCurrentWeaponTime = Time.time + 0.25f; 
                checkCurrentWeapon();
            }
        }

        private float _checkCurrentWeaponTime;

        public void Dispose()
        {
        }

        public InventoryControllerClass InventoryController { get; private set; }

        // delete later
        public bool HasEarPiece => GearInfo.HasEarPiece;

        public bool HasHeavyHelmet => GearInfo.HasHeavyHelmet;

        private float _updateEquipTime = 0f;

        private void checkCurrentWeapon()
        {
            Weapon currentWeapon = BotOwner.WeaponManager.CurrentWeapon;
            if (currentWeapon == null)
            {
                CurrentWeaponInfo = null;
                return;
            }
            if (currentWeapon == PrimaryWeaponInfo?.Weapon)
            {
                if (CurrentWeaponInfo != PrimaryWeaponInfo)
                {
                    CurrentWeaponInfo = PrimaryWeaponInfo;
                }
                return;
            }
            if (currentWeapon == SecondaryWeaponInfo?.Weapon)
            {
                if (CurrentWeaponInfo != SecondaryWeaponInfo)
                {
                    CurrentWeaponInfo = SecondaryWeaponInfo;
                }
                return;
            }
            if (currentWeapon == HolsterWeaponInfo?.Weapon)
            {
                if (CurrentWeaponInfo != HolsterWeaponInfo)
                {
                    CurrentWeaponInfo = HolsterWeaponInfo;
                }
                return;
            }
        }

        public void UpdateWeapons()
        {
            Weapon currentWeapon = BotOwner.WeaponManager.CurrentWeapon;

            Item primaryItem = GearInfo.GetItem(EquipmentSlot.FirstPrimaryWeapon);
            if (primaryItem != null && primaryItem is Weapon primaryWeapon)
            {
                // if (SAINPlugin.DebugMode) Logger.LogWarning("Found FirstPrimary Weapon");
                PrimaryWeaponInfo.Update(primaryWeapon);
                PrimaryWeaponInfo.Log();
            }
            Item secondaryItem = GearInfo.GetItem(EquipmentSlot.SecondPrimaryWeapon);
            if (secondaryItem != null && secondaryItem is Weapon secondaryWeapon)
            {
                SecondaryWeaponInfo.Update(secondaryWeapon);
                SecondaryWeaponInfo.Log();
            }
            Item holsterItem = GearInfo.GetItem(EquipmentSlot.Holster);
            if (holsterItem != null && holsterItem is Weapon holsterWeapon)
            {
                HolsterWeaponInfo.Update(holsterWeapon);
                HolsterWeaponInfo.Log();
            }
        }

        public GearInfo GearInfo { get; private set; }
        public WeaponInfo CurrentWeaponInfo { get; private set; }
        public WeaponInfo PrimaryWeaponInfo { get; private set; } = new WeaponInfo();
        public WeaponInfo SecondaryWeaponInfo { get; private set; } = new WeaponInfo();
        public WeaponInfo HolsterWeaponInfo { get; private set; } = new WeaponInfo();
    }
}