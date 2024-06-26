namespace SAIN.Preset.GlobalSettings
{
    public interface ISAINSettings
    {
        object GetDefaults();
        void CreateDefault();
        void UpdateDefaults(object values);
    }
}