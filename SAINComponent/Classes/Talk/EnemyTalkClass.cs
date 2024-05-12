using EFT;
using Mono.Security.X509.Extensions;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.GlobalSettings.Categories;
using System.Collections;
using UnityEngine;
using static SAIN.Preset.Personalities.PersonalitySettingsClass;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class EnemyTalk : SAINBase, ISAINClass
    {
        public EnemyTalk(SAINComponentClass bot) : base(bot)
        {
        }

        public void Init()
        {
            _randomizationFactor = Random.Range(0.75f, 1.25f);
            UpdateSettings();
            PresetHandler.PresetsUpdated += UpdateSettings;
        }

        public void Update()
        {
            float time = Time.time;
            if (_nextCheckTime < time)
            {
                if (ShallBegForLife())
                {
                    _nextCheckTime = time + 1f;
                    return;
                }
                if (SAIN?.Enemy != null)
                {
                    if (ShallFakeDeath())
                    {
                        _nextCheckTime = time + 15f;
                        return;
                    }
                    if (CanTaunt 
                        && _tauntTimer < time 
                        && TauntEnemy())
                    {
                        _nextCheckTime = time + 1f;
                        _tauntTimer = time + TauntFreq * Random.Range(0.5f, 1.5f);
                        return;
                    }
                }
                _nextCheckTime = time + 0.25f;
            }
        }

        private float _nextCheckTime;
        private float _randomizationFactor;

        public void Dispose()
        {
            PresetHandler.PresetsUpdated -= UpdateSettings;
        }

        private PersonalityVariablesClass PersonalitySettings => SAIN?.Info?.PersonalitySettings;
        private SAINSettingsClass FileSettings => SAIN?.Info?.FileSettings;

        private void UpdateSettings()
        {
            if (PersonalitySettings != null && FileSettings != null)
            {
                CanFakeDeath = PersonalitySettings.CanFakeDeathRare;
                CanBegForLife = PersonalitySettings.CanBegForLife;
                CanTaunt = PersonalitySettings.CanTaunt && FileSettings.Mind.BotTaunts;
                TauntDist = PersonalitySettings.TauntMaxDistance * _randomizationFactor;
                TauntFreq = PersonalitySettings.TauntFrequency * _randomizationFactor;
                CanRespondToVoice = PersonalitySettings.CanRespondToVoice;
                ResponseDist = TauntDist;
            }
            else
            {
                Logger.LogAndNotifyError("Personality settings or filesettings are null!");
            }
        }

        private bool CanTaunt;
        private bool CanFakeDeath;
        private bool CanBegForLife;
        private float ResponseDist;
        private bool CanRespondToVoice;
        private float TauntDist;
        private float TauntFreq;

        private bool ShallFakeDeath()
        {
            if (CanFakeDeath
                && EFTMath.RandomBool(2f)
                && SAIN.Enemy != null
                && !SAIN.Squad.BotInGroup
                && _fakeDeathTimer < Time.time
                && (SAIN.Memory.HealthStatus == ETagStatus.Dying || SAIN.Memory.HealthStatus == ETagStatus.BadlyInjured)
                && (SAIN.Enemy.EnemyPosition - BotOwner.Position).sqrMagnitude < 50f * 50f)
            {
                _fakeDeathTimer = Time.time + 30f;
                SAIN.Talk.Say(EPhraseTrigger.OnDeath);
                return true;
            }
            return false;
        }

        private bool ShallBegForLife()
        {
            if (_begTimer > Time.time)
            {
                return false;
            }
            if (!EFTMath.RandomBool(25))
            {
                _begTimer = Time.time + 10f;
                return false;
            }
            Vector3? currentTarget = SAIN.CurrentTargetPosition;
            if (currentTarget == null)
            {
                _begTimer = Time.time + 10f;
                return false;
            }

            _begTimer = Time.time + 3f;

            bool shallBeg = SAIN.Info.Profile.IsPMC ? !SAIN.Squad.BotInGroup : SAIN.Info.Profile.IsScav;
            if (shallBeg
                && CanBegForLife
                && SAIN.Memory.HealthStatus != ETagStatus.Healthy
                && (currentTarget.Value - SAIN.Position).sqrMagnitude < 50f * 50f)
            {
                IsBeggingForLife = true;
                SAIN.Talk.Say(BegPhrases.PickRandom());
                return true;
            }
            return false;
        }

        public bool IsBeggingForLife 
        { 
            get 
            { 
                if (_isBegging && _beggingTimer < Time.time)
                {
                    _isBegging = false;
                }
                return _isBegging;
            }
            private set
            {
                if (value)
                {
                    _beggingTimer = Time.time + 60f;
                }
                _isBegging = value;
            }
        }

        private bool TauntEnemy()
        {
            bool tauntEnemy = false;
            var enemy = SAIN.Enemy;

            if (enemy != null 
                && (enemy.EnemyPosition - SAIN.Position).sqrMagnitude <= TauntDist * TauntDist)
            {
                if (SAIN.Info.PersonalitySettings.ConstantTaunt)
                {
                    tauntEnemy = true;
                }
                else if (enemy.CanShoot && enemy.IsVisible)
                {
                    tauntEnemy = enemy.EnemyLookingAtMe || SAIN.Info.PersonalitySettings.FrequentTaunt;
                }
            }

            if (!tauntEnemy && BotOwner.AimingData != null)
            {
                var aim = BotOwner.AimingData;
                if (aim != null && aim.IsReady)
                {
                    if (aim.LastDist2Target < TauntDist)
                    {
                        tauntEnemy = true;
                    }
                }
            }

            if (tauntEnemy)
            {
                if (enemy != null 
                    && !enemy.IsVisible 
                    && (enemy.Seen || enemy.Heard)
                    && enemy.TimeSinceSeen > 8f 
                    && EFTMath.RandomBool(33))
                {
                    SAIN.Talk.Say(EPhraseTrigger.OnLostVisual, ETagStatus.Combat, true);
                }
                else
                {
                    SAIN.Talk.Say(EPhraseTrigger.OnFight, ETagStatus.Combat, false);
                }
            }

            return tauntEnemy;
        }

        private IEnumerator RespondToVoice(EPhraseTrigger trigger, ETagStatus mask, float delay, Player sourcePlayer, float responseDist, float chance = 100f, bool friendly = false)
        {
            yield return new WaitForSeconds(delay);

            if (sourcePlayer == null || BotOwner == null || Player == null || SAIN == null)
            {
                yield break;
            }

            if (sourcePlayer.HealthController.IsAlive
                && Player.HealthController.IsAlive 
                && (sourcePlayer.Position - SAIN.Position).sqrMagnitude < responseDist * responseDist)
            {
                //Logger.LogInfo("Responding To Voice! Coroutine finish");
                if (friendly && !BotOwner.Memory.IsPeace)
                {
                    SAIN.Talk.Say(trigger, null, true);
                }
                else
                {
                    if (friendly && _nextGestureTime < Time.time)
                    {
                        _nextGestureTime = Time.time + 3f;
                        Player.HandsController.ShowGesture(EGesture.Hello);
                    }
                    SAIN.Talk.Say(trigger, mask, false);
                }
            }
        }

        public void SetEnemyTalk(Player player)
        {
            if (player == null
                || !player.HealthController.IsAlive
                || (player.Position - SAIN.Position).sqrMagnitude > 80f * 80f)
            {
                return;
            }
            if (SAIN != null 
                && player != null 
                && SAIN.ProfileId != player.ProfileId)
            {
                SAIN.EnemyController.GetEnemy(player.ProfileId)?.SetHeardStatus(true, player.Position, true);

                if (CanRespondToVoice
                    && _nextResponseTime < Time.time)
                {
                    //Logger.LogInfo("Responding To Voice!");
                    _nextResponseTime = Time.time + 0.5f;
                    SAIN.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.OnFight,
                        ETagStatus.Combat,
                        Random.Range(0.4f, 0.6f),
                        player,
                        ResponseDist,
                        60f
                        ));
                }
            }
        }

        private float _nextGestureTime;

        private bool shallBeChatty()
        {
            if (SAIN.Info.Profile.IsScav)
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeScavs;
            }
            if (SAIN.Info.Profile.IsPMC)
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativePMCs;
            }
            var role = SAIN.Info.Profile.WildSpawnType;
            if ((SAIN.Info.Profile.IsBoss || SAIN.Info.Profile.IsFollower))
            {
                if ((role == WildSpawnType.bossKnight || role == WildSpawnType.followerBirdEye || role == WildSpawnType.followerBigPipe))
                {
                    return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeGoons;
                }
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeBosses;
            }
            if ((role == WildSpawnType.pmcBot || role == WildSpawnType.exUsec))
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeRaidersRogues;
            }
            return false;
        }

        public void SetFriendlyTalked(Player player)
        {
            if (player == null || SAIN == null || !player.HealthController.IsAlive || (player.Position - SAIN.Position).sqrMagnitude > 80f * 80f || SAIN.ProfileId == player.ProfileId)
            {
                return;
            }

            if (BotOwner.Memory.IsPeace
                && _nextResponseTime < Time.time)
            {
                _nextResponseTime = Time.time + 0.75f;

                if (player.IsAI == false || shallBeChatty())
                {
                    SAIN.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.MumblePhrase,
                        ETagStatus.Unaware,
                        Random.Range(0.33f, 0.75f),
                        player,
                        _friendlyResponseDistance,
                        _friendlyResponseChance
                    ));
                }
            }
            else if (SAIN?.Squad.SquadInfo != null
                && SAIN.Talk.GroupTalk.FriendIsClose
                && (SAIN.Squad.SquadInfo.SquadPersonality != BotController.Classes.ESquadPersonality.GigaChads
                    || SAIN.Squad.SquadInfo.SquadPersonality != BotController.Classes.ESquadPersonality.Elite)
                && (SAIN.Info.Personality == EPersonality.GigaChad
                    || SAIN.Info.Personality == EPersonality.Chad))
            {
                if (_saySilenceTime < Time.time)
                {
                    _saySilenceTime = Time.time + 20f;
                    SAIN.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.Silence,
                        SAIN.HasEnemy ? ETagStatus.Combat : ETagStatus.Aware,
                        Random.Range(0.2f, 0.5f),
                        player,
                        20f,
                        33f
                        ));
                }
            }
        }

        private float _saySilenceTime;
        private float _beggingTimer;
        private bool _isBegging;
        private float _fakeDeathTimer = 0f;
        private float _begTimer = 0f;
        private static readonly EPhraseTrigger[] BegPhrases = 
        { 
            EPhraseTrigger.Stop, 
            EPhraseTrigger.HoldFire,
            EPhraseTrigger.GetBack
        };
        private const float _friendlyResponseDistance = 30f;
        private const float _friendlyResponseChance = 85f;
        private float _nextResponseTime;
        private float _tauntTimer = 0f;
    }
}