using Aki.Reflection.Patching;
using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.Layers;
using SAIN.Layers.Combat.Run;
using SAIN.Layers.Combat.Solo;
using SAIN.Layers.Combat.Squad;
using SAIN.Preset.GlobalSettings.Categories;
using System.Collections.Generic;
using System.Reflection;
using static EFT.SpeedTree.TreeWind;

namespace SAIN
{
    public class BigBrainHandler
    {
        public static bool IsBotUsingSAINLayer(BotOwner bot)
        {
            return isBotUsingLayer(bot, SAINLayers);
        }

        public static bool IsBotUsingSAINCombatLayer(BotOwner bot)
        {
            return isBotUsingLayer(bot, SAINCombatLayers);
        }

        private static bool isBotUsingLayer(BotOwner bot, List<string> layers)
        {
            return bot?.Brain?.Agent != null
                && BrainManager.IsCustomLayerActive(bot)
                && layers.Contains(bot.Brain.ActiveLayerName());
        }

        public static void Init()
        {
            if (!BigBrainInitialized)
            {
                BrainAssignment.Init();
                BigBrainInitialized = true;

            }
        }

        public static readonly List<string> SAINLayers = new List<string>
        {
            CombatSquadLayer.Name,
            ExtractLayer.Name,
            CombatSoloLayer.Name,
            BotUnstuckLayer.Name,
        };

        public static readonly List<string> SAINCombatLayers = new List<string>
        {
            CombatSquadLayer.Name,
            CombatSoloLayer.Name,
        };

        public static bool BigBrainInitialized;

        public class BrainAssignment
        {
            public static void Init()
            {
                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                List<Brain> brains = BotBrains.AllBrainsList;
                List<string> stringList = new List<string>();
                for (int i = 0; i < brains.Count; i++)
                {
                    var brain = brains[i].ToString();
                    if (goons.Contains(brain))
                    {
                        continue;
                    }
                    stringList.Add(brain);
                }

                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(BotRunLayer), stringList, 99);
                BrainManager.AddCustomLayer(typeof(SAINDogFightLayer), stringList, 80); 
                BrainManager.AddCustomLayer(typeof(ExtractLayer), stringList, settings.SAINExtractLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), stringList, settings.SAINCombatSquadLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), stringList, settings.SAINCombatSoloLayerPriority);

                assignBosses();

                BrainManager.RemoveLayers(LayersToRemove, stringList);
            }

            private static void assignBosses()
            {
                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;

                BrainManager.AddCustomLayer(typeof(BotRunLayer), goons, 99);
                BrainManager.AddCustomLayer(typeof(SAINDogFightLayer), goons, 80);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), goons, 64);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), goons, 62);
                BrainManager.RemoveLayers(LayersToRemove, goons);
            }

            private static readonly List<string> goons = new List<string>()
            {
                Brain.BigPipe.ToString(),
                Brain.BirdEye.ToString(),
                Brain.Knight.ToString()
            };

            private static void assignPMCs()
            {
            }

            private static void assignScavs()
            {
            }

            private static void assignOtherBots()
            {
            }

            public static readonly List<string> LayersToRemove = new List<string>
            {
                "Help",
                "AdvAssaultTarget",
                "Hit",
                "Simple Target",
                "Pmc",
                "AssaultHaveEnemy",
                "Request",
                "FightReqNull",
                "PeacecReqNull",
                "Assault Building",
                "Enemy Building",
                "KnightFight",
                "PtrlBirdEye"
            };
        }
    }
}