using BepInEx.Logging;
using EFT;
using Interpolation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Sense
{
    public class HeardSoundsClass : SAINBase, ISAINClass
    {
        public HeardSoundsClass(BotComponent sain) : base(sain)
        {
        }

        public Action<Sound> OnSoundHeard;
        public Action<Sound> OnGunshotHeard;

        public void Init()
        {
            SAINPlugin.BotController.AISoundPlayed += AddSound;
        }

        public void Update()
        {
            clearSounds(); 
            updateCache();
        }

        private void clearSounds()
        {
            if (_nextClearTime < Time.time)
            {
                _nextClearTime = Time.time + _clearFreq;
                HeardSounds.ClearOld();
                HeardGunshots.ClearOld();
            }
        }

        private float _nextClearTime;
        private const float _clearFreq = 1f;

        private void updateCache()
        {
            if (_nextUpdateCacheTime < Time.time)
            {
                _nextUpdateCacheTime = Time.time + _updateCacheFreq;

                Sound highestPower = null;
                foreach (Sound sound in _soundCache)
                {
                    if (highestPower == null || sound.Power > highestPower.Power)
                    {
                        highestPower = sound;
                    }
                }
                _soundCache.Clear();
                if (highestPower != null)
                {
                    SAINSoundType soundType = highestPower.SoundType;
                    if (soundType == SAINSoundType.Gunshot || soundType == SAINSoundType.SuppressedGunShot)
                    {
                        if (Bot.Hearing.EnemySoundHeard(
                            highestPower.SourcePlayer, 
                            highestPower.Position, 
                            highestPower.Power, 
                            soundType == SAINSoundType.Gunshot ? AISoundType.gun : AISoundType.silencedGun))
                        {
                            Logger.LogWarning("Heard Gunshot");
                            HeardGunshots.Add(highestPower);
                            OnGunshotHeard?.Invoke(highestPower);
                        }
                    }
                    else if (Bot.Hearing.EnemySoundHeard(
                            highestPower.SourcePlayer,
                            highestPower.Position,
                            highestPower.Power,
                            AISoundType.step))
                    {
                        Logger.LogWarning("Heard Sound");
                        HeardSounds.Add(highestPower);
                        OnSoundHeard?.Invoke(highestPower);
                    }
                }
            }
        }

        private float _nextUpdateCacheTime;
        private const float _updateCacheFreq = 0.1f;

        public void Dispose()
        {
            SAINPlugin.BotController.AISoundPlayed -= AddSound;
            HeardSounds.Clear();
        }

        public void AddSound(SAINSoundType soundType, Vector3 position, IPlayer sourcePlayer, float power)
        {
            if (Bot.GameIsEnding)
            {
                return;
            }
            if (sourcePlayer == null || sourcePlayer.Transform == null || sourcePlayer.ProfileId == Bot.ProfileId)
            {
                return;
            }
            if ((position - Bot.Position).sqrMagnitude > power * power)
            {
                return;
            }
            if (Bot.EnemyController.IsPlayerFriendly(sourcePlayer))
            {
                return;
            }
            _soundCache.Add(new Sound(position, power, sourcePlayer, soundType));
        }

        private readonly List<Sound> _soundCache = new List<Sound>();
        private readonly List<Sound> _playerSoundCache = new List<Sound>();

        public readonly SoundCollection HeardSounds = new SoundCollection(240f);
        public readonly SoundCollection HeardGunshots = new SoundCollection(600f);
    }

    public sealed class Sound
    {
        public Sound(Vector3 position, float power, IPlayer sourcePlayer, SAINSoundType soundType)
        {
            Position = position;
            Power = power;
            SourcePlayer = sourcePlayer;
            SoundType = soundType;
            TimeCreated = Time.time;
        }

        public readonly Vector3 Position;
        public readonly float Power;
        public readonly IPlayer SourcePlayer;
        public readonly SAINSoundType SoundType;
        public readonly float TimeCreated;

        public bool HaveLooked { get; set; }
        public bool HaveArrived { get; set; }

        public bool ShallClear(float expireTime)
        {
            return IsBad || Time.time - TimeCreated > expireTime;
        }

        public bool IsBad => SourcePlayer == null
                || SourcePlayer.Transform == null
                || SourcePlayer.HealthController.IsAlive == false;
    }

    public class SoundCollection : List<Sound>
    {
        public SoundCollection(float expireTime)
        {
            ExpireTime = expireTime;
        }

        public void ClearOld()
        {
            this.RemoveAll(x => x.ShallClear(ExpireTime));
        }

        public readonly float ExpireTime;
    }
}