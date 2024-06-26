using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class FlashlightSettings
    {
        [JsonIgnore]
        [Hidden]
        public static readonly FlashlightSettings Defaults = new FlashlightSettings();

        [Default(3f)]
        [MinMax(0.25f, 10f, 100f)]
        public float DazzleEffectiveness = 3f;

        [Default(30f)]
        [MinMax(0f, 60f)]
        public float MaxDazzleRange = 40f;

        [Default(true)]
        public bool AllowLightOnForDarkBuildings = true;

        [Default(true)]
        public bool TurnLightOffNoEnemyPMC = true;

        [Default(false)]
        public bool TurnLightOffNoEnemySCAV = false;

        [Default(true)]
        public bool TurnLightOffNoEnemyGOONS = true;

        [Default(false)]
        public bool TurnLightOffNoEnemyBOSS = false;

        [Default(false)]
        public bool TurnLightOffNoEnemyFOLLOWER = false;

        [Default(false)]
        public bool TurnLightOffNoEnemyRAIDERROGUE = false;

        [Default(false)]
        [Advanced]
        public bool DebugFlash = false;

        [Default(false)]
        public bool SillyMode = false;
    }
}