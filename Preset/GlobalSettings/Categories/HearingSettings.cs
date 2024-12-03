using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class HearingSettings : SAINSettingsBase<HearingSettings>, ISAINSettings
    {
        static HearingSettings()
        {
            // Hearing Dispersion
            HEAR_DISPERSION_VALUES_Defaults = new Dictionary<SAINSoundType, float>()
            {
                { SAINSoundType.Shot, 17.5f },
                { SAINSoundType.SuppressedShot, 13.5f },
                { SAINSoundType.FootStep, 12.5f },
            };
            const float defaultDispersion = 12.5f;
            Helpers.ListHelpers.PopulateKeys(HEAR_DISPERSION_VALUES_Defaults, defaultDispersion);

            // Gunfire Hearing Distances
            HearingDistancesDefaults = new Dictionary<ECaliber, float>()
            {
                { ECaliber.Caliber9x18PM, 110f },
                { ECaliber.Caliber9x19PARA, 110f },
                { ECaliber.Caliber46x30, 120f },
                { ECaliber.Caliber9x21, 120f },
                { ECaliber.Caliber57x28, 120f },
                { ECaliber.Caliber762x25TT, 120f },
                { ECaliber.Caliber1143x23ACP, 115f },
                { ECaliber.Caliber9x33R, 125 },
                { ECaliber.Caliber545x39, 160 },
                { ECaliber.Caliber556x45NATO, 160 },
                { ECaliber.Caliber9x39, 160 },
                { ECaliber.Caliber762x35, 175 },
                { ECaliber.Caliber762x39, 175 },
                { ECaliber.Caliber366TKM, 175 },
                { ECaliber.Caliber762x51, 200f },
                { ECaliber.Caliber127x55, 200f },
                { ECaliber.Caliber762x54R, 225f },
                { ECaliber.Caliber86x70, 250f },
                { ECaliber.Caliber20g, 185 },
                { ECaliber.Caliber12g, 185 },
                { ECaliber.Caliber23x75, 210 },
                { ECaliber.Caliber26x75, 50 },
                { ECaliber.Caliber30x29, 50 },
                { ECaliber.Caliber40x46, 50 },
                { ECaliber.Caliber40mmRU, 50 },
                { ECaliber.Caliber127x108, 300 },
                { ECaliber.Caliber68x51, 200f },
                { ECaliber.Default, 125 },
            };
            const float defaultDistance = 125;
            Helpers.ListHelpers.PopulateKeys(HearingDistancesDefaults, defaultDistance);
        }

        [Name("Max Footstep Audio Distance")]
        [Description("The Maximum Range that a bot can hear footsteps, sprinting, and jumping, turning, gear sounds, and any movement related sounds, in meters.")]
        [MinMax(10f, 150f, 1f)]
        public float MaxFootstepAudioDistance = 70f;

        [Name("Max Footstep Audio Distance without Headphones")]
        [Description("The Maximum Range that a bot can hear footsteps, sprinting, and jumping, turning, gear sounds, and any movement related sounds, in meters when not wearing headphones.")]
        [MinMax(10f, 150f, 1f)]
        public float MaxFootstepAudioDistanceNoHeadphones = 50f;

        [Name("Hearing Randomization and Estimation")]
        [Description(_dispersion_descr)]
        [MinMax(1f, 100f, 100f)]
        [DefaultDictionary(nameof(HEAR_DISPERSION_VALUES_Defaults))]
        public Dictionary<SAINSoundType, float> HEAR_DISPERSION_VALUES = new Dictionary<SAINSoundType, float>
        {
            { SAINSoundType.Shot, 17.5f },
            { SAINSoundType.SuppressedShot, 13.5f },
            { SAINSoundType.FootStep, 12.5f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<SAINSoundType, float> HEAR_DISPERSION_VALUES_Defaults;

        [JsonIgnore]
        [Hidden]
        private const string _dispersion_descr = "Higher = Less Randomization and more accuracy. " +
            "The distance to the sound's position, in meters, is divided by the number here. " +
            "Example: A unsuppressed gunshot is 150 meters away. And the dispersion value for unsuppressed gunfire is 20. So We divide 150 by 20 to result in 7.5, " +
            "so the randomized position that a bot thinks a gunshot came from is a position within 7.5 meters from the actual source of the gunshot." +
            "Note: this randomized position must be somewhere that is walkable so that they can potentially be able the investigate it. " +
            "It is also not randomized in height to avoid bots having difficulty navigating to where they think a sound came from, " +
            "so imagine a flat plane around you that extends 7.5 meters away that includes all walkable space, " +
            "if you shoot - that bot 150 away will estimate you are somewhere within that 7.5 meter radius, on the same height level that you are on. " +
            "If there is no walkable space around you, it will find the closest walkable place.";

        [Name("Minimum Hearing Randomization")]
        [Description("Higher = More Randomization, less accuracy in position prediction. Minimum Dispersion of a bot's estimated position from a sound they heard. In Meters. ")]
        [MinMax(0.0f, 2f, 100f)]
        public float HEAR_DISPERSION_MIN = 0.5f;

        [Name("No Randomization Distance")]
        [Description("If the distance to a sound is less and or equal to this number, a bot will perfectly predict the source position, so no randomization or dispersion at all. A value of 0 will disable this.")]
        [MinMax(0f, 50f, 100f)]
        public float HEAR_DISPERSION_MIN_DISTANCE_THRESH = 10f;

        [Name("Max Randomization Distance")]
        [Description("The max cap, in meters, that an estimated position can be from the real position that a sound is played from. ")]
        [MinMax(10f, 250f, 100f)]
        public float HEAR_DISPERSION_MAX_DISPERSION = 50f;

        [Name("Hearing Randomization Angle - Maximum")]
        [Description(_hear_angle_descr)]
        [MinMax(0.1f, 3f, 100f)]
        public float HEAR_DISPERSION_ANGLE_MULTI_MAX = 1.5f;

        [Name("Hearing Randomization Angle - Minimum")]
        [Description(_hear_angle_descr)]
        [MinMax(0.1f, 3f, 100f)]
        public float HEAR_DISPERSION_ANGLE_MULTI_MIN = 0.5f;

        [JsonIgnore]
        [Hidden]
        private const string _hear_angle_descr = "If a bot is looking at the source of a sound, they will be more accurate in their position prediction, up to the Minimum value here. " +
            "If it is directly behind them, randomization will be multiplied by the Maximumm value here. " +
            "It is a linear scale between these, so a sound directly to their right or left, will have the difference between the Maximumm and Minimum. " +
            "Setting both the Min and the Max to 1.0 will disable this system.";

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Looting = 60f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_MovementTurnSkid = 30f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_GrenadePinDraw = 35f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Prone = 50f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Healing = 40f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Reload = 30f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Surgery = 55f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_DryFire = 10f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float MaxSoundRange_FallLanding = 70;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_AimingandGearRattle = 35f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_EatDrink = 40f;

        [MinMax(1f, 150f, 100f)]
        public float MaxRangeToReportEnemyActionNoHeadset = 50f;

        [Name("Hearing Delay / Reaction Time with Active Enemy")]
        [MinMax(0.0f, 1f, 100f)]
        public float BaseHearingDelayWithEnemy = 0.2f;

        [Name("Hearing Delay / Reaction Time while At Peace")]
        [MinMax(0.0f, 1f, 100f)]
        public float BaseHearingDelayAtPeace = 0.35f;

        [Name("Global Gunshot Audible Range Multiplier")]
        [MinMax(0.1f, 2f, 100f)]
        public float GunshotAudioMultiplier = 1f;

        [Name("Global Footstep Audible Range Multiplier")]
        [MinMax(0.1f, 2f, 100f)]
        public float FootstepAudioMultiplier = 1f;

        [Name("Suppressed Sound Modifier")]
        [Description("Audible Gun Range is multiplied by this number when using a suppressor")]
        [MinMax(0.1f, 0.95f, 100f)]
        public float SuppressorModifier = 0.6f;

        [Name("Subsonic Sound Modifier")]
        [Description("Audible Gun Range is multiplied by this number when using a suppressor and subsonic ammo")]
        [MinMax(0.1f, 0.95f, 100f)]
        public float SubsonicModifier = 0.33f;

        [Name("Hearing Distances by Ammo Type")]
        [Description("How far a bot can hear a gunshot when fired from each specific caliber listed here.")]
        [MinMax(30f, 400f, 10f)]
        [Advanced]
        [DefaultDictionary(nameof(HearingDistancesDefaults))]
        public Dictionary<ECaliber, float> HearingDistances = new Dictionary<ECaliber, float>
        {
            { ECaliber.Caliber9x18PM, 110f },
            { ECaliber.Caliber9x19PARA, 110f },
            { ECaliber.Caliber46x30, 120f },
            { ECaliber.Caliber9x21, 120f },
            { ECaliber.Caliber57x28, 120f },
            { ECaliber.Caliber762x25TT, 120f },
            { ECaliber.Caliber1143x23ACP, 115f },
            { ECaliber.Caliber9x33R, 125 },
            { ECaliber.Caliber545x39, 160 },
            { ECaliber.Caliber556x45NATO, 160 },
            { ECaliber.Caliber9x39, 160 },
            { ECaliber.Caliber762x35, 175 },
            { ECaliber.Caliber762x39, 175 },
            { ECaliber.Caliber366TKM, 175 },
            { ECaliber.Caliber762x51, 200f },
            { ECaliber.Caliber127x55, 200f },
            { ECaliber.Caliber762x54R, 225f },
            { ECaliber.Caliber86x70, 250f },
            { ECaliber.Caliber20g, 185 },
            { ECaliber.Caliber12g, 185 },
            { ECaliber.Caliber23x75, 210 },
            { ECaliber.Caliber26x75, 50 },
            { ECaliber.Caliber30x29, 50 },
            { ECaliber.Caliber40x46, 50 },
            { ECaliber.Caliber40mmRU, 50 },
            { ECaliber.Caliber127x108, 300 },
            { ECaliber.Caliber68x51, 200f },
            { ECaliber.Default, 125 },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ECaliber, float> HearingDistancesDefaults = new Dictionary<ECaliber, float>()
        {
            { ECaliber.Caliber9x18PM, 110f },
            { ECaliber.Caliber9x19PARA, 110f },
            { ECaliber.Caliber46x30, 120f },
            { ECaliber.Caliber9x21, 120f },
            { ECaliber.Caliber57x28, 120f },
            { ECaliber.Caliber762x25TT, 120f },
            { ECaliber.Caliber1143x23ACP, 115f },
            { ECaliber.Caliber9x33R, 125 },
            { ECaliber.Caliber545x39, 160 },
            { ECaliber.Caliber556x45NATO, 160 },
            { ECaliber.Caliber9x39, 160 },
            { ECaliber.Caliber762x35, 175 },
            { ECaliber.Caliber762x39, 175 },
            { ECaliber.Caliber366TKM, 175 },
            { ECaliber.Caliber762x51, 200f },
            { ECaliber.Caliber127x55, 200f },
            { ECaliber.Caliber762x54R, 225f },
            { ECaliber.Caliber86x70, 250f },
            { ECaliber.Caliber20g, 185 },
            { ECaliber.Caliber12g, 185 },
            { ECaliber.Caliber23x75, 210 },
            { ECaliber.Caliber26x75, 50 },
            { ECaliber.Caliber30x29, 50 },
            { ECaliber.Caliber40x46, 50 },
            { ECaliber.Caliber40mmRU, 50 },
            { ECaliber.Caliber127x108, 300 },
            { ECaliber.Caliber68x51, 200f },
            { ECaliber.Default, 125 },
        };

        public override void Init(List<ISAINSettings> list)
        {
            Helpers.ListHelpers.CloneEntries(HEAR_DISPERSION_VALUES_Defaults, HEAR_DISPERSION_VALUES);
            Helpers.ListHelpers.CloneEntries(HearingDistancesDefaults, HearingDistances);
            list.Add(this);
        }
    }
}