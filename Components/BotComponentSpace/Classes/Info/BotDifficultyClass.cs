using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset;
using System.Collections;
using static HBAO_Core;

namespace SAIN.SAINComponent.Classes
{
    public class BotDifficultyClass : BotBase, IBotClass
    {
        public TemporaryStatModifiers GlobalDifficultyModifiers { get; }
        public TemporaryStatModifiers BotDifficultyModifiers { get; }
        public TemporaryStatModifiers PersonalityDifficultyModifiers { get; }
        public TemporaryStatModifiers LocationDifficultyModifiers { get; }

        public float AggressionModifier { get; private set; }
        public float HearingDistanceModifier { get; private set; }

        public BotDifficultyClass(BotComponent sain) : base(sain)
        {
            GlobalDifficultyModifiers = new TemporaryStatModifiers();
            BotDifficultyModifiers = new TemporaryStatModifiers();
            PersonalityDifficultyModifiers = new TemporaryStatModifiers();
            LocationDifficultyModifiers = new TemporaryStatModifiers();
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            dismiss();
        }

        public void UpdateSettings(SAINPresetClass preset)
        {
            dismiss();
            applyGlobal(preset);
            applyBot(preset);
            applyLocation(preset);
            applyPersonality(preset);
            apply();

            createCustomMods(preset);
        }

        private void createCustomMods(SAINPresetClass preset)
        {
            var globalSettings = preset.GlobalSettings.Difficulty;
            var locationSettings = preset.GlobalSettings.Location.Current();
            var botSettings = Bot.Info.FileSettings.Difficulty;
            var personalitySettings = Bot.Info.PersonalitySettingsClass.Difficulty;

            AggressionModifier = 1f *
                globalSettings.AggressionCoef *
                locationSettings.AggressionCoef *
                botSettings.AggressionCoef *
                personalitySettings.AggressionCoef;

            HearingDistanceModifier = 1f *
                globalSettings.HearingDistanceCoef *
                locationSettings.HearingDistanceCoef *
                botSettings.HearingDistanceCoef *
                personalitySettings.HearingDistanceCoef;
        }

        private void applyGlobal(SAINPresetClass preset)
        {
            var globalSettings = preset.GlobalSettings.Difficulty;
            var mods = GlobalDifficultyModifiers.Modifiers;

            mods.AccuratySpeedCoef = globalSettings.AccuracySpeedCoef;
            mods.PrecicingSpeedCoef = globalSettings.PrecisionSpeedCoef;
            mods.VisibleDistCoef = globalSettings.VisibleDistCoef;
            mods.ScatteringCoef = globalSettings.ScatteringCoef;
            //mods.PriorityScatteringCoef = globalSettings.PriorityScatteringCoef;
            mods.GainSightCoef = globalSettings.GainSightCoef;
            mods.HearingDistCoef = globalSettings.HearingDistanceCoef;
        }

        private void applyBot(SAINPresetClass preset)
        {
            var botSettings = Bot.Info.FileSettings.Difficulty;
            var mods = BotDifficultyModifiers.Modifiers;

            mods.AccuratySpeedCoef = botSettings.AccuracySpeedCoef;
            mods.PrecicingSpeedCoef = botSettings.PrecisionSpeedCoef;
            mods.VisibleDistCoef = botSettings.VisibleDistCoef;
            mods.ScatteringCoef = botSettings.ScatteringCoef;
            //mods.PriorityScatteringCoef = botSettings.PriorityScatteringCoef;
            mods.GainSightCoef = botSettings.GainSightCoef;
            mods.HearingDistCoef = botSettings.HearingDistanceCoef;
        }

        private void applyLocation(SAINPresetClass preset)
        {
            var locationSettings = preset.GlobalSettings.Location.Current();
            var mods = LocationDifficultyModifiers.Modifiers;

            mods.AccuratySpeedCoef = locationSettings.AccuracySpeedCoef;
            mods.PrecicingSpeedCoef = locationSettings.PrecisionSpeedCoef;
            mods.VisibleDistCoef = locationSettings.VisibleDistCoef;
            mods.ScatteringCoef = locationSettings.ScatteringCoef;
            //mods.PriorityScatteringCoef = locationSettings.PriorityScatteringCoef;
            mods.GainSightCoef = locationSettings.GainSightCoef;
            mods.HearingDistCoef = locationSettings.HearingDistanceCoef;
        }

        private void applyPersonality(SAINPresetClass preset)
        {
            var personalitySettings = Bot.Info.PersonalitySettingsClass.Difficulty;
            var mods = PersonalityDifficultyModifiers.Modifiers;

            mods.AccuratySpeedCoef = personalitySettings.AccuracySpeedCoef;
            mods.PrecicingSpeedCoef = personalitySettings.PrecisionSpeedCoef;
            mods.VisibleDistCoef = personalitySettings.VisibleDistCoef;
            mods.ScatteringCoef = personalitySettings.ScatteringCoef;
            //mods.PriorityScatteringCoef = personalitySettings.PriorityScatteringCoef;
            mods.GainSightCoef = personalitySettings.GainSightCoef;
            mods.HearingDistCoef = personalitySettings.HearingDistanceCoef;
        }

        private void apply()
        {
            var current = BotOwner.Settings.Current;
            current.Apply(GlobalDifficultyModifiers.Modifiers);
            current.Apply(BotDifficultyModifiers.Modifiers);
            current.Apply(PersonalityDifficultyModifiers.Modifiers);
            current.Apply(LocationDifficultyModifiers.Modifiers);
        }

        private void dismiss()
        {
            var current = BotOwner.Settings.Current;
            current.Dismiss(GlobalDifficultyModifiers.Modifiers);
            current.Dismiss(BotDifficultyModifiers.Modifiers);
            current.Dismiss(PersonalityDifficultyModifiers.Modifiers);
            current.Dismiss(LocationDifficultyModifiers.Modifiers);
        }
    }
}