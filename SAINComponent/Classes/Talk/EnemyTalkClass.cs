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
            _randomizationFactor = Random.Range(0.66f, 1.33f);
        }

        public void Init()
        {
            UpdateSettings();
            PresetHandler.PresetsUpdated += UpdateSettings;
        }

        public void Update()
        {
            if (PersonalitySettings == null || FileSettings == null)
            {
                return;
            }

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
                    if (CanTaunt && _tauntTimer < time)
                    {
                        _nextCheckTime = time + 1f;
                        _tauntTimer = time + TauntFreq * Random.Range(0.5f, 1.5f);
                        TauntEnemy();
                        return;
                    }
                }
                _nextCheckTime = time + 1f;
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
                SAIN.Talk.Say(EPhraseTrigger.OnFight, ETagStatus.Combat, true);
            }

            return tauntEnemy;
        }

        private IEnumerator RespondToVoice(EPhraseTrigger trigger, ETagStatus mask, float delay, Player sourcePlayer, float responseDist, float chance = 100f)
        {
            yield return new WaitForSeconds(delay);

            if (sourcePlayer?.HealthController?.IsAlive == true 
                && SAIN?.Player?.HealthController?.IsAlive == true 
                && (sourcePlayer.Position - SAIN.Position).sqrMagnitude < responseDist * responseDist)
            {
                if (mask == ETagStatus.Unaware && !BotOwner.Memory.IsPeace)
                {
                    yield break;
                }
                SAIN.Talk.Say(trigger, mask, true);
            }
        }

        public void SetEnemyTalk(Player player)
        {
            if (CanRespondToVoice 
                && SAIN != null 
                && player != null 
                && _nextResponseTime < Time.time 
                && SAIN.ProfileId != player.ProfileId)
            {
                _nextResponseTime = Time.time + 1f;

                SAIN.StartCoroutine(RespondToVoice(
                    EPhraseTrigger.OnFight, 
                    ETagStatus.Combat, 
                    Random.Range(0.33f, 0.75f), 
                    player,
                    ResponseDist, 
                    80f
                    ));
            }
        }

        public void SetFriendlyTalked(Player player)
        {
            if (CanRespondToVoice
                && SAIN != null
                && player != null 
                && BotOwner.Memory.IsPeace
                && _nextResponseTime < Time.time
                && SAIN.ProfileId != player.ProfileId)
            {
                _nextResponseTime = Time.time + 1f;

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
        private const float _friendlyResponseChance = 60f;
        private const float _friendlyResponseDistance = 40f;
        private float _nextResponseTime;
        private float _tauntTimer = 0f;
    }
}