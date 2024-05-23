using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo
{
    public class ThrowGrenadeAction : SAINAction
    {
        public ThrowGrenadeAction(BotOwner bot) : base(bot, nameof(ThrowGrenadeAction))
        {
        }

        public override void Update()
        {
            if (!Stopped && Time.time - StartTime > 1f || SAINBot.Cover.CheckLimbsForCover())
            {
                Stopped = true;
                BotOwner.StopMove();
            }

            if (SAINBot.Squad.BotInGroup && SAINBot.Talk.GroupTalk.FriendIsClose)
            {
                SAINBot.Talk.Say(EPhraseTrigger.OnGrenade);
            }
        }

        private float StartTime = 0f;
        private bool Stopped = false;

        public override void Start()
        {
            StartTime = Time.time;
        }

        public override void Stop()
        {
        }
    }
}