using Newtonsoft.Json;
using SAIN.Attributes;
using System;

namespace SAIN.Preset.GlobalSettings
{

    public abstract class SAINSettingsBase<T> : ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        public void CreateDefault()
        {
            Defaults = (T)Activator.CreateInstance(typeof(T));
        }

        public void UpdateDefaults(object values)
        {
            CloneSettingsClass.CopyFields(values, Defaults);
        }

        [Hidden]
        [JsonIgnore]
        public T Defaults;
    }
}