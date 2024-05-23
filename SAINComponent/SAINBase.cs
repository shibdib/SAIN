using BepInEx.Logging;
using EFT;
using SAIN.Preset.GlobalSettings;

namespace SAIN.SAINComponent
{
    public abstract class SAINBase : SAINComponentAbstract
    {
        public SAINBase(Bot sain) : base (sain)
        {
        }

        public BotOwner BotOwner => SAIN?.BotOwner;
        public Player Player => SAIN?.Player;
        public GlobalSettingsClass GlobalSettings => SAINPlugin.LoadedPreset?.GlobalSettings;
    }

    public class SAINComponentAbstract
    {
        public SAINComponentAbstract(Bot sain)
        {
            SAIN = sain;
        }

        public Bot SAIN { get; private set; }
    }
}