namespace SAIN.Preset.GlobalSettings
{
    public interface ISAINSettings
    {
        void Update();
        object GetDefaults();
        void CreateDefault();
        void UpdateDefaults(object values);
    }
}