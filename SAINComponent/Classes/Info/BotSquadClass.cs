using BepInEx.Logging;
using EFT;
using SAIN.BotController.Classes;
using SAIN.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Info
{
    public class SAINSquadClass : SAINBase, ISAINClass
    {
        public SAINSquadClass(Bot sain) : base(sain)
        {
        }

        public void Init()
        {
            SquadInfo = SAINPlugin.BotController.BotSquads.GetSquad(SAINBot);
        }

        public Squad SquadInfo { get; private set; }

        public float DistanceToSquadLeader = 0f;

        public string SquadID => SquadInfo.Id;

        public readonly List<Bot> VisibleMembers = new List<Bot>();

        private float UpdateMembersTimer = 0f;

        public bool IAmLeader => SquadInfo.LeaderId == SAINBot.ProfileId;

        public Bot LeaderComponent => SquadInfo?.LeaderComponent;

        public bool BotInGroup => BotOwner.BotsGroup.MembersCount > 1;

        public Dictionary<string, Bot> Members => SquadInfo?.Members;

        public bool MemberIsFallingBack => SquadInfo?.MemberIsFallingBack == true;

        public void Update()
        {
            if (BotInGroup && SquadInfo != null && UpdateMembersTimer < Time.time)
            {
                UpdateMembersTimer = Time.time + 0.5f;

                UpdateVisibleMembers();

                if (LeaderComponent != null)
                {
                    DistanceToSquadLeader = (SAINBot.Position - LeaderComponent.Position).magnitude;
                }
            }
        }

        public void Dispose()
        {
        }

        private void UpdateVisibleMembers()
        {
            VisibleMembers.Clear();
            foreach (var member in Members.Values)
            {
                if (member != null && member.ProfileId != SAINBot.ProfileId && SAINBot.Memory.VisiblePlayers.Contains(member.Player))
                {
                    VisibleMembers.Add(member);
                }
            }
        }
    }
}