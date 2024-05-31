using BepInEx.Logging;
using EFT;
using SAIN.BotController.Classes;
using SAIN.Components;
using SAIN.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Info
{
    public class SAINSquadClass : SAINBase, ISAINClass
    {
        public SAINSquadClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            SquadInfo = SAINPlugin.BotController.BotSquads.GetSquad(SAINBot);
        }

        public Squad SquadInfo { get; private set; }

        public float DistanceToSquadLeader = 0f;

        public string SquadID => SquadInfo.Id;

        public readonly List<BotComponent> VisibleMembers = new List<BotComponent>();

        private float UpdateMembersTimer = 0f;

        public bool IAmLeader => SquadInfo.LeaderId == SAINBot.ProfileId;

        public BotComponent LeaderComponent => SquadInfo?.LeaderComponent;

        public bool BotInGroup => BotOwner.BotsGroup.MembersCount > 1 || HumanFriendClose;

        public Dictionary<string, BotComponent> Members => SquadInfo?.Members;

        public bool MemberIsFallingBack => SquadInfo?.MemberIsFallingBack == true;

        public bool HumanFriendClose
        {
            get
            {
                if (_nextCheckhumantime < Time.time)
                {
                    _nextCheckhumantime = Time.time + 3f;
                    _humanFriendclose = humanFriendClose(50f.Sqr());
                }
                return _humanFriendclose;
            }
        }

        private bool _humanFriendclose;

        private float _nextCheckhumantime;

        private bool humanFriendClose(float distToCheck)
        {
            foreach (var player in EFTInfo.AlivePlayers)
            {
                if (player != null &&
                    !player.IsAI &&
                    SAINBot?.EnemyController?.IsPlayerFriendly(player) == true
                    && (player.Position - SAINBot.Position).sqrMagnitude < distToCheck)
                {
                    return true;
                }
            }
            return false;
        }

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
                if (member != null && 
                    member.ProfileId != SAINBot.ProfileId && 
                    SAINBot.Memory.VisiblePlayers.Contains(member.Player))
                {
                    VisibleMembers.Add(member);
                }
            }
        }
    }
}