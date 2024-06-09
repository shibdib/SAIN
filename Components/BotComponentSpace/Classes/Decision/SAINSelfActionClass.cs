﻿using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
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
        public SAINSelfActionClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        private float _handsBusyTimer;
        private float _nextCheckTime;

        public void Update()
        {
            if (!Bot.SAINLayersActive)
            {
                return;
            }
            if (!UsingMeds)
            {
                if (_nextCheckTime > Time.time)
                {
                    return;
                }
                _nextCheckTime = Time.time + 0.1f;

                if (Bot.Decision.CurrentSelfDecision == SelfDecision.Reload)
                {
                    TryReload();
                    return;
                }
                if (_handsBusyTimer < Time.time)
                {
                    var handsController = Player.HandsController;
                    if (handsController.IsInInteractionStrictCheck())
                    {
                        _handsBusyTimer = Time.time + 0.25f;
                        return;
                    }

                    bool didHeal = false;
                    bool didReload = false;

                    switch (Bot.Decision.CurrentSelfDecision)
                    {
                        case SelfDecision.Reload:
                            if (_healTime + 0.25f < Time.time)
                            {
                                didReload = TryReload();
                            }
                            break;

                        case SelfDecision.Surgery:
                            if (_healTime + 0.5f < Time.time)
                            {
                                //didHeal = DoSurgery();
                            }
                            break;

                        case SelfDecision.FirstAid:
                            if (_healTime + 0.5f < Time.time)
                            {
                                didHeal = DoFirstAid();
                            }
                            break;

                        case SelfDecision.Stims:
                            if (_healTime + 0.5f < Time.time)
                            {
                                didHeal = DoStims();
                            }
                            break;

                        default:
                            break;
                    }

                    if (didHeal || didReload)
                    {
                        _healTime = Time.time;
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        private bool UsingMeds => BotOwner.Medecine?.Using == true;

        public bool DoFirstAid()
        {
            var heal = BotOwner.Medecine?.FirstAid;
            if (heal == null)
            {
                return false;
            }
            if (_firstAidTimer < Time.time &&
                heal.ShallStartUse())
            {
                _firstAidTimer = Time.time + 5f;
                heal.TryApplyToCurrentPart();
                return true;
            }
            return false;
        }

        private float _firstAidTimer;

        public bool DoSurgery()
        {
            var surgery = BotOwner.Medecine?.SurgicalKit;
            if (surgery == null)
            {
                return false;
            }
            if (_trySurgeryTime < Time.time &&
                surgery.ShallStartUse())
            {
                _trySurgeryTime = Time.time + 5f;
                surgery.ApplyToCurrentPart();
                return true;
            }
            return false;
        }

        private float _trySurgeryTime;

        public bool DoStims()
        {
            var stims = BotOwner.Medecine?.Stimulators;
            if (stims == null)
            {
                return false;
            }
            if (_stimTimer < Time.time &&
                stims.CanUseNow())
            {
                _stimTimer = Time.time + 3f;
                try { stims.TryApply(); }
                catch { }
                return true;
            }
            return false;
        }

        private float _stimTimer;

        private bool HaveStimsToHelp()
        {
            return false;
        }

        public bool TryReload()
        {
            bool result = false;
            try
            {
                result = BotOwner.WeaponManager.Reload.TryReload();
                if (!result && 
                    checkRefillMags() && 
                    BotOwner.WeaponManager.Reload.TryReload())
                {
                    result = true;
                }
                if (!result)
                {
                    BotOwner.WeaponManager.Selector.TryChangeWeapon(true);
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            return result;
        }

        private bool checkRefillMags()
        {
            var weapon = BotOwner.WeaponManager.CurrentWeapon;
            var mag = weapon?.GetMagazineSlot();
            if (mag == null)
            {
                return false;
            }

            MagRefillClass refill = new MagRefillClass
            {
                magazineSlot = mag,
            };

            _preallocatedMagazineList.Clear();
            Player.GClass2761_0.GetReachableItemsOfTypeNonAlloc<MagazineClass>(_preallocatedMagazineList, new Func<MagazineClass, bool>(refill.canAccept));
            if (_preallocatedMagazineList.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < _preallocatedMagazineList.Count; i++)
            {
                MagazineClass magazineClass = _preallocatedMagazineList[i];
                if (magazineClass.Count < magazineClass.MaxCount)
                {
                    BotOwner.WeaponManager.Reload.method_2(weapon, magazineClass);
                }
            }
            _preallocatedMagazineList.Clear();
            return true;
        }

        private static readonly List<MagazineClass> _preallocatedMagazineList = new List<MagazineClass>();

        public void BotCancelReload()
        {
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                BotOwner.WeaponManager.Reload.TryStopReload();
            }
        }

        private float _healTime = 0f;
    }

    public class MagRefillClass
    {
        public bool canAccept(MagazineClass mag)
        {
            return this.magazineSlot.CanAccept(mag);
        }

        public Slot magazineSlot;
    }
}