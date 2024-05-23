using BepInEx.Logging;
using EFT;
using SAIN.Preset.GlobalSettings;

namespace SAIN.SAINComponent
{
    public abstract class SAINBase : SAINComponentAbstract
    {
        public SAINBase(Bot bot) : base (bot)
        {
            BotOwner = bot.BotOwner;
            Player = bot.Player;
        }

        public readonly BotOwner BotOwner;
        public readonly Player Player;
        public GlobalSettingsClass GlobalSettings => SAINPlugin.LoadedPreset?.GlobalSettings;
    }

    public class SAINComponentAbstract
    {
        public SAINComponentAbstract(Bot sain)
        {
            SAINBot = sain;
        }

        public Bot SAINBot { get; private set; }
    }
}