using EFT.InventoryLogic;
using SAIN.Helpers;
using System;
using System.Linq;

namespace SAIN.SAINComponent.Classes.Info
{
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
}