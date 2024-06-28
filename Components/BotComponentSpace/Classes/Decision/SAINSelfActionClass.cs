using BepInEx.Logging;
using Comfort.Common;
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

        private float _reloadCallFreqLimit;

        public bool TryReload()
        {
            if (_reloadCallFreqLimit > Time.time)
            {
                return false;
            }
            _reloadCallFreqLimit = Time.time + 0.25f;

            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                return true;
            }

            if (BotOwner.ShootData.Shooting)
            {
                return false;
            }

            if (BotOwner.WeaponManager.Malfunctions.HaveMalfunction() && 
                BotOwner.WeaponManager.Malfunctions.MalfunctionType() != Weapon.EMalfunctionState.Misfire)
            {
                return false;
            }

            var magWeapon = Bot.Info.WeaponInfo.Reload.ActiveMagazineWeapon;
            if (magWeapon != null)
            {
                var currentMag = magWeapon.Weapon.GetCurrentMagazine();
                if (currentMag != null && currentMag.Count == currentMag.MaxCount)
                {
                    return false;
                }
                if (magWeapon.FullMagazineCount == 0)
                {
                    magWeapon.TryRefillMags(1);
                }
            }

            if (tryCatchReload())
            {
                magWeapon?.BotReloaded();
                return true;
            }

            if (magWeapon != null &&
                magWeapon.FullMagazineCount == 0 &&
                magWeapon.EmptyMagazineCount > 0 &&
                magWeapon.TryRefillAllMags() &&
                tryCatchReload())
            {
                magWeapon?.BotReloaded();
                return true;
            }

            if (!BotOwner.WeaponManager.Selector.TryChangeWeapon(true) && 
                BotOwner.WeaponManager.Selector.CanChangeToMeleeWeapons)
            {
                if (magWeapon != null &&
                    magWeapon.FullMagazineCount == 0 &&
                    magWeapon.PartialMagazineCount == 0)
                {
                    BotOwner.WeaponManager.Selector.ChangeToMelee();
                }
                if (magWeapon == null && 
                    BotOwner.WeaponManager.Reload.BulletCount == 0 && 
                    Bot.Enemy.RealDistance < 10f)
                {
                    BotOwner.WeaponManager.Selector.ChangeToMelee();
                }
            }

            return false;
        }

        private bool tryCatchReload()
        {
            bool result = false;
            try
            {
                var reload = BotOwner.WeaponManager.Reload;
                if (reload.CanReload(false, out var magazineClass, out var list))
                {
                    if (magazineClass != null)
                    {
                        reload.ReloadMagazine(magazineClass);
                        result = true;
                        reload.Reloading = true;
                    }
                    if (list != null && list.Count > 0)
                    {
                        reload.ReloadAmmo(list);
                        result = true;
                        reload.Reloading = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (SAINPlugin.DebugMode || true)
                {
                    Logger.LogError($"Error Trying to get Bot to reload: {ex}");
                }
            }
            return result;
        }

        public void BotCancelReload()
        {
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                BotOwner.WeaponManager.Reload.TryStopReload();
            }
        }

        private float _healTime = 0f;
    }
}