using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public class WeaponInfo
    {
        public Weapon Weapon { get; private set; }

        public void Update(Weapon weapon)
        {
            if (Weapon == null || Weapon != weapon)
            {
                Weapon = weapon;
            }

            HasSuppressor = false;
            HasRedDot = false;
            HasOptic = false;

            WeaponClass = EnumValues.ParseWeaponClass(weapon.Template.weapClass);

            // Another thing to fix later
            var mods = weapon.Mods.ToArray();
            // OLD:
            // var mods = weapon.Mods;

            for (int i = 0; i < mods.Length; i++)
            {
                CheckMod(mods[i]);
                if (mods[i].Slots.Length > 0)
                {
                    for (int j = 0; j < mods[i].Slots.Length; j++)
                    {
                        Item containedItem = mods[i].Slots[j].ContainedItem;
                        if (containedItem != null && containedItem is Mod mod)
                        {
                            Type modType = mod.GetType();
                            if (IsSilencer(modType))
                            {
                                HasSuppressor = true;
                            }
                            else if (IsOptic(modType))
                            {
                                HasOptic = true;
                            }
                            else if (IsRedDot(modType))
                            {
                                HasRedDot = true;
                            }
                        }
                    }
                }
            }
        }

        private void CheckMod(Mod mod)
        {
            if (mod != null)
            {
                Type modType = mod.GetType();
                if (IsSilencer(modType))
                {
                    HasSuppressor = true;
                }
                else if (IsOptic(modType))
                {
                    HasOptic = true;
                }
                else if (IsRedDot(modType))
                {
                    HasRedDot = true;
                }
            }
        }

        public void Log()
        {
            if (SAINPlugin.DebugMode)
            {
                Logger.LogWarning(
                    $"Found Weapon Info: " +
                    $"Weapon Class: [{WeaponClass}] " +
                    $"Has Red Dot? [{HasRedDot}] " +
                    $"Has Optic? [{HasOptic}] " +
                    $"Has Suppressor? [{HasSuppressor}]");
            }
        }

        public IWeaponClass WeaponClass;
        public bool HasRedDot;
        public bool HasOptic;
        public bool HasSuppressor;

        private static bool IsSilencer(Type modType)
        {
            return modType == TemplateIdToObjectMappingsClass.TypeTable[SuppressorTypeId];
        }

        private static bool IsOptic(Type modType)
        {
            return CheckTemplates(modType, AssaultScopeTypeId, OpticScopeTypeId, SpecialScopeTypeId);
        }

        private static bool IsRedDot(Type modType)
        {
            return CheckTemplates(modType, CollimatorTypeId, CompactCollimatorTypeId);
        }

        private static bool CheckTemplates(Type modType, params string[] templateIDs)
        {
            for (int i = 0; i < templateIDs.Length; i++)
            {
                if (CheckTemplateType(modType, templateIDs[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckTemplateType(Type modType, string id)
        {
            if (TemplateIdToObjectMappingsClass.TypeTable.TryGetValue(id, out Type result))
            {
                if (result == modType)
                {
                    return true;
                }
            }
            if (TemplateIdToObjectMappingsClass.TemplateTypeTable.TryGetValue(id, out result))
            {
                if (result == modType)
                {
                    return true;
                }
            }
            return false;
        }

        private static readonly string SuppressorTypeId = "550aa4cd4bdc2dd8348b456c";
        private static readonly string CollimatorTypeId = "55818ad54bdc2ddc698b4569";
        private static readonly string CompactCollimatorTypeId = "55818acf4bdc2dde698b456b";
        private static readonly string AssaultScopeTypeId = "55818add4bdc2d5b648b456f";
        private static readonly string OpticScopeTypeId = "55818ae44bdc2dde698b456c";
        private static readonly string SpecialScopeTypeId = "55818aeb4bdc2ddc698b456a";
    }

    public enum EScopeType
    {
        RedDot,
        Optic,
        Assault,
        Special
    }

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