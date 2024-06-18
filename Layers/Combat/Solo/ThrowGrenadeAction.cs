using EFT;
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
            if (!Stopped && Time.time - StartTime > 1f || Bot.Cover.CheckLimbsForCover())
            {
                Stopped = true;
                BotOwner.StopMove();
            }

            if (Bot.Squad.BotInGroup && Bot.Talk.GroupTalk.FriendIsClose)
            {
                Bot.Talk.Say(EPhraseTrigger.OnGrenade);
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