using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Components;
using SAIN.Helpers;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class LocationSettingsClass : SAINSettingsBase<LocationSettingsClass>, ISAINSettings
    {
        [JsonConstructor]
        public LocationSettingsClass()
        {
        }

        public LocationSettingsClass(float baseMod)
        {
            addNewLocations(baseMod);
        }

        private void addNewLocations(float baseMod)
        {
            foreach (var type in EnumValues.GetEnum<ELocation>()) {
                if (Settings.ContainsKey(type))
                    continue;

                if (type == ELocation.None || type == ELocation.Terminal || type == ELocation.Town) {
                    continue;
                }

                var settings = new LocationSettings {
                    VisionSpeedModifier = baseMod,
                    ScatterMultiplier = baseMod,
                    AggressionMultiplier = baseMod,
                };
                Settings.Add(type, settings);
            }
        }

        public LocationSettings Current()
        {
            var gameworld = GameWorldComponent.Instance;
            if (gameworld == null || gameworld.Location == null) {
                return null;
            }
            if (Settings.TryGetValue(gameworld.Location.Location, out var settings)) {
                return settings;
            }
            return null;
        }

        [Name("Location Specific Modifiers")]
        [Description("These modifiers only apply to bots on the location they are assigned to. Applies to all bots equally.")]
        [MinMax(0.01f, 5f, 100f)]
        public Dictionary<ELocation, LocationSettings> Settings = new Dictionary<ELocation, LocationSettings>();

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}