using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.Personalities;
using System.Collections;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class EnemyTalk : SAINBase, ISAINClass
    {
        public EnemyTalk(BotComponent bot) : base(bot)
        {
            _randomizationFactor = Random.Range(0.75f, 1.25f);
        }

        public void Init()
        {
            UpdateSettings();
            PresetHandler.OnPresetUpdated += UpdateSettings;
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
                if (SAINBot?.Enemy != null)
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
        private float _randomizationFactor = 1f;

        public void Dispose()
        {
            PresetHandler.OnPresetUpdated -= UpdateSettings;
            if (Singleton<BotEventHandler>.Instance != null)
            {
                Singleton<BotEventHandler>.Instance.OnGrenadeExplosive -= tryFakeDeathGrenade;
            }
        }

        private PersonalityTalkSettings PersonalitySettings => SAINBot?.Info?.PersonalitySettings.Talk;
        private SAINSettingsClass FileSettings => SAINBot?.Info?.FileSettings;

        private float FakeDeathChance = 2f;

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
                _canRespondToEnemy = PersonalitySettings.CanRespondToEnemyVoice;

                var talkSettings = SAINPlugin.LoadedPreset.GlobalSettings.Talk;
                _friendlyResponseChance = talkSettings.FriendlyReponseChance;
                _friendlyResponseChanceAI = talkSettings.FriendlyReponseChanceAI;
                _friendlyResponseDistance = talkSettings.FriendlyReponseDistance;
                _friendlyResponseDistanceAI = talkSettings.FriendlyReponseDistanceAI;
                _friendlyResponseFrequencyLimit = talkSettings.FriendlyResponseFrequencyLimit;
                _friendlyResponseMinRandom = talkSettings.FriendlyResponseMinRandomDelay;
                _friendlyResponseMaxRandom = talkSettings.FriendlyResponseMaxRandomDelay;
            }
            else
            {
                Logger.LogAndNotifyError("Personality settings or filesettings are null! Cannot Apply Settings!");
            }
        }

        private bool CanTaunt = true;
        private bool CanFakeDeath = false;
        private bool CanBegForLife = false;
        private bool _canRespondToEnemy = true;
        private float TauntDist = 50f;
        private float TauntFreq = 5f;

        private bool ShallFakeDeath()
        {
            if (CanFakeDeath
                && EFTMath.RandomBool(FakeDeathChance)
                && SAINBot.Enemy != null
                && !SAINBot.Squad.BotInGroup
                && _fakeDeathTimer < Time.time
                && (SAINBot.Memory.Health.HealthStatus == ETagStatus.Dying || SAINBot.Memory.Health.HealthStatus == ETagStatus.BadlyInjured)
                && (SAINBot.Enemy.EnemyPosition - BotOwner.Position).sqrMagnitude < 70f * 70f)
            {
                _fakeDeathTimer = Time.time + 30f;
                SAINBot.Talk.Say(EPhraseTrigger.OnDeath);
                return true;
            }
            return false;
        }

        private void tryFakeDeathGrenade(Vector3 grenadeExplosionPosition, string playerProfileID, bool isSmoke, float smokeRadius, float smokeLifeTime)
        {
            if (CanFakeDeath
                && EFTMath.RandomBool(FakeDeathChance)
                && !isSmoke
                && SAINBot.Enemy != null
                && _fakeDeathTimer < Time.time
                && playerProfileID != SAINBot.ProfileId
                && (grenadeExplosionPosition - SAINBot.Position).sqrMagnitude < 25f * 25f)
            {
                _fakeDeathTimer = Time.time + 30f;
                SAINBot.Talk.Say(EPhraseTrigger.OnDeath);
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
            Vector3? currentTarget = SAINBot.CurrentTargetPosition;
            if (currentTarget == null)
            {
                _begTimer = Time.time + 10f;
                return false;
            }

            _begTimer = Time.time + 3f;

            bool shallBeg = SAINBot.Info.Profile.IsPMC ? !SAINBot.Squad.BotInGroup : SAINBot.Info.Profile.IsScav;
            if (shallBeg
                && CanBegForLife
                && SAINBot.Memory.Health.HealthStatus != ETagStatus.Healthy
                && (currentTarget.Value - SAINBot.Position).sqrMagnitude < 50f * 50f)
            {
                IsBeggingForLife = true;
                SAINBot.Talk.Say(BegPhrases.PickRandom());
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
            var enemy = SAINBot.Enemy;

            if (enemy != null
                && (enemy.EnemyPosition - SAINBot.Position).sqrMagnitude <= TauntDist * TauntDist)
            {
                if (SAINBot.Info.PersonalitySettings.Talk.ConstantTaunt)
                {
                    tauntEnemy = true;
                }
                else if (enemy.IsVisible || enemy.TimeSinceSeen < 5f)
                {
                    tauntEnemy = enemy.EnemyLookingAtMe || SAINBot.Info.PersonalitySettings.Talk.FrequentTaunt;
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
                    && EFTMath.RandomBool(20))
                {
                    SAINBot.Talk.Say(EPhraseTrigger.OnLostVisual, ETagStatus.Combat, true);
                }
                else
                {
                    EPhraseTrigger trigger = EFTMath.RandomBool(90) ? EPhraseTrigger.OnFight : EPhraseTrigger.BadWork;
                    SAINBot.Talk.Say(trigger, ETagStatus.Combat, false);
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
            if (sourcePlayer == null ||
                BotOwner == null ||
                Player == null ||
                SAINBot == null ||
                sourcePlayer.ProfileId == SAINBot.ProfileId)
            {
                yield break;
            }
            if ((sourcePlayer.Position - SAINBot.Position).sqrMagnitude > responseDist * responseDist)
            {
                //if (sourcePlayer.IsYourPlayer)
                //    Logger.LogInfo("No Response. Too far");
                yield break;
            }

            yield return new WaitForSeconds(delay);

            if (sourcePlayer == null ||
                BotOwner == null ||
                Player == null ||
                SAINBot == null)
            {
                yield break;
            }
            if (!sourcePlayer.HealthController.IsAlive
                || !Player.HealthController.IsAlive)
            {
                //if (sourcePlayer.IsYourPlayer)
                //    Logger.LogInfo("No Response. Player dead");
                yield break;
            }

            if (friendly && !BotOwner.Memory.IsPeace)
            {
                if (SAINBot.Talk.Say(trigger, null, false))
                {
                    //if (sourcePlayer.IsYourPlayer)
                    //    Logger.LogInfo("Response Done");
                }
                yield break;
            }

            if (friendly && _nextGestureTime < Time.time)
            {
                _nextGestureTime = Time.time + 3f;
                Player.HandsController.ShowGesture(EGesture.Hello);
            }
            if (SAINBot.Talk.Say(trigger, mask, false))
            {
                //if (sourcePlayer.IsYourPlayer)
                //    Logger.LogInfo("Response Done");
            }
        }

        public void SetEnemyTalk(Player player)
        {
            if ((player.Position - SAINBot.Position).sqrMagnitude < 70f.Sqr())
            {
                SAINBot.EnemyController.GetEnemy(player.ProfileId)?.SetHeardStatus(true, player.Position + UnityEngine.Random.onUnitSphere + Vector3.up, SAINSoundType.Conversation, true);
            }

            if (_canRespondToEnemy &&
                _nextResponseTime < Time.time)
            {
                float chance = 60;
                EPhraseTrigger trigger = EFTMath.RandomBool(90) ? EPhraseTrigger.OnFight : EPhraseTrigger.BadWork;

                //if (player.IsYourPlayer)
                //    Logger.LogInfo($"Starting Responding To Voice! Min Dist: {TauntDist} Chance: {chance}");

                _nextResponseTime = Time.time + 2f;
                SAINBot.StartCoroutine(RespondToVoice(
                    trigger,
                    ETagStatus.Combat,
                    Random.Range(0.4f, 0.6f),
                    player,
                    TauntDist,
                    chance
                    ));
            }
        }

        private float _nextGestureTime;

        private bool shallBeChatty()
        {
            if (SAINBot.Info.Profile.IsScav)
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeScavs;
            }
            if (SAINBot.Info.Profile.IsPMC)
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativePMCs;
            }
            var role = SAINBot.Info.Profile.WildSpawnType;
            if ((SAINBot.Info.Profile.IsBoss || SAINBot.Info.Profile.IsFollower))
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
                || SAINBot == null
                || !player.HealthController.IsAlive
                || SAINBot.ProfileId == player.ProfileId
                || (player.Position - SAINBot.Position).sqrMagnitude > 100f * 100f)
            {
                return;
            }

            if ((BotOwner.Memory.IsPeace || (SAINBot.Squad.HumanFriendClose && !player.IsAI))
                && _nextResponseTime < Time.time)
            {
                _nextResponseTime = Time.time + _friendlyResponseFrequencyLimit;

                if (player.IsAI == false || shallBeChatty())
                {
                    float maxDist = player.IsAI ? _friendlyResponseDistanceAI : _friendlyResponseDistance;
                    float chance = player.IsAI ? _friendlyResponseChanceAI : _friendlyResponseChance;

                    SAINBot.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.MumblePhrase,
                        SAINBot.EnemyController.NoEnemyContact ? ETagStatus.Unaware : ETagStatus.Combat,
                        Random.Range(_friendlyResponseMinRandom, _friendlyResponseMaxRandom),
                        player,
                        maxDist,
                        chance
                    ));
                }
            }
            else if (SAINBot?.Squad.SquadInfo != null
                && SAINBot.Talk.GroupTalk.FriendIsClose
                && (SAINBot.Squad.SquadInfo.SquadPersonality != BotController.Classes.ESquadPersonality.GigaChads
                    || SAINBot.Squad.SquadInfo.SquadPersonality != BotController.Classes.ESquadPersonality.Elite)
                && (SAINBot.Info.Personality == EPersonality.GigaChad
                    || SAINBot.Info.Personality == EPersonality.Chad))
            {
                if (_saySilenceTime < Time.time)
                {
                    _saySilenceTime = Time.time + 20f;
                    SAINBot.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.Silence,
                        SAINBot.EnemyController.NoEnemyContact ? ETagStatus.Combat : ETagStatus.Aware,
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