namespace SAIN.SAINComponent.Classes.Info
{
    public class WeaponAIPresetHistory
    {
        public WeaponAIPresetHistory(WeaponAIPreset preset)
        {
            WeaponAIPresetType = preset.WeaponAIPresetType;
            BaseShift = preset.BaseShift;
            XZ_COEF = preset.XZ_COEF;
        }

        public readonly float BaseShift;
        public readonly float XZ_COEF;
        public EWeaponAIPresetType WeaponAIPresetType;
    }
}