using EFT;
using EFT.InventoryLogic;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Info
{
    public class GearInfo
    {
        public GearInfo(InventoryControllerClass inventoryController)
        {
            InventoryController = inventoryController;
        }

        public void Update()
        {
            if (_nextUpdateTime < Time.time)
            {
                _nextUpdateTime = Time.time + 5f;
                checkAllGear();
            }
        }

        private void checkAllGear()
        {
            HasEarPiece = GetItem(EquipmentSlot.Earpiece) != null;

            // Reset previous results if any
            HasFaceShield = false;

            // Get the headwear item on this player
            Item helmetItem = GetItem(EquipmentSlot.Headwear);

            if (helmetItem != null)
            {
                // Get a list of faceshield components attached to the headwear item, see if any have AC.
                helmetItem.GetItemComponentsInChildrenNonAlloc(_faceShieldComponents);
                foreach (var faceComponent in _faceShieldComponents)
                {
                    if (faceComponent.Item.IsArmorMod())
                    {
                        HasFaceShield = true;
                        break;
                    }
                }
                _faceShieldComponents.Clear();
            }

            // Reset previous results if any
            HasHeavyHelmet = false;

            // Get a list of armor components attached to the headwear item, check to see which has the highest AC, and check if any make the user deaf.
            HelmetArmorClass = FindMaxAC(helmetItem);

            foreach (ArmorComponent armor in _armorList)
            {
                if (armor.Deaf == EDeafStrength.High)
                {
                    HasHeavyHelmet = true;
                    break;
                }
            }
            _armorList.Clear();

            int vestAC = FindMaxAC(EquipmentSlot.ArmorVest);
            _armorList.Clear();

            int bodyAC = FindMaxAC(EquipmentSlot.TacticalVest);
            _armorList.Clear();

            BodyArmorClass = Mathf.Max(vestAC, bodyAC);

            if (SAINPlugin.DebugMode)
            {
                Logger.LogInfo(
                    $" Found GearInfo: " +
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

        private readonly List<ArmorComponent> _helmetComponents = new List<ArmorComponent>();

        public bool HasFaceShield { get; private set; }

        private readonly List<FaceShieldComponent> _faceShieldComponents = new List<FaceShieldComponent>();

        public bool HasArmor => BodyArmorClass != 0;

        public int BodyArmorClass { get; private set; }

        public Item GetItem(EquipmentSlot slot)
        {
            return InventoryController.Inventory.Equipment.GetSlot(slot).ContainedItem;
        }

        private int FindMaxAC(Item item)
        {
            if (item == null) return 0;

            item.GetItemComponentsInChildrenNonAlloc(_armorList, true);

            int result = 0;
            for (int i = 0; i < _armorList.Count; i++)
            {
                ArmorComponent armor = _armorList[i];
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
            return FindMaxAC(item);
        }

        private readonly List<ArmorComponent> _armorList = new List<ArmorComponent>();

        private readonly InventoryControllerClass InventoryController;

        private float _nextUpdateTime;
    }
}