using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Utilities;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Info;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngineInternal;

namespace SAIN.SAINComponent.Classes.Talk
{
    public enum ISquadOrder
    {
        None = 0,
        HoldPosition = 1,
        Search = 2,
        Rush = 3,
        Suppress = 4,
        Help = 5,
    }
    public class GroupTalk : SAINBase, ISAINClass
    {
        public GroupTalk(SAINComponentClass bot) : base(bot)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (!SAIN.BotActive || SAIN.GameIsEnding || !SAIN.Talk.CanTalk || !BotSquad.BotInGroup)
            {
                if (Subscribed)
                {
                    Dispose();
                }
                return;
            }

            if (!SAIN.Info.FileSettings.Mind.SquadTalk)
            {
                return;
            }

            if (!Subscribed)
            {
                Subscribe();
            }

            if (TalkTimer < Time.time)
            {
                TalkTimer = Time.time + 1f;
                FriendIsClose = AreFriendsClose();
                if (FriendIsClose)
                {
                    if (ShallReportReloading())
                    {
                        return;
                    }
                    if (SAIN.Squad.IAmLeader 
                        && UpdateLeaderCommand())
                    {
                        return;
                    }
                    if (CheckEnemyContact())
                    {
                        return;
                    }
                    if (TalkHurt())
                    {
                        return;
                    }
                    if (ShallTalkRetreat())
                    {
                        return;
                    }
                    if (TalkEnemyLocation())
                    {
                        return;
                    }
                    if (ShallReportLostVisual())
                    {
                        return;
                    }
                    if (ShallReportEnemyHealth())
                    {
                        return;
                    }
                    if (ShallReportNeedHelp())
                    {
                        return;
                    }
                    if (HearNoise())
                    {
                        return;
                    }
                    if (TalkBotDecision(out var trigger, out _))
                    {
                        if (EFTMath.RandomBool(25))
                        {
                            SAIN.Talk.Say(trigger, null, false);
                        }
                    }
                }
            }
        }

        private bool ShallReportReloading()
        {
            if (_nextReportReloadTime < Time.time
                && SAIN.Memory.Decisions.Self.Current == SelfDecision.Reload)
            {
                _nextReportReloadTime = Time.time + 1f;
                return SAIN.Talk.GroupSay(reloadPhrases.PickRandom(), null, false, 33);
            }
            return false;
        }

        private readonly List<EPhraseTrigger> reloadPhrases = new List<EPhraseTrigger> { EPhraseTrigger.OnWeaponReload, EPhraseTrigger.NeedAmmo, EPhraseTrigger.OnOutOfAmmo };

        private float _nextReportReloadTime;

        private bool ShallReportLostVisual()
        {
            var enemy = SAIN.Enemy;
            if (enemy != null && enemy.Vision.ShallReportLostVisual)
            {
                enemy.Vision.ShallReportLostVisual = false;
                if (EFTMath.RandomBool(40))
                {
                    ETagStatus mask = PersonIsClose(enemy.EnemyPlayer) ? ETagStatus.Combat : ETagStatus.Aware;
                    if (enemy.TimeSinceSeen > 60 && EFTMath.RandomBool(33))
                    {
                        return SAIN.Talk.GroupSay(EPhraseTrigger.Rat, null, false, 33);
                    }
                    else
                    {
                        return SAIN.Talk.GroupSay(EPhraseTrigger.LostVisual, null, false, 100);
                    }
                }

            }
            return false;
        }

        private void EnemyConversation(EPhraseTrigger trigger, ETagStatus status, Player player)
        {
            if (player == null)
            {
                return;
            }
            if (SAIN.HasEnemy || !FriendIsClose)
            {
                return;
            }
            if (!BotOwner.BotsGroup.IsPlayerEnemy(player))
            {
                return;
            }
            if ((player.Position - SAIN.Position).sqrMagnitude > 50f * 50f)
            {
                return;
            }
            SAIN.EnemyController.GetEnemy(player.ProfileId)?.SetHeardStatus(true, player.Position, true);
            SAIN.Talk.GroupSay(EPhraseTrigger.OnEnemyConversation, null, false, 5);
        }

        public void TalkEnemySniper()
        {
            if (FriendIsClose)
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.SniperPhrase, ETagStatus.Combat, UnityEngine.Random.Range(0.5f, 1f));
            }
        }

        public void Dispose()
        {
            if (Subscribed)
            {
                SAIN.Squad.SquadInfo.MemberKilled -= OnFriendlyDown;
                SAINPlugin.BotController.PlayerTalk -= EnemyConversation;
                BotOwner.BotsGroup.OnReportEnemy -= OnAddEnemy;
                BotOwner.DeadBodyWork.OnStartLookToBody -= OnLootBody;
                BotOwner.BotsGroup.OnEnemyRemove -= OnEnemyDown;
                Subscribed = false;
            }
        }

        private void Subscribe()
        {
            if (!Subscribed)
            {
                Subscribed = true;

                SAIN.Squad.SquadInfo.MemberKilled += OnFriendlyDown;
                SAINPlugin.BotController.PlayerTalk += EnemyConversation;
                BotOwner.BotsGroup.OnReportEnemy += OnAddEnemy;
                BotOwner.DeadBodyWork.OnStartLookToBody += OnLootBody;
                BotOwner.BotsGroup.OnEnemyRemove += OnEnemyDown;
            }
        }

        private bool ShallReportEnemyHealth()
        {
            if (_nextCheckEnemyHPTime < Time.time && SAIN.Enemy != null)
            {
                _nextCheckEnemyHPTime = Time.time + 8f;
                if (EFTMath.RandomBool(40))
                {
                    ETagStatus health = SAIN.Enemy.EnemyPlayer.HealthStatus;
                    return SAIN.Enemy != null
                        && (health == ETagStatus.Dying || health == ETagStatus.BadlyInjured)
                        && SAIN.Talk.GroupSay(EPhraseTrigger.OnEnemyShot, null, false, 100);
                }
            }
            return false;
        }

        private float _nextCheckEnemyHPTime;

        private bool CheckEnemyContact()
        {
            SAINEnemy enemy = SAIN.Enemy;
            if (FriendIsClose 
                && enemy != null)
            {
                if (enemy.FirstContactOccured
                    && !enemy.FirstContactReported)
                {
                    enemy.FirstContactReported = true;
                    if (EFTMath.RandomBool(40))
                    {
                        ETagStatus mask = PersonIsClose(enemy.EnemyPlayer) ? ETagStatus.Combat : ETagStatus.Aware;
                        return SAIN.Talk.GroupSay(EPhraseTrigger.OnFirstContact, mask, true, 100);
                    }
                }
                if (enemy.Vision.ShallReportRepeatContact)
                {
                    enemy.Vision.ShallReportRepeatContact = false;
                    if (EFTMath.RandomBool(40))
                    {
                        ETagStatus mask = PersonIsClose(enemy.EnemyPlayer) ? ETagStatus.Combat : ETagStatus.Aware;
                        return SAIN.Talk.GroupSay(EPhraseTrigger.OnRepeatedContact, mask, false, 100);
                    }
                }
            }
            return false;
        }

        private void OnEnemyDown(IPlayer person)
        {
            AggressorStats aggressorStats = person?.Profile?.Stats?.Eft?.Aggressor;
            if (aggressorStats == null || aggressorStats.ProfileId != SAIN.ProfileId)
            {
                return;
            }

            var settings = person?.Profile?.Info?.Settings;
            if (settings == null || !BotOwner.BotsGroup.IsPlayerEnemyByRole(settings.Role))
            {
                return;
            }

            if (!FriendIsClose || !PersonIsClose(person))
            {
                return;
            }

            if (EFTMath.RandomBool(60))
            {
                float randomTime = UnityEngine.Random.Range(0.2f, 0.6f);
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.EnemyDown, ETagStatus.Aware, randomTime);

                var leader = SAIN.Squad.SquadInfo?.LeaderComponent;
                if (leader?.Person?.IPlayer != null 
                    && !SAIN.Squad.IAmLeader 
                    && EFTMath.RandomBool(60) 
                    && PersonIsClose(leader.Person.IPlayer))
                {
                    leader.Talk.TalkAfterDelay(EPhraseTrigger.GoodWork, ETagStatus.Aware, randomTime + 0.75f);
                }
            }
        }

        private bool PersonIsClose(IPlayer player)
        {
            return player != null && BotOwner != null && (player.Position - BotOwner.Position).magnitude < 30f;
        }

        private bool PersonIsClose(Player player)
        {
            return player != null && BotOwner != null && (player.Position - BotOwner.Position).magnitude < 30f;
        }

        public bool FriendIsClose;

        private const float LeaderFreq = 1f;
        private const float TalkFreq = 0.5f;
        private const float FriendTooFar = 30f;
        private const float FriendTooClose = 5f;
        private const float EnemyTooClose = 5f;

        private void OnFriendlyDown(IPlayer player, DamageInfo damage, float time)
        {
            if (BotOwner.IsDead || BotOwner.BotState != EBotState.Active)
            {
                return;
            }
            if (!FriendIsClose || !PersonIsClose(player))
            {
                return;
            }
            if (EFTMath.RandomBool(60))
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.OnFriendlyDown, ETagStatus.Combat, UnityEngine.Random.Range(0.33f, 0.66f));
            }
        }

        private void OnAddEnemy(IPlayer person, Vector3 enemyPos, Vector3 weaponRootLast, EEnemyPartVisibleType isVisibleOnlyBySense)
        {
            if (BotOwner.IsDead || BotOwner.BotState != EBotState.Active)
            {
                return;
            }
            if (!FriendIsClose)
            {
                return;
            }
        }

        private void OnLootBody(float num)
        {
            if (BotOwner.IsDead 
                || BotOwner.BotState != EBotState.Active 
                || SAIN.Enemy != null 
                || !FriendIsClose)
            {
                return;
            }

            EPhraseTrigger trigger = LootPhrases.PickRandom();
            SAIN.Talk.Say(trigger, null, true);
        }

        private readonly List<EPhraseTrigger> LootPhrases = new List<EPhraseTrigger> { EPhraseTrigger.LootBody, EPhraseTrigger.LootGeneric, EPhraseTrigger.OnLoot };

        private const float _friendCloseDist = 20f;

        private bool AreFriendsClose()
        {
            foreach (var member in SAIN.Squad.Members.Values)
            {
                if (member.Player != null 
                    && member.Player.ProfileId != Player.ProfileId 
                    && member.BotIsAlive 
                    && (member.Position - BotOwner.Position).sqrMagnitude < _friendCloseDist * _friendCloseDist)
                {
                    return true;
                }
            }
            return false;
        }

        private void AllMembersSay(EPhraseTrigger trigger, ETagStatus mask, float delay = 1.5f, float chance = 100f)
        {
            if (SAIN.Squad.LeaderComponent == null)
            {
                return;
            }

            bool memberTalked = false;
            foreach (var member in BotSquad.Members.Values)
            {
                if (member?.BotIsAlive == true 
                    && SAIN.Squad.LeaderComponent != null 
                    && !member.Squad.IAmLeader 
                    && member.Squad.DistanceToSquadLeader <= 30f)
                {
                    if (EFTMath.RandomBool(chance))
                    {
                        memberTalked = true;
                        member.Talk.TalkAfterDelay(trigger, mask, delay * UnityEngine.Random.Range(0.75f, 1.25f));
                    }
                }
            }

            if (memberTalked && EFTMath.RandomBool(5))
            {
                //SAIN.Squad.LeaderComponent?.Talk.TalkAfterDelay(EPhraseTrigger.Silence, ETagStatus.Aware, 1.25f);
            }
        }

        private bool UpdateLeaderCommand()
        {
            if (LeaderComponent != null)
            {
                if (BotSquad.IAmLeader && LeaderTimer < Time.time)
                {
                    LeaderTimer = Time.time + Randomized * SAIN.Info.FileSettings.Mind.SquadLeadTalkFreq;

                    if (!CheckIfLeaderShouldCommand())
                    {
                        if (CheckFriendliesTimer < Time.time 
                            && CheckFriendlyLocation(out var trigger))
                        {
                            CheckFriendliesTimer = Time.time + SAIN.Info.FileSettings.Mind.SquadLeadTalkFreq * 5f;

                            if (SAIN.Talk.Say(trigger))
                            {
                                AllMembersSay(EPhraseTrigger.Roger, ETagStatus.Aware, Random.Range(0.65f, 1.25f), 50f);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private float CheckFriendliesTimer = 0f;

        private bool TalkHurt()
        {
            if (HurtTalkTimer < Time.time)
            {
                var trigger = EPhraseTrigger.PhraseNone;
                HurtTalkTimer = Time.time + SAIN.Info.FileSettings.Mind.SquadMemberTalkFreq * 5f * Random.Range(0.66f, 1.33f);

                if (SAIN.HasEnemy && SAIN.Enemy.RealDistance < 25f)
                {
                    return false;
                }

                var health = SAIN.Memory.HealthStatus;
                switch (health)
                {
                    case ETagStatus.Injured:
                        if (EFTMath.RandomBool(60))
                        {
                            trigger = EFTMath.RandomBool() ? EPhraseTrigger.Hit : EPhraseTrigger.HurtLight;
                        }
                        break;

                    case ETagStatus.BadlyInjured:
                        if (EFTMath.RandomBool(75))
                        {
                            trigger = EFTMath.RandomBool() ? EPhraseTrigger.HurtLight : EPhraseTrigger.HurtHeavy;
                        }
                        break;

                    case ETagStatus.Dying:
                        if (EFTMath.RandomBool(75))
                        {
                            trigger = EPhraseTrigger.HurtNearDeath;
                        }
                        break;

                    default:
                        trigger = EPhraseTrigger.PhraseNone; 
                        break;
                }

                if (trigger != EPhraseTrigger.PhraseNone)
                {
                    return SAIN.Talk.Say(trigger);
                }
            }
            return false;
        }

        public bool TalkRetreat => SAIN.Enemy?.IsVisible == true && SAIN.Decision.RetreatDecisions.Contains(SAIN.Memory.Decisions.Main.Current);

        private bool ShallTalkRetreat()
        {
            if (_nextCheckTalkRetreatTime < Time.time && TalkRetreat)
            {
                _nextCheckTalkRetreatTime = Time.time + 10f;
                if (EFTMath.RandomBool(60))
                {
                    return SAIN.Talk.Say(EFTMath.RandomBool(30) ? EPhraseTrigger.NeedHelp : EPhraseTrigger.CoverMe, ETagStatus.Combat, true);
                }
            }
            return false;
        }

        private float _nextCheckTalkRetreatTime;

        private bool ShallReportNeedHelp()
        {
            if (SAIN.Enemy != null
                && BotOwner.Memory.IsUnderFire)
            {
                if (EFTMath.RandomBool(45))
                {
                    return SAIN.Talk.Say(EPhraseTrigger.NeedHelp, ETagStatus.Combat, true);
                }
            }
            return false;
        }

        private bool HearNoise()
        {
            if (SAIN.Enemy != null)
            {
                return false;
            }

            var hear = BotOwner.BotsGroup.YoungestPlace(BotOwner, 50f, true);

            if (hear != null)
            {
                if (hear.CheckingPlayer != null && hear.CheckingPlayer.ProfileId != BotOwner.ProfileId)
                {
                    return false;
                }
                if (!hear.IsDanger)
                {
                    if (hear.CreatedTime + 0.5f < Time.time && hear.CreatedTime + 1f > Time.time)
                    {
                        if (EFTMath.RandomBool(60))
                        {
                            SAIN.Talk.Say(EPhraseTrigger.NoisePhrase, ETagStatus.Aware, true);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool TalkBotDecision(out EPhraseTrigger trigger, out ETagStatus mask)
        {
            mask = ETagStatus.Combat;
            switch (SAIN.Memory.Decisions.Self.Current)
            {
                case SelfDecision.RunAway:
                    trigger = EPhraseTrigger.OnYourOwn;
                    break;

                case SelfDecision.FirstAid:
                case SelfDecision.Stims:
                case SelfDecision.Surgery:
                    trigger = EPhraseTrigger.StartHeal;
                    break;

                default:
                    trigger = EPhraseTrigger.PhraseNone;
                    break;
            }

            return trigger != EPhraseTrigger.PhraseNone;
        }

        public bool CheckIfLeaderShouldCommand()
        {
            if (CommandSayTimer < Time.time)
            {
                var mySquadDecision = SAIN.Memory.Decisions.Squad.Current;
                var myCurrentDecision = SAIN.Memory.Decisions.Main.Current;

                CommandSayTimer = Time.time + SAIN.Info.FileSettings.Mind.SquadLeadTalkFreq;
                var commandTrigger = EPhraseTrigger.PhraseNone;
                var trigger = EPhraseTrigger.PhraseNone;
                var gesture = EGesture.None;

                if (SAIN.Squad.SquadInfo?.MemberHasDecision(SoloDecision.RushEnemy) == true)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.Gogogo;
                    trigger = EPhraseTrigger.OnFight;
                }
                if (SAIN.Squad.SquadInfo?.MemberHasDecision(SquadDecision.Suppress) == true)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.Suppress;
                    trigger = EPhraseTrigger.Covering;
                }
                else if (mySquadDecision == SquadDecision.Search)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.FollowMe;
                    trigger = EPhraseTrigger.Going;
                }
                else if (SAIN.Squad.MemberIsFallingBack)
                {
                    gesture = EGesture.ComeToMe;
                    commandTrigger = EFTMath.RandomBool() ? EPhraseTrigger.GetInCover : EPhraseTrigger.GetBack;
                    trigger = EPhraseTrigger.PhraseNone;
                }
                else if (BotOwner.DoorOpener.Interacting && EFTMath.RandomBool(33f))
                {
                    commandTrigger = EPhraseTrigger.OpenDoor;
                    trigger = EPhraseTrigger.Roger;
                }
                else if (SAIN.Squad.SquadInfo?.MemberIsRegrouping == true)
                {
                    gesture = EGesture.ComeToMe;
                    commandTrigger = EPhraseTrigger.Regroup;
                    trigger = EPhraseTrigger.Roger;
                }
                else if (mySquadDecision == SquadDecision.Help)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.Gogogo;
                    trigger = EPhraseTrigger.Going;
                }
                else if (myCurrentDecision == SoloDecision.HoldInCover)
                {
                    gesture = EGesture.Stop;
                    commandTrigger = EPhraseTrigger.HoldPosition;
                    trigger = EPhraseTrigger.Roger;
                }
                else if (myCurrentDecision == SoloDecision.Retreat)
                {
                    commandTrigger = EPhraseTrigger.OnYourOwn;
                    trigger = EFTMath.RandomBool() ? EPhraseTrigger.Repeat : EPhraseTrigger.Stop;
                }

                if (commandTrigger != EPhraseTrigger.PhraseNone)
                {
                    if (gesture != EGesture.None && SAIN.Squad.VisibleMembers.Count > 0 && SAIN.Enemy?.IsVisible == false)
                    {
                        Player.HandsController.ShowGesture(gesture);
                    }
                    if (SAIN.Squad.VisibleMembers.Count / (float)SAIN.Squad.Members.Count < 0.5f)
                    {
                        SAIN.Talk.Say(commandTrigger);
                        AllMembersSay(trigger, ETagStatus.Aware, Random.Range(0.75f, 1.5f), 35f);
                    }
                    return true;
                }
            }

            return false;
        }

        private float EnemyPosTimer = 0f;

        public bool TalkEnemyLocation()
        {
            if (EnemyPosTimer < Time.time && SAIN.Enemy != null)
            {
                EnemyPosTimer = Time.time + 1f;
                var trigger = EPhraseTrigger.PhraseNone;
                var mask = ETagStatus.Aware;

                var enemy = SAIN.Enemy;
                if (SAIN.Enemy.IsVisible && enemy.EnemyLookingAtMe && EFTMath.RandomBool(40))
                {
                    mask = ETagStatus.Combat;
                    bool injured = !SAIN.Memory.Healthy && !SAIN.Memory.Injured;
                    trigger = injured ? EPhraseTrigger.NeedHelp : EPhraseTrigger.OnRepeatedContact;
                }
                else if (
                    (enemy.IsVisible 
                    || (enemy.Seen && enemy.TimeSinceSeen < 3f)) 
                    && EFTMath.RandomBool(60))
                {
                    EnemyDirectionCheck(enemy.EnemyPosition, out trigger, out mask);
                }

                if (trigger != EPhraseTrigger.PhraseNone)
                {
                    return SAIN.Talk.Say(trigger, mask, true);
                }
            }

            return false;
        }

        private bool EnemyDirectionCheck(Vector3 enemyPosition, out EPhraseTrigger trigger, out ETagStatus mask)
        {
            // Check Behind
            if (IsEnemyInDirection(enemyPosition, 180f, AngleToDot(75f)))
            {
                mask = ETagStatus.Aware;
                trigger = EPhraseTrigger.OnSix;
                return true;
            }

            // Check Left Flank
            if (IsEnemyInDirection(enemyPosition, -90f, AngleToDot(33f)))
            {
                mask = ETagStatus.Aware;
                trigger = EPhraseTrigger.LeftFlank;
                return true;
            }

            // Check Right Flank
            if (IsEnemyInDirection(enemyPosition, 90f, AngleToDot(33f)))
            {
                mask = ETagStatus.Aware;
                trigger = EPhraseTrigger.RightFlank;
                return true;
            }

            // Check Front
            if (IsEnemyInDirection(enemyPosition, 0f, AngleToDot(33f)))
            {
                mask = ETagStatus.Combat;
                trigger = EPhraseTrigger.InTheFront;
                return true;
            }

            trigger = EPhraseTrigger.PhraseNone;
            mask = ETagStatus.Unaware;
            return false;
        }

        private float AngleToRadians(float angle)
        {
            return (angle * (Mathf.PI)) / 180;
        }

        private float AngleToDot(float angle)
        {
            return Mathf.Cos(AngleToRadians(angle));
        }

        private bool CheckFriendlyLocation(out EPhraseTrigger trigger)
        {
            trigger = EPhraseTrigger.PhraseNone;
            if (SAIN.Squad.SquadInfo?.MemberIsRegrouping == true)
            {
                trigger = EPhraseTrigger.Regroup;
                return true;
            }
            return false;

            int tooClose = 0;
            int total = 0;

            foreach (var member in SAIN.Squad.Members.Values)
            {
                if (member == null) continue;

                total++;
                if ((member.Position - SAIN.Position).sqrMagnitude <= FriendTooClose * FriendTooClose)
                {
                    tooClose++;
                }
            }

            float tooCloseRatio = (float)tooClose / (float)total;

            if (tooCloseRatio > 0.5f)
            {
                trigger = EPhraseTrigger.Spreadout;
            }
            if (SAIN.Squad.SquadInfo?.MemberIsRegrouping == true)
            {
                trigger = EPhraseTrigger.Regroup;
            }

            return trigger != EPhraseTrigger.PhraseNone;
        }

        private bool IsEnemyInDirection(Vector3 enemyPosition, float angle, float threshold)
        {
            Vector3 enemyDirectionFromBot = enemyPosition - BotOwner.Transform.position;

            Vector3 enemyDirectionNormalized = enemyDirectionFromBot.normalized;
            Vector3 botLookDirectionNormalized = Player.MovementContext.PlayerRealForward.normalized;

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * botLookDirectionNormalized;

            return Vector3.Dot(enemyDirectionNormalized, direction) > threshold;
        }

        public SAINBotTalkClass LeaderComponent => SAIN.Squad.LeaderComponent?.Talk;
        private float Randomized => Random.Range(0.75f, 1.25f);
        private SAINSquadClass BotSquad => SAIN.Squad;

        private float CommandSayTimer = 0f;
        private float LeaderTimer = 0f;
        private float TalkTimer = 0f;
        private float HurtTalkTimer = 0f;
        private bool Subscribed = false;
    }
}