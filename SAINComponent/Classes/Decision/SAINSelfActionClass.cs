using BepInEx.Logging;
using EFT;
using HarmonyLib;
using SAIN.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SAINSelfActionClass : SAINBase, ISAINClass
    {
        public SAINSelfActionClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            RefreshMeds();
        }

        public void Update()
        {
            if (!UsingMeds)
            {
                if (WasUsingMeds)
                {
                    RefreshMeds();
                }
                switch (SAIN.Memory.Decisions.Self.Current)
                {
                    case SelfDecision.Reload:
                        TryReload();
                        break;

                    case SelfDecision.Surgery:
                        DoSurgery();
                        break;

                    case SelfDecision.FirstAid:
                        DoFirstAid();
                        break;

                    case SelfDecision.Stims:
                        DoStims();
                        break;

                    default:
                        break;
                }
                WasUsingMeds = UsingMeds;
            }
        }

        private void RefreshMeds()
        {
            // According to BSG bots should only be able to look either in the secure container or EVERYWHERE ELSE, so I'm setting the toggle both ways and calling RefreshMeds to get both. If I understand that right.
            if (UseMedsOnlySafeContainerProp == null)
            {
                FirstAidField = AccessTools.Field(typeof(BotMedecine), "FirstAid");
                UseMedsOnlySafeContainerProp = AccessTools.Field(FirstAidField.FieldType, "bool_2");
            }

            BotOwner.Medecine.RefreshCurMeds();

            return;

            if (UseMedsOnlySafeContainerProp != null)
            {
                object value = UseMedsOnlySafeContainerProp.GetValue(BotOwner.Medecine.FirstAid);
                if (value != null && value is bool useSafeContainer)
                {
                    useSafeContainer = !useSafeContainer;
                    UseMedsOnlySafeContainerProp.SetValue(BotOwner.Medecine.FirstAid, useSafeContainer);

                    BotOwner.Medecine.RefreshCurMeds();
                }
            }
        }

        private static FieldInfo FirstAidField;
        private static FieldInfo UseMedsOnlySafeContainerProp;

        public void Dispose()
        {
        }


        private bool WasUsingMeds = false;

        private bool UsingMeds => BotOwner.Medecine?.Using == true;


        public void DoFirstAid()
        {
            var heal = BotOwner.Medecine.FirstAid;
            if (HealTimer < Time.time && heal.ShallStartUse())
            {
                HealTimer = Time.time + 5f;
                heal.TryApplyToCurrentPart();
            }
        }

        public void DoSurgery()
        {
            var surgery = BotOwner.Medecine.SurgicalKit;
            if (HealTimer < Time.time && surgery.ShallStartUse())
            {
                HealTimer = Time.time + 5f;
                surgery.ApplyToCurrentPart();
            }
        }

        public void DoStims()
        {
            var stims = BotOwner.Medecine.Stimulators;
            if (StimTimer < Time.time && stims.CanUseNow())
            {
                StimTimer = Time.time + 5f;
                try { stims.TryApply(); }
                catch { }
            }
        }

        public void TryReload()
        {
            try
            {
                BotOwner.WeaponManager.Reload.TryReload();
                if (BotOwner.WeaponManager.Reload.NoAmmoForReloadCached)
                {
                    //System.Console.WriteLine("NoAmmoForReloadCached");
                    BotOwner.WeaponManager.Reload.TryFillMagazines();
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        public void BotCancelReload()
        {
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                BotOwner.WeaponManager.Reload.TryStopReload();
            }
        }

        private float StimTimer = 0f;
        private float HealTimer = 0f;
    }
}
