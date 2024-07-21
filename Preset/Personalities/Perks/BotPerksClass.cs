using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAIN.Preset.Personalities.Perks
{
    public enum EBotPerk
    {
        EagleEye,
        TunnelVisioned,
        TriggerHappy,
        SprayNPray,
        AimGod,
        Attentive,
        CarefulShot,
        BadShot,
        LegMetaEnjoyer,
    }

    internal class BotPerksClass: BasePreset
    {
        public BotPerksClass(SAINPresetClass preset) : base(preset)
        {
        }

    }
}
