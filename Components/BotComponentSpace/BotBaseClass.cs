using EFT;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using System;

namespace SAIN.SAINComponent
{
    public abstract class BotBaseClass : PresetUpdaterBase
    {
        public BotBaseClass(BotComponent bot)
        {
            Bot = bot;
        }

        protected override void SubscribeToPresetChanges(Action<SAINPresetClass> func)
        {
            base.SubscribeToPresetChanges(func);
            Bot.OnSAINDisposed += this.UnSubscribeToPresetChanges;
        }

        protected override void UnSubscribeToPresetChanges()
        {
            base.UnSubscribeToPresetChanges();
            Bot.OnSAINDisposed -= this.UnSubscribeToPresetChanges;
        }

        public BotComponent Bot { get; private set; }
        public BotOwner BotOwner => Bot.Person.BotOwner;
        public Player Player => Bot.Person.Player;
        public IPlayer IPlayer => Bot.Person.IPlayer;
        public GlobalSettingsClass GlobalSettings => GlobalSettingsClass.Instance;
    }

    public abstract class BotSubClassBase<T> : PresetUpdaterBase where T : ISAINClass
    {
        public BotSubClassBase(T sainClass)
        {
            BaseClass = sainClass;
        }

        protected T BaseClass;

        protected override void SubscribeToPresetChanges(Action<SAINPresetClass> func)
        {
            base.SubscribeToPresetChanges(func);
            Bot.OnSAINDisposed += this.UnSubscribeToPresetChanges;
        }

        protected override void UnSubscribeToPresetChanges()
        {
            base.UnSubscribeToPresetChanges();
            Bot.OnSAINDisposed -= this.UnSubscribeToPresetChanges;
        }

        public BotComponent Bot => BaseClass.Bot;
        protected BotOwner BotOwner => Bot.Person.BotOwner;
        protected Player Player => Bot.Person.Player;
        protected IPlayer IPlayer => Bot.Person.IPlayer;
        protected GlobalSettingsClass GlobalSettings => GlobalSettingsClass.Instance;
    }
}