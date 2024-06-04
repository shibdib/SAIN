using Comfort.Common;
using EFT;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN
{
    public class GameWorldHandler
    {
        public static void Create(GameObject gameWorldObject)
        {
            if (SAINGameWorld != null)
            {
                Logger.LogWarning($"Old SAIN Gameworld is not null! Destroying...");
                SAINGameWorld.Dispose();
                GameObject.Destroy(SAINGameWorld);
            }
            SAINGameWorld = gameWorldObject.AddComponent<SAINGameworldComponent>();
        }

        public static SAINGameworldComponent SAINGameWorld { get; private set; }
        public static SAINBotController SAINBotController => SAINGameWorld?.SAINBotController;
        public static SAINMainPlayerComponent SAINMainPlayer => SAINGameWorld?.SAINMainPlayer;
    }
}
