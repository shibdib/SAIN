using SAIN.Attributes;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINMoveSettings
    {
        [Name("Turn Speed Base")]
        [Default(200)]
        [MinMax(100f, 500f)]
        public float BASE_ROTATE_SPEED = 200;

        [Default(275f)]
        [Advanced]
        public float FIRST_TURN_SPEED = 225;

        [Default(250)]
        [Advanced]
        public float FIRST_TURN_BIG_SPEED = 250;

        [Name("Turn Speed Sprint")]
        [Default(250)]
        [MinMax(100f, 500f)]
        public float TURN_SPEED_ON_SPRINT = 250;

        [Hidden] 
        public float RUN_TO_COVER_MIN = 0f;
        [Hidden]
        public float COEF_IF_MOVE = 1.33f;
        [Hidden]
        public float TIME_COEF_IF_MOVE = 1.33f;
        [Hidden]    
        public float BOT_MOVE_IF_DELTA = 0.01f;
    }
}