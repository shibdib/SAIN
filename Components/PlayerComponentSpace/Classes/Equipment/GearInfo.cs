using EFT.InventoryLogic;
using SAIN.Components.PlayerComponentSpace.Classes.Equipment;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Info
{
    public class GearInfo
    {
        public bool HasEarPiece { get; private set; }

        public bool HasHelmet => HelmetArmorClass > 0;

        public bool HasHeavyHelmet { get; private set; }

        public int HelmetArmorClass { get; private set; }

        public bool HasFaceShield { get; private set; }

        public bool HasArmor => BodyArmorClass != 0;

        public int BodyArmorClass { get; private set; }

        protected SAINEquipmentClass Equipment { get; private set; }

        protected EquipmentClass _equipment => Equipment.EquipmentClass;

        public GearInfo(SAINEquipmentClass equipment)
        {
            Equipment = equipment;
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
            HelmetArmorClass = findMaxAC(helmetItem);

            foreach (ArmorComponent armor in _armorList)
            {
                if (armor.Deaf == EDeafStrength.High)
                {
                    HasHeavyHelmet = true;
                    break;
                }
            }
            _armorList.Clear();

            int vestAC = findMaxAC(EquipmentSlot.ArmorVest);
            _armorList.Clear();

            int bodyAC = findMaxAC(EquipmentSlot.TacticalVest);
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

        public Item GetItem(EquipmentSlot slot)
        {
            return _equipment.GetSlot(slot).ContainedItem;
        }

        private int findMaxAC(Item item)
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

        private int findMaxAC(EquipmentSlot slot)
        {
            Item item = _equipment.GetSlot(slot).ContainedItem;
            return findMaxAC(item);
        }

        private readonly List<FaceShieldComponent> _faceShieldComponents = new List<FaceShieldComponent>();
        private readonly List<ArmorComponent> _armorList = new List<ArmorComponent>();
        private float _nextUpdateTime;
    }
}