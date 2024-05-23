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

        public void AddMember(Bot member)
        {
            member.Decision.OnSAINStart += OnMemberSAINStart;
            member.Decision.OnSAINEnd += OnMemberSAINEnd;
        }

        public void RemoveMember(Bot member)
        {
            member.Decision.OnSAINStart -= OnMemberSAINStart;
            member.Decision.OnSAINEnd -= OnMemberSAINEnd;
        }

        public void Update()
        {
            SAINBotController.StartCoroutine(FindCoverForMembers());
        }

        private void OnMemberSAINStart(SoloDecision solo, SquadDecision squad, SelfDecision self, float time)
        {

        }

        private void OnMemberSAINEnd(float time)
        {

        }

        private Coroutine FindCoverCoroutine;

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
        public Dictionary<string, Bot> Members { get; private set; }
        public SAINBotController SAINBotController { get; private set; }
    }
}
