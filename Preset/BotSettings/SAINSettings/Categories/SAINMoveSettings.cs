using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINMoveSettings
    {
        [Hidden]
        [JsonIgnore]
        public float BASE_ROTATE_SPEED = 300;

        [Hidden]
        [JsonIgnore]
        public float FIRST_TURN_SPEED = 240;
        // 160 default

        [Hidden]
        [JsonIgnore]
        public float FIRST_TURN_BIG_SPEED = 350;
        // 320 default

        [Hidden]
        [JsonIgnore]
        public float TURN_SPEED_ON_SPRINT = 300;
        // 200 default

        [Hidden]
        [JsonIgnore]
        public float RUN_TO_COVER_MIN = 2f;

        [Hidden]
        [JsonIgnore]
        public float BOT_MOVE_IF_DELTA = 0.05f;
    }
}