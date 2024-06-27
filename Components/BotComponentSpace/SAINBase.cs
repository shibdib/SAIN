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

    public abstract class SAINSubBase<T> where T : ISAINClass
    {
        public SAINSubBase(T sainClass)
        {
            BaseClass = sainClass;
        }

        protected T BaseClass;

        public BotComponent Bot => BaseClass.Bot;
        protected BotOwner BotOwner => Bot.Person.BotOwner;
        protected Player Player => Bot.Person.Player;
        protected IPlayer IPlayer => Bot.Person.IPlayer;
        protected GlobalSettingsClass GlobalSettings => GlobalSettingsClass.Instance;
    }
}