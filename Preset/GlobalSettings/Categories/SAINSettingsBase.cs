using Newtonsoft.Json;
using SAIN.Attributes;
using System;

namespace SAIN.Preset.GlobalSettings
{
    public interface ISAINSettings
    {
        object GetDefaults();
    }

    public abstract class SAINSettingsBase<T>
    {
        [Hidden]
        [JsonIgnore]
        public static readonly T Defaults = (T)Activator.CreateInstance(typeof(T));
    }
}