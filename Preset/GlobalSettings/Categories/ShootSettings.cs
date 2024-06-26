using Newtonsoft.Json;
using SAIN;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class ShootSettings : SAINSettingsBase<ShootSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        [Name("Global Scatter Multiplier")]
        [Description("Higher = more scattering. Modifies EFT's default scatter feature. 1.5 = 1.5x more scatter")]
        [MinMax(0.01f, 10f, 100f)]
        public float GlobalScatterMultiplier = 1f;

        [Name("Global Recoil Multiplier")]
        [Description("Higher = more recoil. Modifies SAIN's recoil scatter feature. 1.5 = 1.5x more recoilfrom a single gunshot")]
        [MinMax(0.01f, 3f, 100f)]
        public float RecoilMultiplier = 1f;

        [Name("Add or Subtract Recoil")]
        [Description("Linearly add or subtract from the final recoil result")]
        [MinMax(-20f, 20f, 100f)]
        [Advanced]
        public float AddRecoil = 5f;

        [Name("Recoil Decay Coefficient")]
        [Description("Controls the speed that bots will recover from a weapon's recoil. Higher = faster decay")]
        [MinMax(0.01f, 5f, 100f)]
        [Advanced]
        public float RecoilDecayCoef = 1;

        [Name("Recoil Barrel Rise Coefficient")]
        [Description("Controls the speed that a bot's weapon will rise after a shot occurs.  Higher = faster application")]
        [MinMax(0.01f, 20f, 100f)]
        [Advanced]
        public float RecoilRiseCoef = 8;

        [Name("Ammo Shootability" )]
        [Description(
            "Lower is BETTER. " +
            "How Shootable this ammo type is, affects semi auto firerate and full auto burst length." +
            "Value is scaled but roughly gives a plus or minus 20% to firerate depending on the value set here." +
            "For Example. 9x19 will shoot about 20% faster fire-rate on semi-auto at 50 meters" +
            ", and fire 20% longer bursts when on full auto"
            )]
        [Percentage0to1(0.01f)]
        [Advanced]
        [DefaultDictionary(nameof(AmmoCaliberShootabilityDefaults))]
        public Dictionary<ICaliber, float> AmmoCaliberShootability = new Dictionary<ICaliber, float>()
        {
            { ICaliber.Caliber9x18PM, 0.225f },
            { ICaliber.Caliber9x19PARA, 0.275f },
            { ICaliber.Caliber46x30, 0.325f },
            { ICaliber.Caliber9x21, 0.325f },
            { ICaliber.Caliber57x28, 0.35f },
            { ICaliber.Caliber762x25TT, 0.425f },
            { ICaliber.Caliber1143x23ACP, 0.425f },
            { ICaliber.Caliber9x33R, 0.65f },
            { ICaliber.Caliber545x39, 0.525f },
            { ICaliber.Caliber556x45NATO, 0.525f },
            { ICaliber.Caliber9x39, 0.6f },
            { ICaliber.Caliber762x35, 0.575f },
            { ICaliber.Caliber762x39, 0.675f },
            { ICaliber.Caliber366TKM, 0.675f },
            { ICaliber.Caliber762x51, 0.725f },
            { ICaliber.Caliber127x55, 0.775f },
            { ICaliber.Caliber762x54R, 0.85f },
            { ICaliber.Caliber86x70, 1.0f },
            { ICaliber.Caliber20g, 0.7f },
            { ICaliber.Caliber12g, 0.725f },
            { ICaliber.Caliber23x75, 0.85f },
            { ICaliber.Caliber26x75, 1f },
            { ICaliber.Caliber30x29, 1f },
            { ICaliber.Caliber40x46, 1f },
            { ICaliber.Caliber40mmRU, 1f },
            { ICaliber.Caliber127x108, 0.5f },
            { ICaliber.Caliber68x51, 0.6f },
            { ICaliber.Default, 0.5f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ICaliber, float> AmmoCaliberShootabilityDefaults = new Dictionary<ICaliber, float>()
        {
            { ICaliber.Caliber9x18PM, 0.225f },
            { ICaliber.Caliber9x19PARA, 0.275f },
            { ICaliber.Caliber46x30, 0.325f },
            { ICaliber.Caliber9x21, 0.325f },
            { ICaliber.Caliber57x28, 0.35f },
            { ICaliber.Caliber762x25TT, 0.425f },
            { ICaliber.Caliber1143x23ACP, 0.425f },
            { ICaliber.Caliber9x33R, 0.65f },
            { ICaliber.Caliber545x39, 0.525f },
            { ICaliber.Caliber556x45NATO, 0.525f },
            { ICaliber.Caliber9x39, 0.6f },
            { ICaliber.Caliber762x35, 0.575f },
            { ICaliber.Caliber762x39, 0.675f },
            { ICaliber.Caliber366TKM, 0.675f },
            { ICaliber.Caliber762x51, 0.725f },
            { ICaliber.Caliber127x55, 0.775f },
            { ICaliber.Caliber762x54R, 0.85f },
            { ICaliber.Caliber86x70, 1.0f },
            { ICaliber.Caliber20g, 0.7f },
            { ICaliber.Caliber12g, 0.725f },
            { ICaliber.Caliber23x75, 0.85f },
            { ICaliber.Caliber26x75, 1f },
            { ICaliber.Caliber30x29, 1f },
            { ICaliber.Caliber40x46, 1f },
            { ICaliber.Caliber40mmRU, 1f },
            { ICaliber.Caliber127x108, 0.5f },
            { ICaliber.Caliber68x51, 0.6f },
            { ICaliber.Default, 0.5f },
        };

        [Name("Max FullAuto Distances")]
        [Description("The maximum distance a bot using this caliber can fire it full auto. Not all values are used since some calibers don't have any full auto weapons that use it.")]
        [MinMax(10f, 150f)]
        [Advanced]
        [DefaultDictionary(nameof(AmmoCaliberFullAutoMaxDistancesDefaults))]
        public Dictionary<ICaliber, float> AmmoCaliberFullAutoMaxDistances = new Dictionary<ICaliber, float>
        {
            { ICaliber.Caliber9x18PM, 80f },
            { ICaliber.Caliber9x19PARA, 80f },
            { ICaliber.Caliber46x30, 70f },
            { ICaliber.Caliber9x21, 70f },
            { ICaliber.Caliber57x28, 70f },
            { ICaliber.Caliber762x25TT, 70f },
            { ICaliber.Caliber1143x23ACP, 75f },
            { ICaliber.Caliber9x33R, 75f },
            { ICaliber.Caliber545x39, 65f },
            { ICaliber.Caliber556x45NATO, 65f },
            { ICaliber.Caliber9x39, 60f },
            { ICaliber.Caliber762x35, 55f },
            { ICaliber.Caliber762x39, 50f },
            { ICaliber.Caliber366TKM, 45f },
            { ICaliber.Caliber762x51, 45f },
            { ICaliber.Caliber127x55, 50f },
            { ICaliber.Caliber762x54R, 50f },
            { ICaliber.Caliber86x70, 40f },
            { ICaliber.Caliber20g, 30f },
            { ICaliber.Caliber12g, 30f },
            { ICaliber.Caliber23x75, 30f },
            { ICaliber.Caliber26x75, 30f },
            { ICaliber.Caliber30x29, 30f },
            { ICaliber.Caliber40x46, 30f },
            { ICaliber.Caliber40mmRU, 30f },
            { ICaliber.Caliber127x108, 30f },
            { ICaliber.Caliber68x51, 50f },
            { ICaliber.Default, 55f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ICaliber, float> AmmoCaliberFullAutoMaxDistancesDefaults = new Dictionary<ICaliber, float>()
        {
            { ICaliber.Caliber9x18PM, 80f },
            { ICaliber.Caliber9x19PARA, 80f },
            { ICaliber.Caliber46x30, 70f },
            { ICaliber.Caliber9x21, 70f },
            { ICaliber.Caliber57x28, 70f },
            { ICaliber.Caliber762x25TT, 70f },
            { ICaliber.Caliber1143x23ACP, 75f },
            { ICaliber.Caliber9x33R, 75f },
            { ICaliber.Caliber545x39, 65f },
            { ICaliber.Caliber556x45NATO, 65f },
            { ICaliber.Caliber9x39, 60f },
            { ICaliber.Caliber762x35, 55f },
            { ICaliber.Caliber762x39, 50f },
            { ICaliber.Caliber366TKM, 45f },
            { ICaliber.Caliber762x51, 45f },
            { ICaliber.Caliber127x55, 50f },
            { ICaliber.Caliber762x54R, 50f },
            { ICaliber.Caliber86x70, 40f },
            { ICaliber.Caliber20g, 30f },
            { ICaliber.Caliber12g, 30f },
            { ICaliber.Caliber23x75, 30f },
            { ICaliber.Caliber26x75, 30f },
            { ICaliber.Caliber30x29, 30f },
            { ICaliber.Caliber40x46, 30f },
            { ICaliber.Caliber40mmRU, 30f },
            { ICaliber.Caliber127x108, 30f },
            { ICaliber.Caliber68x51, 50f },
            { ICaliber.Default, 55f },
        };

        [Name("Weapon Shootability")]
        [Description(
            "Lower is BETTER. " +
            "How Shootable this weapon type is, affects semi auto firerate and full auto burst length." +
            "Value is scaled but roughly gives a plus or minus 20% to firerate depending on the value set here." +
            "For Example. SMGs will shoot about 20% faster fire-rate on semi-auto at 50 meters" +
            ", and fire 20% longer bursts when on full auto"
            )]
        [Percentage0to1(0.01f)]
        [Advanced]
        [DefaultDictionary(nameof(WeaponClassShootabilityDefaults))]
        public Dictionary<IWeaponClass, float> WeaponClassShootability = new Dictionary<IWeaponClass, float>
        {
            { IWeaponClass.Default, 0.425f },
            { IWeaponClass.assaultCarbine, 0.5f },
            { IWeaponClass.assaultRifle, 0.5f },
            { IWeaponClass.machinegun, 0.15f },
            { IWeaponClass.smg, 0.25f },
            { IWeaponClass.pistol, 0.4f },
            { IWeaponClass.marksmanRifle, 0.75f },
            { IWeaponClass.sniperRifle, 1f },
            { IWeaponClass.shotgun, 0.75f },
            { IWeaponClass.grenadeLauncher, 1f },
            { IWeaponClass.specialWeapon, 1f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<IWeaponClass, float> WeaponClassShootabilityDefaults = new Dictionary<IWeaponClass, float>()
        {
            { IWeaponClass.Default, 0.425f },
            { IWeaponClass.assaultCarbine, 0.5f },
            { IWeaponClass.assaultRifle, 0.5f },
            { IWeaponClass.machinegun, 0.15f },
            { IWeaponClass.smg, 0.25f },
            { IWeaponClass.pistol, 0.4f },
            { IWeaponClass.marksmanRifle, 0.75f },
            { IWeaponClass.sniperRifle, 1f },
            { IWeaponClass.shotgun, 0.75f },
            { IWeaponClass.grenadeLauncher, 1f },
            { IWeaponClass.specialWeapon, 1f },
        };

        [Name("Weapon Firerate Wait Time")]
        [Description(
            "HIGHER is BETTER. " +
            "This is the time to wait inbetween shots for every meter." +
            "the number is divided by the distance to their target, to get a wait period between shots." +
            "For Example. With a setting of 100: " +
            "if a target is 50m away, they will wait 0.5 sec between shots because 50 / 100 is 0.5." +
            "This number is later modified by the Shootability multiplier, to get a final fire-rate that gets sent to a bot."
            )]
        [MinMax(30f, 250f, 1f)]
        [Advanced]
        [DefaultDictionary(nameof(WeaponPerMeterDefaults))]
        public Dictionary<IWeaponClass, float> WeaponPerMeter = new Dictionary<IWeaponClass, float>
        {
            { IWeaponClass.Default, 120f },
            { IWeaponClass.assaultCarbine, 140 },
            { IWeaponClass.assaultRifle, 130 },
            { IWeaponClass.machinegun, 135 },
            { IWeaponClass.smg, 160 },
            { IWeaponClass.pistol, 65 },
            { IWeaponClass.marksmanRifle, 75 },
            { IWeaponClass.sniperRifle, 50 },
            { IWeaponClass.shotgun, 60 },
            { IWeaponClass.grenadeLauncher, 75 },
            { IWeaponClass.specialWeapon, 80 },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<IWeaponClass, float> WeaponPerMeterDefaults = new Dictionary<IWeaponClass, float>()
        {
            { IWeaponClass.Default, 120f },
            { IWeaponClass.assaultCarbine, 140 },
            { IWeaponClass.assaultRifle, 130 },
            { IWeaponClass.machinegun, 135 },
            { IWeaponClass.smg, 160 },
            { IWeaponClass.pistol, 65 },
            { IWeaponClass.marksmanRifle, 75 },
            { IWeaponClass.sniperRifle, 50 },
            { IWeaponClass.shotgun, 60 },
            { IWeaponClass.grenadeLauncher, 75 },
            { IWeaponClass.specialWeapon, 80 },
        };

        [Name("Bot Preferred Shoot Distances")]
        [Description(
            "The distances that a bot prefers to shoot a particular weapon class. " +
            "Bots will try to close the distance if further than this."
            )]
        [MinMax(10f, 250f, 1f)]
        [Advanced]
        [DefaultDictionary(nameof(EngagementDistanceDefaults))]
        public Dictionary<IWeaponClass, float> EngagementDistance = new Dictionary<IWeaponClass, float>
        {
            { IWeaponClass.Default, 125f },
            { IWeaponClass.assaultCarbine, 125f },
            { IWeaponClass.assaultRifle, 150f },
            { IWeaponClass.machinegun, 125f },
            { IWeaponClass.smg, 70f },
            { IWeaponClass.pistol, 50f },
            { IWeaponClass.marksmanRifle, 175f },
            { IWeaponClass.sniperRifle, 300f },
            { IWeaponClass.shotgun, 50f },
            { IWeaponClass.grenadeLauncher, 100f },
            { IWeaponClass.specialWeapon, 100f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<IWeaponClass, float> EngagementDistanceDefaults = new Dictionary<IWeaponClass, float>()
        {
            { IWeaponClass.Default, 125f },
            { IWeaponClass.assaultCarbine, 125f },
            { IWeaponClass.assaultRifle, 150f },
            { IWeaponClass.machinegun, 125f },
            { IWeaponClass.smg, 70f },
            { IWeaponClass.pistol, 50f },
            { IWeaponClass.marksmanRifle, 175f },
            { IWeaponClass.sniperRifle, 300f },
            { IWeaponClass.shotgun, 50f },
            { IWeaponClass.grenadeLauncher, 100f },
            { IWeaponClass.specialWeapon, 100f },
        };

        [JsonIgnore]
        [Hidden]
        private const string Shootability = "Affects Weapon Shootability Calculations. ";

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float WeaponClassScaling = 0.25f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float RecoilScaling = 0.35f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float ErgoScaling = 0.08f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float AmmoCaliberScaling = 0.3f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float WeaponProficiencyScaling = 0.3f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float DifficultyScaling = 0.3f;
    }
}