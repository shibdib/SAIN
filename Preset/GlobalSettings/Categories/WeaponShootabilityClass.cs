﻿using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;
using System.ComponentModel;

namespace SAIN.Preset.GlobalSettings
{
    public class WeaponShootabilityClass
    {
        public WeaponShootabilityClass()
        {
        }

        public void UpdateValues()
        {
            Values = GetValuesFromClass.UpdateValues(IWeaponClass.Default, Default, this, Values);
        }

        public float Get(string weaponClass)
        {
            float modifier;
            if (System.Enum.TryParse(weaponClass, out IWeaponClass result))
            {
                modifier = Get(result);
            }
            else
            {
                Logger.LogError($"{weaponClass} could not parse");
                modifier = Default;
            }
            return modifier;
        }

        public float Get(IWeaponClass key)
        {
            if (Values.Count == 0)
            {
                UpdateValues();
            }
            if (Values.ContainsKey(key))
            {
                return (float)Values[key];
            }
            Logger.LogWarning($"{key} does not exist in {GetType()} Dictionary");
            return Default;
        }

        [JsonIgnore]
        public Dictionary<object, object> Values = new Dictionary<object, object>();
        private const string Description = "Lower is BETTER. How Shootable this weapon type is, affects semi auto firerate and full auto burst length";

        [DefaultValue(0.25f)]
        [WeaponClass(IWeaponClass.assaultRifle)]
        [NameAndDescription(nameof(AssaultRifle), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float AssaultRifle = 0.25f;

        [DefaultValue(0.3f)]
        [WeaponClass(IWeaponClass.assaultCarbine)]
        [NameAndDescription(nameof(AssaultCarbine), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float AssaultCarbine = 0.3f;

        [DefaultValue(0.25f)]
        [WeaponClass(IWeaponClass.machinegun)]
        [NameAndDescription(nameof(Machinegun), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float Machinegun = 0.25f;

        [DefaultValue(0.2f)]
        [WeaponClass(IWeaponClass.smg)]
        [NameAndDescription(nameof(SMG), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float SMG = 0.2f;

        [DefaultValue(0.4f)]
        [WeaponClass(IWeaponClass.pistol)]
        [NameAndDescription(nameof(Pistol), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float Pistol = 0.4f;

        [DefaultValue(0.5f)]
        [WeaponClass(IWeaponClass.marksmanRifle)]
        [NameAndDescription(nameof(MarksmanRifle), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float MarksmanRifle = 0.5f;

        [DefaultValue(0.75f)]
        [WeaponClass(IWeaponClass.sniperRifle)]
        [NameAndDescription(nameof(SniperRifle), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float SniperRifle = 0.75f;

        [DefaultValue(0.5f)]
        [WeaponClass(IWeaponClass.shotgun)]
        [NameAndDescription(nameof(Shotgun), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float Shotgun = 0.5f;

        [DefaultValue(1f)]
        [WeaponClass(IWeaponClass.grenadeLauncher)]
        [NameAndDescription(nameof(GrenadeLauncher), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float GrenadeLauncher = 1f;

        [DefaultValue(1f)]
        [WeaponClass(IWeaponClass.specialWeapon)]
        [NameAndDescription(nameof(SpecialWeapon), Description)]
        [MinMax(0.01f, 1f, 100f)]
        public float SpecialWeapon = 1;

        public static readonly float Default = 0.5f;
    }
}