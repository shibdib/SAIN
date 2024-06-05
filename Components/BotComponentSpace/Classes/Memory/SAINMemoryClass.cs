using EFT;
using HarmonyLib;
using JetBrains.Annotations;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Enemy;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Memory
{
    public class SAINMemoryClass : SAINBase, ISAINClass
    {
        public readonly EnemyTargetsClass EnemyTargets = new EnemyTargetsClass();

        public SAINMemoryClass(BotComponent sain) : base(sain)
        {
            Health = new HealthTracker(sain);
            Location = new LocationTracker(sain);
        }

        public void Init()
        {
        }

        public void Update()
        {
            Health.Update();
            Location.Update();
            checkResetUnderFire();
        }

        private void checkVisiblePlayers()
        {
            if (_nextCheckVisPlayersTime < Time.time)
            {
                _nextCheckVisPlayersTime = Time.time + 10f;
                Logger.LogDebug($"Checking Visible {VisiblePlayers.Count} Players for {BotOwner.name}...");
                foreach (var player in VisiblePlayers)
                {
                    if (player != null)
                    {
                        Logger.LogDebug($"Visible for {BotOwner.name} = {player.name} : {player.Side} : {player.ProfileId} : Is Friendly? {Bot.EnemyController.IsPlayerFriendly(player)}");
                    }
                }
            }
        }

        private float _nextCheckVisPlayersTime;

        public Action<SAINEnemy> OnEnemyHeardFromPeace { get; set; }

        public void EnemyWasHeard(SAINEnemy enemy)
        {
            if (Bot.HasEnemy || !BotOwner.Memory.IsPeace)
            {
                return;
            }
            OnEnemyHeardFromPeace?.Invoke(enemy);
        }

        public void SetUnderFire(IPlayer source, Vector3 position)
        {
            if (source != null)
            {
                try
                {
                    BotOwner.Memory.SetUnderFire(source);
                }
                catch { }

                LastUnderFireSource = source;
            }
            UnderFireFromPosition = position;
        }

        private void checkResetUnderFire()
        {
            if (_nextCheckDeadTime < Time.time)
            {
                _nextCheckDeadTime = Time.time + 0.5f;

                if (BotOwner.Memory.IsUnderFire
                    && LastUnderFireSource != null
                    && !LastUnderFireSource.HealthController.IsAlive)
                {
                    if (_underFireTimeField == null)
                    {
                        _underFireTimeField = AccessTools.Field(typeof(BotMemoryClass), "float_4");
                    }
                    _underFireTimeField.SetValue(BotOwner.Memory, Time.time);
                }
            }
        }

        private float _nextCheckDeadTime;

        private static FieldInfo _underFireTimeField;

        public IPlayer LastUnderFireSource { get; private set; }

        public void Dispose()
        {
        }

        public readonly List<Player> VisiblePlayers = new List<Player>();

        public Vector3 UnderFireFromPosition { get; set; }

        //public DecisionWrapper Decisions { get; private set; }

        public SAINExtract Extract { get; private set; } = new SAINExtract();

        public HealthTracker Health { get; private set; }

        public LocationTracker Location { get; private set; }
    }
}