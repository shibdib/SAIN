using EFT;
using EFT.Interactive;
using JetBrains.Annotations;
using SAIN.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static Class824;
using static RootMotion.FinalIK.InteractionTrigger.Range;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINBotRun : SAINBase, ISAINClass
    {
        public SAINBotRun(SAINComponentClass sain) : base (sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public bool RunToPoint(NavMeshPath path)
        {
            BotOwner.Mover.GoToByWay(path.corners, 0.75f);
            return true;
        }

        public void Dispose()
        {
        }
    }
}
