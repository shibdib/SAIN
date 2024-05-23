using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT;
using SAIN.Components;
using UnityEngine.AI;
using BepInEx.Logging;
using SAIN.SAINComponent;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class SquadLeaderClass : SAINBase
    {
        public SquadLeaderClass(Bot owner) : base(owner)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (!SAINBot.BotActive || SAINBot.GameIsEnding)
            {
                return;
            }

            if (!SAINBot.Squad.BotInGroup)
            {
                return;
            }
        }

        public void Dispose()
        {
        }
    }
}
