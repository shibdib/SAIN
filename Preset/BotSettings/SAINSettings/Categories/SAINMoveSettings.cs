using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINMoveSettings
    {
        [Hidden]
        [JsonIgnore]
        public float BASE_ROTATE_SPEED = 270;

        [Hidden]
        [JsonIgnore]
        public float FIRST_TURN_SPEED = 160;

        [Hidden]
        [JsonIgnore]
        public float FIRST_TURN_BIG_SPEED = 320;

        [Hidden]
        [JsonIgnore]
        public float TURN_SPEED_ON_SPRINT = 200;

        [Hidden]
        [JsonIgnore]
        public float RUN_TO_COVER_MIN = 0f;

        [Hidden]
        [JsonIgnore]
        public float COEF_IF_MOVE = 1.33f;

        [Hidden]
        [JsonIgnore]
        public float TIME_COEF_IF_MOVE = 1.33f;

        [Hidden]
        [JsonIgnore]
        public float BOT_MOVE_IF_DELTA = 0.05f;
    }
}