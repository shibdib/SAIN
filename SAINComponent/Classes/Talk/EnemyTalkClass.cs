using Comfort.Common;
using EFT;
using Mono.Security.X509.Extensions;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.Preset.Personalities;
using System.Collections;
using UnityEngine;
using static SAIN.Preset.Personalities.PersonalitySettingsClass;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class EnemyTalk : SAINBase, ISAINClass
    {
        public EnemyTalk(Bot bot) : base(bot)
        {
        }

        public void Init()
        {
            _randomizationFactor = Random.Range(0.75f, 1.25f);
            UpdateSettings();
            PresetHandler.PresetsUpdated += UpdateSettings;
            if (Singleton<BotEventHandler>.Instance != null)
            {
                Singleton<BotEventHandler>.Instance.OnGrenadeExplosive += tryFakeDeathGrenade;
            }
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
                if (Bot?.Enemy != null)
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
            if (Singleton<BotEventHandler>.Instance != null )
            {
                Singleton<BotEventHandler>.Instance.OnGrenadeExplosive -= tryFakeDeathGrenade;
            }
        }

        private PersonalityVariablesClass PersonalitySettings => Bot?.Info?.PersonalitySettings;
        private SAINSettingsClass FileSettings => Bot?.Info?.FileSettings;

        float FakeDeathChance = 2f;

        private void UpdateSettings()
        {
            if (PersonalitySettings != null && FileSettings != null)
            {
                CanFakeDeath = PersonalitySettings.CanFakeDeathRare;
                FakeDeathChance = PersonalitySettings.FakeDeathChance;
                CanBegForLife = PersonalitySettings.CanBegForLife;
                CanTaunt = PersonalitySettings.CanTaunt && FileSettings.Mind.BotTaunts;
                TauntDist = PersonalitySettings.TauntMaxDistance * _randomizationFactor;
                TauntFreq = PersonalitySettings.TauntFrequency * _randomizationFactor;
                CanRespondToEnemyVoice = PersonalitySettings.CanRespondToEnemyVoice;
                EnemyResponseDist = TauntDist;

                _friendlyResponseChance = SAINPlugin.LoadedPreset.GlobalSettings.Talk.FriendlyReponseChance;
                _friendlyResponseChanceAI = SAINPlugin.LoadedPreset.GlobalSettings.Talk.FriendlyReponseChanceAI;
                _friendlyResponseDistance = SAINPlugin.LoadedPreset.GlobalSettings.Talk.FriendlyReponseDistance;
                _friendlyResponseDistanceAI = SAINPlugin.LoadedPreset.GlobalSettings.Talk.FriendlyReponseDistanceAI;
                _friendlyResponseFrequencyLimit = SAINPlugin.LoadedPreset.GlobalSettings.Talk.FriendlyResponseFrequencyLimit;
                _friendlyResponseMinRandom = SAINPlugin.LoadedPreset.GlobalSettings.Talk.FriendlyResponseMinRandomDelay;
                _friendlyResponseMaxRandom = SAINPlugin.LoadedPreset.GlobalSettings.Talk.FriendlyResponseMaxRandomDelay;
            }
            else
            {
                Logger.LogAndNotifyError("Personality settings or filesettings are null! Cannot Apply Settings!");
            }
        }

        private bool CanTaunt;
        private bool CanFakeDeath;
        private bool CanBegForLife;
        private float EnemyResponseDist;
        private bool CanRespondToEnemyVoice;
        private float TauntDist;
        private float TauntFreq;

        private bool ShallFakeDeath()
        {
            if (CanFakeDeath
                && EFTMath.RandomBool(FakeDeathChance)
                && Bot.Enemy != null
                && !Bot.Squad.BotInGroup
                && _fakeDeathTimer < Time.time
                && (Bot.Memory.Health.HealthStatus == ETagStatus.Dying || Bot.Memory.Health.HealthStatus == ETagStatus.BadlyInjured)
                && (Bot.Enemy.EnemyPosition - BotOwner.Position).sqrMagnitude < 70f * 70f)
            {
                _fakeDeathTimer = Time.time + 30f;
                Bot.Talk.Say(EPhraseTrigger.OnDeath);
                return true;
            }
            return false;
        }

        private void tryFakeDeathGrenade(Vector3 grenadeExplosionPosition, string playerProfileID, bool isSmoke, float smokeRadius, float smokeLifeTime)
        {
            if (CanFakeDeath
                && EFTMath.RandomBool(FakeDeathChance)
                && !isSmoke
                && Bot.Enemy != null
                && _fakeDeathTimer < Time.time 
                && playerProfileID != Bot.ProfileId
                && (grenadeExplosionPosition - Bot.Position).sqrMagnitude < 25f * 25f)
            {
                _fakeDeathTimer = Time.time + 30f;
                Bot.Talk.Say(EPhraseTrigger.OnDeath);
            }
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
            Vector3? currentTarget = Bot.CurrentTargetPosition;
            if (currentTarget == null)
            {
                _begTimer = Time.time + 10f;
                return false;
            }

            _begTimer = Time.time + 3f;

            bool shallBeg = Bot.Info.Profile.IsPMC ? !Bot.Squad.BotInGroup : Bot.Info.Profile.IsScav;
            if (shallBeg
                && CanBegForLife
                && Bot.Memory.Health.HealthStatus != ETagStatus.Healthy
                && (currentTarget.Value - Bot.Position).sqrMagnitude < 50f * 50f)
            {
                IsBeggingForLife = true;
                Bot.Talk.Say(BegPhrases.PickRandom());
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
            var enemy = Bot.Enemy;

            if (enemy != null 
                && (enemy.EnemyPosition - Bot.Position).sqrMagnitude <= TauntDist * TauntDist)
            {
                if (Bot.Info.PersonalitySettings.ConstantTaunt)
                {
                    tauntEnemy = true;
                }
                else if (enemy.CanShoot && enemy.IsVisible)
                {
                    tauntEnemy = enemy.EnemyLookingAtMe || Bot.Info.PersonalitySettings.FrequentTaunt;
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
                    Bot.Talk.Say(EPhraseTrigger.OnLostVisual, ETagStatus.Combat, true);
                }
                else
                {
                    Bot.Talk.Say(EPhraseTrigger.OnFight, ETagStatus.Combat, false);
                }
            }

            return tauntEnemy;
        }

        private IEnumerator RespondToVoice(EPhraseTrigger trigger, ETagStatus mask, float delay, Player sourcePlayer, float responseDist, float chance = 100f, bool friendly = false)
        {
            if (!EFTMath.RandomBool(chance))
            {
                yield break;
            }
            if (sourcePlayer == null || BotOwner == null || Player == null || Bot == null || sourcePlayer.ProfileId == Bot.ProfileId)
            {
                yield break;
            }
            if (!sourcePlayer.HealthController.IsAlive
                || !Player.HealthController.IsAlive
                || (sourcePlayer.Position - Bot.Position).sqrMagnitude > responseDist * responseDist)
            {
                yield break;
            }

            yield return new WaitForSeconds(delay);

            if (sourcePlayer == null || BotOwner == null || Player == null || Bot == null)
            {
                yield break;
            }

            if (friendly && !BotOwner.Memory.IsPeace)
            {
                Bot.Talk.Say(trigger, null, true);
            }
            else
            {
                if (friendly && _nextGestureTime < Time.time)
                {
                    _nextGestureTime = Time.time + 3f;
                    Player.HandsController.ShowGesture(EGesture.Hello);
                }
                Bot.Talk.Say(trigger, mask, false);
            }
        }

        public void SetEnemyTalk(Player player)
        {
            if (player == null
                || !player.HealthController.IsAlive
                || (player.Position - Bot.Position).sqrMagnitude > 100f * 100f)
            {
                return;
            }

            if (Bot != null 
                && player != null 
                && Bot.ProfileId != player.ProfileId)
            {
                Bot.EnemyController.GetEnemy(player.ProfileId)?.SetHeardStatus(true, player.Position + UnityEngine.Random.onUnitSphere + Vector3.up, SAINSoundType.Conversation);

                if (CanRespondToEnemyVoice
                    && _nextResponseTime < Time.time)
                {
                    //Logger.LogInfo("Responding To Voice!");
                    _nextResponseTime = Time.time + 1f;
                    Bot.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.OnFight,
                        ETagStatus.Combat,
                        Random.Range(0.4f, 0.6f),
                        player,
                        EnemyResponseDist,
                        60f
                        ));
                }
            }
        }

        private float _nextGestureTime;

        private bool shallBeChatty()
        {
            if (Bot.Info.Profile.IsScav)
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeScavs;
            }
            if (Bot.Info.Profile.IsPMC)
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativePMCs;
            }
            var role = Bot.Info.Profile.WildSpawnType;
            if ((Bot.Info.Profile.IsBoss || Bot.Info.Profile.IsFollower))
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
            if (player == null 
                || Bot == null 
                || !player.HealthController.IsAlive 
                || (player.Position - Bot.Position).sqrMagnitude > 100f * 100f 
                || Bot.ProfileId == player.ProfileId)
            {
                return;
            }

            if (BotOwner.Memory.IsPeace
                && _nextResponseTime < Time.time)
            {
                _nextResponseTime = Time.time + _friendlyResponseFrequencyLimit;

                if (player.IsAI == false || shallBeChatty())
                {
                    float maxDist = player.IsAI ? _friendlyResponseDistanceAI : _friendlyResponseDistance;
                    float chance = player.IsAI ? _friendlyResponseChanceAI : _friendlyResponseChance;

                    Bot.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.MumblePhrase,
                        ETagStatus.Unaware,
                        Random.Range(_friendlyResponseMinRandom, _friendlyResponseMaxRandom),
                        player,
                        maxDist,
                        chance
                    ));
                }
            }
            else if (Bot?.Squad.SquadInfo != null
                && Bot.Talk.GroupTalk.FriendIsClose
                && (Bot.Squad.SquadInfo.SquadPersonality != BotController.Classes.ESquadPersonality.GigaChads
                    || Bot.Squad.SquadInfo.SquadPersonality != BotController.Classes.ESquadPersonality.Elite)
                && (Bot.Info.Personality == EPersonality.GigaChad
                    || Bot.Info.Personality == EPersonality.Chad))
            {
                if (_saySilenceTime < Time.time)
                {
                    _saySilenceTime = Time.time + 20f;
                    Bot.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.Silence,
                        Bot.HasEnemy ? ETagStatus.Combat : ETagStatus.Aware,
                        Random.Range(0.2f, 0.5f),
                        player,
                        20f,
                        33f
                        ));
                }
            }
        }

        private float _friendlyResponseFrequencyLimit = 1f;
        private float _friendlyResponseMinRandom = 0.33f;
        private float _friendlyResponseMaxRandom = 0.75f;
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

        private float _friendlyResponseDistance = 60f;
        private float _friendlyResponseDistanceAI = 35f;
        private float _friendlyResponseChance = 85f;
        private float _friendlyResponseChanceAI = 80f;
        private float _nextResponseTime;
        private float _tauntTimer = 0f;
    }
}