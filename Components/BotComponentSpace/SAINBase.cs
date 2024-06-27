using EFT;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.Personalities;

namespace SAIN.SAINComponent
{
    public abstract class SAINBase
    {
        public SAINBase(BotComponent bot)
        {
            Bot = bot;
        }

        public BotComponent Bot { get; private set; }
        public BotOwner BotOwner => Bot.Person.BotOwner;
        public Player Player => Bot.Person.Player;
        public IPlayer IPlayer => Bot.Person.IPlayer;
        public GlobalSettingsClass GlobalSettings => GlobalSettingsClass.Instance;
    }
}