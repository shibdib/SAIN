using HarmonyLib;
using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Preset.GlobalSettings
{
    public class GeneralSettings : SAINSettingsBase<GeneralSettings>, ISAINSettings
    {
        [Name("Bots Use Grenades")]
        public bool BotsUseGrenades = true;

        [Name("Bot Weight Effects")]
        [Description("Bots are properly affected by the weight of their equipment and loot. Requires raid restart for existing bots, as it applies on bot creation.")]
        public bool BotWeightEffects = true;

        [Name("Vanilla Bot Behavior Settings")]
        [Description("If a option here is set to ON, they will use vanilla logic, ALL Features will be disabled for these types, including personality, recoil, difficulty, and behavior.")]
        public VanillaBotSettings VanillaBots = new VanillaBotSettings();

        public PerformanceSettings Performance = new PerformanceSettings();

        public AILimitSettings AILimit = new AILimitSettings();

        public CoverSettings Cover = new CoverSettings();

        public DoorSettings Doors = new DoorSettings();

        public ExtractSettings Extract = new ExtractSettings();

        public FlashlightSettings Flashlight = new FlashlightSettings();

        [Name("Looting Bots Integration")]
        [Description("Modify settings that relate to Looting Bots. Requires Looting Bots to be installed.")]
        public LootingBotsSettings LootingBots = new LootingBotsSettings();

        public JokeSettings Jokes = new JokeSettings();

        public DebugSettings Debug = new DebugSettings();

        [Hidden]
        public LayerSettings Layers = new LayerSettings();

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
            list.Add(VanillaBots);
            list.Add(Performance);
            list.Add(AILimit);
            list.Add(Cover);
            list.Add(Doors);
            list.Add(Extract);
            list.Add(Flashlight);
            list.Add(LootingBots);
            list.Add(Jokes);
            list.Add(Debug);
            list.Add(Layers);
        }

        [JsonIgnore]
        [Hidden]
        public float SprintReachDistance = 1f;

        [JsonIgnore]
        [Hidden]
        public float BaseReachDistance = 0.5f;
    }

}