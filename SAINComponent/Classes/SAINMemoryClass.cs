using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Decision;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINMemoryClass : SAINBase, ISAINClass
    {
        public SAINMemoryClass(SAINComponentClass sain) : base(sain)
        {
            Decisions = new DecisionWrapper(sain);
        }

        public Action<ETagStatus> HealthStatusChanged { get; set; }

        public void Init() 
        {
        }

        public void Update()
        {
            if (!SAIN.PatrolDataPaused)
            {
                return;
            }
            if (UpdateHealthTimer < Time.time)
            {
                UpdateHealthTimer = Time.time + 0.5f;

                var oldStatus = HealthStatus;
                HealthStatus = Player.HealthStatus;
                if (HealthStatus != oldStatus)
                {
                    HealthStatusChanged?.Invoke(HealthStatus);
                }
            }
            if (_checkIndoorsTime < Time.time)
            {
                _checkIndoorsTime = Time.time + 1f;
                IsIndoors = Physics.SphereCast(BotOwner.LookSensor._headPoint, 0.5f, Vector3.up, out _, 10f, LayerMaskClass.HighPolyWithTerrainMask);
            }

            checkResetUnderFire();
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
                    if (underFireTimeField == null)
                    {
                        underFireTimeField = AccessTools.Field(typeof(BotMemoryClass), "float_4");
                    }
                    underFireTimeField.SetValue(BotOwner.Memory, Time.time);
                }
            }

        }

        private float _nextCheckDeadTime;

        private static FieldInfo underFireTimeField;

        public IPlayer LastUnderFireSource { get; private set; }

        public void Dispose()
        {
        }

        public bool IsIndoors { get; private set; }
        private float _checkIndoorsTime;

        public Collider BotZoneCollider => BotZone?.Collider;
        public AIPlaceInfo BotZone => BotOwner.AIData.PlaceInfo;

        public List<Player> VisiblePlayers = new List<Player>();

        private float UpdateHealthTimer = 0f;

        public Vector3? ExfilPosition { get; set; }
        public ExfiltrationPoint ExfilPoint { get; set; }

        public bool Healthy => HealthStatus == ETagStatus.Healthy;
        public bool Injured => HealthStatus == ETagStatus.Injured;
        public bool BadlyInjured => HealthStatus == ETagStatus.BadlyInjured;
        public bool Dying => HealthStatus == ETagStatus.Dying;

        public ETagStatus HealthStatus { get; private set; }

        public Vector3 UnderFireFromPosition { get; set; }

        public DecisionWrapper Decisions { get; private set; }
    }
}