using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class LootingBotsSettings : SAINSettingsBase<LootingBotsSettings>, ISAINSettings
    {
        public object GetDefaults()
        {
            return Defaults;
        }

        [Name("Bot Extraction From Loot")]
        public bool ExtractFromLoot = true;

        [Name("Min Loot Val PMC")]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValPMC = 500000;

        [Name("Min Loot Val SCAV")]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValSCAV = 200000;

        [Name("Min Loot Val Other")]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValOther = 350000;

        [Name("Min Loot Val Exception")]
        [Description("If a bot's loot value is greater than or equal to this, they will extract even with space available in their inventory.")]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValException = 1500000;
    }
}