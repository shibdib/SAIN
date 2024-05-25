using Comfort.Common;
using EFT;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN
{
    public class GameWorldHandler
    {
        public static void Update()
        {
            InitSAINGameWorld();
            SAINGearInfoHandler.Update();
        }

        public static void InitSAINGameWorld()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null && SAINGameWorld != null)
            {
                Object.Destroy(SAINGameWorld);
            }
            else if (gameWorld != null && SAINGameWorld == null)
            {
                SAINGameWorld = gameWorld.GetOrAddComponent<SAINGameworldComponent>();
            }
            if (gameWorld == null)
            {
                SAINPlugin.ClearExcludedIDs();
            }
        }

        public static ELocation FindLocation()
        {
            if (!Singleton<GameWorld>.Instantiated || Singleton<GameWorld>.Instance == null)
            {
                return ELocation.None;
            }

            ELocation Location = ELocation.None;
            string locationString = Singleton<GameWorld>.Instance.LocationId;
            if (locationString.IsNullOrEmpty())
            {
                return ELocation.None;
            }
            switch (locationString.ToLower())
            {
                case "bigmap":
                    Location = ELocation.Customs;
                    break;
                case "factory4_day":
                    Location = ELocation.Factory;
                    break;
                case "factory4_night":
                    Location = ELocation.FactoryNight;
                    break;
                case "interchange":
                    Location = ELocation.Interchange;
                    break;
                case "laboratory":
                    Location = ELocation.Labs;
                    break;
                case "lighthouse":
                    Location = ELocation.Lighthouse;
                    break;
                case "rezervbase":
                    Location = ELocation.Reserve;
                    break;
                case "sandbox":
                    Location = ELocation.GroundZero;
                    break;
                case "shoreline":
                    Location = ELocation.Shoreline;
                    break;
                case "tarkovstreets":
                    Location = ELocation.Streets;
                    break;
                case "terminal":
                    Location = ELocation.Terminal;
                    break;
                case "town":
                    Location = ELocation.Town;
                    break;
                default:
                    Location = ELocation.None;
                    break;
            }
            return Location;
        }

        public static SAINGameworldComponent SAINGameWorld { get; private set; }
        public static SAINBotController SAINBotController => SAINGameWorld?.SAINBotController;
        public static SAINMainPlayerComponent SAINMainPlayer => SAINGameWorld?.SAINMainPlayer;
    }
}
