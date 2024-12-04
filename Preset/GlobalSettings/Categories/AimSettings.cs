using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class AimSettings : SAINSettingsBase<AimSettings>, ISAINSettings
    {
        [Category("Aim Target")]
        public HitEffectSettings HitEffects = new HitEffectSettings();

        [Name("Always Aim Center Mass Global")]
        [Description("Force Bots to aim for center of mass. If this is disabled, all bots will have Always Aim Center Mass turned OFF, so their individual settings will be ignored.")]
        [Category("Aim Target")]
        public bool AimCenterMassGlobal = true;

        [Category("Scatter Modifiers")]
        [Name("Enemy Move Scatter Max Buff")]
        [Description("The max buff to bot scatter, so if their enemy is standing still. Scales with velocity. A value of 1 is disabled")]
        [MinMax(1f, 1.5f, 100f)]
        public float EnemyVelocityMaxBuff = 1.2f;

        [Category("Scatter Modifiers")]
        [Name("Enemy Move Scatter Max Debuff")]
        [Description("The minimum debuff to bot scatter, so if their enemy is moving at full speed, but not sprinting. Scales with velocity. A value of 1 is disabled")]
        [MinMax(0.5f, 1f, 100f)]
        public float EnemyVelocityMaxDebuff = 0.8f;

        [Category("Scatter Modifiers")]
        [Name("Enemy Move Scatter Sprint Debuff")]
        [Description("How much to divide bot scatter by if their enemy is sprinting. So the lower the number, the worse their aim will be. A value of 1 is disabled")]
        [MinMax(0.5f, 1f, 100f)]
        public float EnemySprintingScatterMulti = 0.66f;

        [Category("Scatter Modifiers")]
        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 100f)]
        public float PoseLevelScatterMulti = 0.8f;

        [Category("Scatter Modifiers")]
        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 100f)]
        public float ProneScatterMulti = 0.7f;

        [Category("Scatter Modifiers")]
        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 100f)]
        public float PartVisScatterMulti = 0.75f;

        [Category("Scatter Modifiers")]
        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(1f, 1.5f, 100f)]
        public float OpticFarMulti = 1.2f;

        [Category("Scatter Modifiers")]
        [Advanced]
        [MinMax(25f, 150f, 10f)]
        public float OpticFarDistance = 100f;

        [Category("Scatter Modifiers")]
        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 100f)]
        public float OpticCloseMulti = 0.8f;

        [Category("Scatter Modifiers")]
        [Advanced]
        [MinMax(25f, 150f, 10f)]
        public float OpticCloseDistance = 75f;

        [Category("Scatter Modifiers")]
        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 100f)]
        public float RedDotFarMulti = 0.85f;

        [Category("Scatter Modifiers")]
        [Advanced]
        [MinMax(25f, 150f, 10f)]
        public float RedDotFarDistance = 100f;

        [Category("Scatter Modifiers")]
        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(1f, 1.5f, 100f)]
        public float RedDotCloseMulti = 1.15f;

        [Category("Scatter Modifiers")]
        [Advanced]
        [MinMax(25f, 150f, 10f)]
        public float RedDotCloseDistance = 65f;

        [Category("Scatter Modifiers")]
        [Description("Lower is more scatter")]
        [Advanced]
        [MinMax(0.5f, 1f, 100f)]
        public float IronSightFarMulti = 0.7f;

        [Category("Scatter Modifiers")]
        [Advanced]
        [MinMax(25f, 200f, 10f)]
        public float IronSightScaleDistanceStart = 50f;

        [Category("Scatter Modifiers")]
        [Advanced]
        [MinMax(25f, 200f, 10f)]
        public float IronSightScaleDistanceEnd = 100f;

        [Name("Center Mass Point")]
        [Description("The maximum height that bots will target if Always Aim Center Mass is on. " +
            "A value of 0 will be directly on the center your head, a value of 1 will be directly at the floor below you at your feet. " +
            "If their aim target is above this, the height will be adjusted to be where this point is.")]
        [Category("Aim Target")]
        [Advanced]
        [MinMax(0f, 1f, 10000f)]
        public float CenterMassVal = 0.3125f;

        [Category("Time To Aim")]
        [Name("Global Faster CQB Reactions")]
        [Description("if this toggle is disabled, all bots will have Faster CQB Reactions turned OFF, so their individual settings will be ignored.")]
        public bool FasterCQBReactionsGlobal = true;

        [Category("Time To Aim")]
        [Name("Aim Down Sight Aim Time Multiplier")]
        [Description("If a bot is aiming down sights, their time to aim will be multiplied by this number")]
        [MinMax(0.01f, 1f, 100f)]
        public float AimDownSightsAimTimeMultiplier = 0.8f;

        [Name("PMCs Can Aim for Headshots")]
        [Category("Aim Target")]
        public bool PMCSAimForHead = false;

        [Category("Aim Target")]
        [Name("PMCs Can Aim for Headshots - Percentage Chance")]
        [Percentage]
        public float PMCAimForHeadChance = 33f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
            HitEffects.Init(list);
        }
    }
}