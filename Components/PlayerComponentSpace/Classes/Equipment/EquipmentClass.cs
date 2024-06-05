using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Info;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.InventoryLogic;

namespace SAIN.Components.PlayerComponentSpace.Classes.Equipment
{
    public class EquipmentClass : PlayerComponentBase
    {
        static EquipmentClass()
        {
            InventoryControllerProp = AccessTools.Field(typeof(Player), "_inventoryController");
        }
        private static readonly FieldInfo InventoryControllerProp;

        public EquipmentClass(PlayerComponent playerComponent) : base(playerComponent)
        {
            InventoryController = (InventoryControllerClass)InventoryControllerProp.GetValue(Player);
            GearInfo = new GearInfo(Player, InventoryController);
        }

        public InventoryControllerClass InventoryController { get; private set; }

        public GearInfo GearInfo { get; private set; }

        public WeaponInfo CurrentWeapon
        {
            get
            {
                var item = Player.HandsController.Item;
                if (item != null && item is Weapon weapon)
                {
                    foreach (var weaponInfo in WeaponInfos)
                    {
                        if (weapon == weaponInfo.Value.Weapon)
                        {
                            return weaponInfo.Value;
                        }
                    }
                }
                return null;
            }
        }

        public Dictionary<EquipmentSlot, WeaponInfo> WeaponInfos { get; private set; } = new Dictionary<EquipmentSlot, WeaponInfo>();
    }
}
