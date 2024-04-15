using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.BaseClasses;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Mover;
using System;
using UnityEngine;

namespace SAIN.Components
{
    public class SAINMainPlayerComponent : MonoBehaviour
    {
        public void Awake()
        {
            if (SAINPersonComponent.TryAddSAINPersonToPlayer(MainPlayer, out var component))
            {
                SAINPersonComponent = component;
                SAINPerson = component.SAINPerson;
            }
            MainPlayerLight = MainPlayer.GetOrAddComponent<SAINFlashLightComponent>();
            CamoClass = new SAINCamoClass(this);
        }

        public SAINCamoClass CamoClass { get; private set; }
        public SAINPersonClass SAINPerson { get; private set; }
        public SAINPersonComponent SAINPersonComponent { get; private set; }
        public SAINFlashLightComponent MainPlayerLight { get; private set; }

        private void Start()
        {
            CamoClass.Start();
        }

        private void Update()
        {
            if (MainPlayer == null)
            {
                Dispose();
                return;
            }
            if (debugtimer < Time.time)
            {
                debugtimer = Time.time + 1f;
                float speedRatio = MainPlayer.MovementContext.ClampedSpeed / MainPlayer.MovementContext.MaxSpeed;
                //Logger.LogDebug(speedRatio.Round100());
            }
        }

        private float debugtimer;

        private void OnDestroy()
        {
            CamoClass.OnDestroy();
        }

        private void OnGUI()
        {
            //CamoClass.OnGUI();
        }

        public RaycastHit CurrentHit { get; private set; }
        public RaycastHit LastHit { get; private set; }

        private void CheckPlayerLook()
        {

        }

        private void Dispose()
        {
            try
            {
                ComponentHelpers.DestroyComponent(SAINPersonComponent);
                ComponentHelpers.DestroyComponent(MainPlayerLight);
                Destroy(this);
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose Component Error: [{e}]");
            }
        }

        public Player MainPlayer => Singleton<GameWorld>.Instance?.MainPlayer;
    }
}