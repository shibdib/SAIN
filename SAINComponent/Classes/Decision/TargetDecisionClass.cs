using EFT;
using EFT.Hideout.ShootingRange;
using SAIN.Components;
using SAIN.SAINComponent;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class TargetDecisionClass : SAINBase, ISAINClass
    {
        public TargetDecisionClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public float FoundTargetTimer { get; private set; }

        public bool IgnorePlaceTarget { get; set; } = false;

        public bool GetDecision(out SoloDecision Decision)
        {
            Decision = SoloDecision.None;

            // This was previously "if (!BotOwner.Memory.GoalTarget.HaveMainTarget())", which returns (this.HavePlaceTarget() || this.HaveZeroTarget())
            // HaveZeroTarget() seems like a way to arbitrarily keep the bot in a combat state, so let's ignore it here
            if (!BotOwner.Memory.GoalTarget.HavePlaceTarget())
            {
                FoundTargetTimer = -1f;
                return false;
            }
            if (Bot.CurrentTargetPosition == null)
            {
                FoundTargetTimer = -1f;
                return false;
            }
            if (FoundTargetTimer < 0f)
            {
                FoundTargetTimer = Time.time;
            }

            if (IgnorePlaceTarget)
            {
                return false;
            }

            if (startSearch())
            {
                Decision = SoloDecision.Search;
            }
            else if (Bot.Decision.EnemyDecisions.shallHoldInCover())
            {
                Decision = SoloDecision.HoldInCover;
            }
            else if (BotOwner.Memory.IsUnderFire)
            {
                Decision = SoloDecision.RunToCover;
            }
            else
            {
                Decision = SoloDecision.MoveToCover;
            }

            return true;
        }

        private bool StartAmbush()
        {
            return false;
        }

        private bool StartInvestigate()
        {
            if (Time.time - FoundTargetTimer > 10f)
            {
                var sound = BotOwner.BotsGroup.YoungestPlace(BotOwner, 200f, true);
                if (sound != null)
                {
                    if (sound.IsDanger)
                    {
                        return true;
                    }
                    if (Bot.Info.Profile.IsPMC)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ShallNotSearch()
        {
            if (!Bot.Info.PersonalitySettings.Search.WillSearchFromAudio)
            {
                return false;
            }
            Vector3? target = Bot.CurrentTargetPosition;
            if (target != null && !Bot.Info.Profile.IsPMC && Bot.Memory.Location.BotZoneCollider != null)
            {
                Vector3 closestPointInZone = Bot.Memory.Location.BotZoneCollider.ClosestPointOnBounds(target.Value);
                float distance = (target.Value - closestPointInZone).magnitude;
                if (distance > 50f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool startSearch()
        {
            return Bot.Search.ShallStartSearch(out _, true);
        }
    }
}
