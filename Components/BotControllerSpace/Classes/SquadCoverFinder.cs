using SAIN.BotController.Classes;
using SAIN.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class SquadCoverFinder
    {
        public SquadCoverFinder(Squad squad, SAINBotController botController)
        {
            Squad = squad;
            Members = squad.Members;
            SAINBotController = botController;
        }

        public void AddMember(BotComponent member)
        {
        }

        public void RemoveMember(BotComponent member)
        {
        }

        public void Update()
        {
            SAINBotController.StartCoroutine(FindCoverForMembers());
        }

        private IEnumerator FindCoverForMembers()
        {
            while (true)
            {

            }
        }

        public void Dispose()
        {
            
        }

        public Squad Squad { get; private set; }
        public Dictionary<string, BotComponent> Members { get; private set; }
        public SAINBotController SAINBotController { get; private set; }
    }
}
