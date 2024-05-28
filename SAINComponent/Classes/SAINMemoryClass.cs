using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Enemy;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINMemoryClass : SAINBase, ISAINClass
    {
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
                        Logger.LogDebug($"Visible for {BotOwner.name} = {player.name} : {player.Side} : {player.ProfileId} : Is Friendly? {SAINBot.EnemyController.IsPlayerFriendly(player)}");
                    }
                }
            }
        }

        private float _nextCheckVisPlayersTime;

        public Action<SAINEnemy> OnEnemyHeardFromPeace { get; set; }

        public void EnemyWasHeard(SAINEnemy enemy)
        {
            if (SAINBot.HasEnemy || !BotOwner.Memory.IsPeace)
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

    public class SAINExtract
    {
        public Vector3? ExfilPosition { get; set; }
        public ExfiltrationPoint ExfilPoint { get; set; }
        public EExtractReason ExtractReason { get; set; }
        public EExtractStatus ExtractStatus { get; set; }
    }

    public class LocationTracker : SAINBase, ISAINClass
    {
        public Collider BotZoneCollider => BotZone?.Collider;
        public AIPlaceInfo BotZone => BotOwner.AIData.PlaceInfo;
        public bool IsIndoors { get; private set; }

        public LocationTracker(BotComponent sain) : base(sain)
        {
        }

        public void Init() { }

        public void Update()
        {
            if (_checkIndoorsTime < Time.time)
            {
                _checkIndoorsTime = Time.time + 0.2f;
                IsIndoors = Player.AIData.EnvironmentId != 0;
            }
        }

        public void Dispose() { }

        private float _checkIndoorsTime;
    }

    public class HealthTracker : SAINBase, ISAINClass
    {
        public Action<ETagStatus> HealthStatusChanged { get; set; }
        public bool Healthy => HealthStatus == ETagStatus.Healthy;
        public bool Injured => HealthStatus == ETagStatus.Injured;
        public bool BadlyInjured => HealthStatus == ETagStatus.BadlyInjured;
        public bool Dying => HealthStatus == ETagStatus.Dying;
        public ETagStatus HealthStatus { get; private set; }

        public HealthTracker(BotComponent sain) : base(sain)
        {
        }

        public void Init() { }

        public void Update()
        {
            if (_nextHealthUpdateTime < Time.time)
            {
                _nextHealthUpdateTime = Time.time + 0.5f;

                var oldStatus = HealthStatus;
                HealthStatus = Player.HealthStatus;
                if (HealthStatus != oldStatus)
                {
                    HealthStatusChanged?.Invoke(HealthStatus);
                }

                OnPainKillers = Player.MovementContext.PhysicalConditionIs(EPhysicalCondition.OnPainkillers);
            }
        }

        public bool OnPainKillers { get; private set; }

        public void Dispose() { }

        private float _nextHealthUpdateTime = 0f;

    }

    public enum EExtractReason
    {
        None = 0,
        Injured = 1,
        Time = 2,
        Loot = 3,
        External = 4,
    }

    public enum EExtractStatus
    {
        None = 0,
        MovingTo = 1,
        Fighting = 2,
        ExtractingNow = 3,
    }
}