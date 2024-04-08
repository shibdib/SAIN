using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.BaseClasses;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Mover;
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
            CamoClass = new SAINCamoClass(this);
        }

        public SAINCamoClass CamoClass { get; private set; }
        public SAINPersonClass SAINPerson { get; private set; }
        public SAINPersonComponent SAINPersonComponent { get; private set; }

        private void Start()
        {
            CamoClass.Start();
        }

        private void Update()
        {
            if (MainPlayer == null)
            {
                Logger.LogError("MainPlayer Null");
            }
            if (SAINPerson == null)
            {
                Logger.LogError("SAINPerson Null");
            }
        }


        private void OnDestroy()
        {
            CamoClass.OnDestroy();
            try
            {
                ComponentHelpers.DestroyComponent(SAINPersonComponent);
            }
            catch
            {
                Logger.LogError("Dispose Component Error");
            }
        }

        private void OnGUI()
        {
            //CamoClass.OnGUI();
        }
        public Player MainPlayer => Singleton<GameWorld>.Instance?.MainPlayer;
    }
}