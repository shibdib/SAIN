using EFT;
using EFT.InventoryLogic;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Info
{
    public class GearInfo
    {
        public GearInfo(Player player, InventoryControllerClass inventoryController)
        {
            Player = player;
            InventoryController = inventoryController;
        }

        public readonly Player Player;

        public readonly InventoryControllerClass InventoryController;

        public void Update()
        {
            HasEarPiece = GetItem(EquipmentSlot.Earpiece) != null;

            // Reset previous results if any
            HasFaceShield = false;

            // Get the headwear item on this player
            Item helmetItem = GetItem(EquipmentSlot.Headwear);

            if (helmetItem != null)
            {
                // Get a list of faceshield components attached to the headwear item, see if any have AC.
                helmetItem.GetItemComponentsInChildrenNonAlloc(FaceShieldComponents);
                foreach (var faceComponent in FaceShieldComponents)
                {
                    if (faceComponent.Item.IsArmorMod())
                    {
                        HasFaceShield = true;
                        break;
                    }
                }
                FaceShieldComponents.Clear();
            }

            // Reset previous results if any
            HasHeavyHelmet = false;

            // Get a list of armor components attached to the headwear item, check to see which has the highest AC, and check if any make the user deaf.
            HelmetArmorClass = FindMaxAC(helmetItem, HelmetArmorComponents);

            if (HelmetArmorComponents.Count > 0)
            {
                foreach (ArmorComponent armor in HelmetArmorComponents)
                {
                    if (armor.Deaf == EDeafStrength.High)
                    {
                        HasHeavyHelmet = true;
                        break;
                    }
                }
                HelmetArmorComponents.Clear();
            }

            int vestAC = FindMaxAC(EquipmentSlot.ArmorVest);
            int bodyAC = FindMaxAC(EquipmentSlot.TacticalVest);
            BodyArmorClass = Mathf.Max(vestAC, bodyAC);

            if (SAINPlugin.DebugMode)
            {
                Logger.LogInfo(
                    $" Found GearInfo for [{Player.Profile.Nickname}]:" +
                    $" Body Armor Class: [{BodyArmorClass}]" +
                    $" Helmet Armor Class [{HelmetArmorClass}]" +
                    $" Has Heavy Helmet? [{HasHeavyHelmet}]" +
                    $" Has EarPiece? [{HasEarPiece}]" +
                    $" Has Face Shield? [{HasFaceShield}]");
            }
        }

        public bool HasEarPiece { get; private set; }

        public bool HasHelmet => HelmetArmorClass > 0;

        public bool HasHeavyHelmet { get; private set; }

        public int HelmetArmorClass { get; private set; }

        private readonly List<ArmorComponent> HelmetArmorComponents = new List<ArmorComponent>();

        public bool HasFaceShield { get; private set; }

        private readonly List<FaceShieldComponent> FaceShieldComponents = new List<FaceShieldComponent>();

        public bool HasArmor => BodyArmorClass != 0;

        public int BodyArmorClass { get; private set; }

        public Item GetItem(EquipmentSlot slot)
        {
            return InventoryController.Inventory.Equipment.GetSlot(slot).ContainedItem;
        }

        private static int FindMaxAC(Item item, List<ArmorComponent> armorComponents)
        {
            if (item == null) return 0;

            armorComponents.Clear();
            item.GetItemComponentsInChildrenNonAlloc(armorComponents, true);
            return FindMaxAC(armorComponents);
        }

        private static int FindMaxAC(List<ArmorComponent> armorComponents)
        {
            int result = 0;
            for (int i = 0; i < armorComponents.Count; i++)
            {
                ArmorComponent armor = armorComponents[i];
                if (armor.ArmorClass > result)
                {
                    result = armor.ArmorClass;
                }
            }
            return result;
        }

        private int FindMaxAC(EquipmentSlot slot)
        {
            Item item = InventoryController.Inventory.Equipment.GetSlot(slot).ContainedItem;
            return FindMaxAC(item, StaticArmorList);
        }

        private static readonly List<ArmorComponent> StaticArmorList = new List<ArmorComponent>();
    }
}