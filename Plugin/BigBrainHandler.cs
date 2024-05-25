using Aki.Reflection.Patching;
using Comfort.Common;
using Dissonance;
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
            SAINAvoidThreatLayer.Name,
        };
        public static readonly List<string> SAINCombatLayers = new List<string>
        {
            CombatSquadLayer.Name,
            CombatSoloLayer.Name,
            SAINAvoidThreatLayer.Name,
        };

        public static bool BigBrainInitialized;

        public class BrainAssignment
        {
            public static void Init()
            {
                handlePMCS();
                handleScavs();
                handleBosses();
                handleFollowers();
                handleOthers();
                handleGoons();
            }

            private static void handlePMCS()
            {
                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                List<string> pmcBrain = new List<string>();
                pmcBrain.Add(Brain.PMC.ToString());

                BrainManager.AddCustomLayer(typeof(BotRunLayer), pmcBrain, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), pmcBrain, 80);
                BrainManager.AddCustomLayer(typeof(ExtractLayer), pmcBrain, settings.SAINExtractLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), pmcBrain, settings.SAINCombatSquadLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), pmcBrain, settings.SAINCombatSoloLayerPriority); 

                List<string> LayersToRemove = new List<string>
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
                BrainManager.RemoveLayers(LayersToRemove, pmcBrain);
            }

            private static void handleScavs()
            {
                if (SAINPlugin.LoadedPreset.GlobalSettings.General.VanillaScavs)
                {
                    return;
                }

                List<string> brainList = getBrainList(AIBrains.Scavs);
                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;

                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(BotRunLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(ExtractLayer), brainList, settings.SAINExtractLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, settings.SAINCombatSquadLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, settings.SAINCombatSoloLayerPriority);

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "PtrlBirdEye"
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static List<string> getBrainList(List<Brain> brains)
            {
                List<string> brainList = new List<string>();
                for (int i = 0; i < brains.Count; i++)
                {
                    brainList.Add(brains[i].ToString());
                }
                return brainList;
            }

            private static void handleOthers()
            {
                List<string> brainList = getBrainList(AIBrains.Others);

                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(BotRunLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(ExtractLayer), brainList, settings.SAINExtractLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, settings.SAINCombatSquadLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, settings.SAINCombatSoloLayerPriority);

                List<string> LayersToRemove = new List<string>
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
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static void handleBosses()
            {
                if (SAINPlugin.LoadedPreset.GlobalSettings.General.VanillaBosses)
                {
                    return;
                }

                List<string> brainList = getBrainList(AIBrains.Bosses);

                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(BotRunLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, 70);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, 69);

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "PtrlBirdEye",
                    "BossBoarFight"
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static void handleFollowers()
            {
                if (SAINPlugin.LoadedPreset.GlobalSettings.General.VanillaBosses)
                {
                    return;
                }
                List<string> brainList = getBrainList(AIBrains.Followers);

                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(BotRunLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, 70);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, 69);

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "PtrlBirdEye",
                    "BoarGrenadeDanger"
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static void handleGoons()
            {
                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General; 
                List<string> brainList = getBrainList(AIBrains.Goons);

                BrainManager.AddCustomLayer(typeof(BotRunLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, 64);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, 62); 

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    //"FightReqNull",
                    //"PeacecReqNull",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "PtrlBirdEye"
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }
        }
    }
}