using EFT;
using SAIN.Preset.GlobalSettings;

namespace SAIN.SAINComponent
{
    public abstract class BotBaseClass : PresetUpdaterBase
    {
        public BotBaseClass(BotComponent bot) : base(bot)
        {
            Bot = bot;
        }

        public BotComponent Bot { get; private set; }
        public BotOwner BotOwner => Bot.Person.BotOwner;
        public Player Player => Bot.Person.Player;
        public IPlayer IPlayer => Bot.Person.IPlayer;
        public GlobalSettingsClass GlobalSettings => GlobalSettingsClass.Instance;
    }

    public abstract class BotSubClassBase<T> : PresetUpdaterBase where T : ISAINClass
    {
        public BotSubClassBase(T sainClass) : base(sainClass)
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