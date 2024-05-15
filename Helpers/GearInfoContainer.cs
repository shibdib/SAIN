using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Interpolation;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;
using static RootMotion.FinalIK.InteractionTrigger;

namespace SAIN
{
    public class GearInfoContainer
    {
        public GearInfoContainer(Player player)
        {
            Player = player;
            Slots.Add(EquipmentSlot.FirstPrimaryWeapon, null);
            Slots.Add(EquipmentSlot.SecondPrimaryWeapon, null);
            Slots.Add(EquipmentSlot.Holster, null);
        }

        public void ClearCache()
        {
            Weapons.Clear();
            Slots.Clear();
        }

        public void PlayAISound(float range, AISoundType soundType)
        {
            if (Player?.AIData != null 
                && _nextShootSoundTime < Time.time 
                && Singleton<BotEventHandler>.Instantiated)
            {
                float timeAdd = Player.AIData.IsAI ? 1f : 0.1f;
                _nextShootSoundTime = Time.time + timeAdd;
                Singleton<BotEventHandler>.Instance.PlaySound(Player, Player.WeaponRoot.position, range, soundType);
            }
        }

        private float _nextShootSoundTime;

        public void CheckForNewGear()
        {
            if (_nextUpdateTime < Time.time)
            {
                _nextUpdateTime = Time.time + 1f;
                var firearmController = Player.HandsController as FirearmController;
                GetWeaponInfo(firearmController?.Item);
                UpdateSlots();
            }
        }

        private float _nextUpdateTime;

        public void UpdateSlots()
        {
            var primary = Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.FirstPrimaryWeapon)?.ContainedItem;
            if (primary != null && primary is Weapon primaryWeapon)
            {
                Slots[EquipmentSlot.FirstPrimaryWeapon] = GetWeaponInfo(primaryWeapon);
            }

            var secondary = Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.SecondPrimaryWeapon)?.ContainedItem;
            if (secondary != null && secondary is Weapon secondaryWeapon)
            {
                Slots[EquipmentSlot.SecondPrimaryWeapon] = GetWeaponInfo(secondaryWeapon);
            }

            var holster = Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.Holster)?.ContainedItem;
            if (holster != null && holster is Weapon holsterWeapon)
            {
                Slots[EquipmentSlot.Holster] = GetWeaponInfo(holsterWeapon);
            }

            Backpack = Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.Backpack)?.ContainedItem;
            Headwear = Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.Headwear)?.ContainedItem;
            FaceCover = Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.FaceCover)?.ContainedItem;
        }

        public SAINWeaponInfo GetWeaponInfo(Weapon weapon)
        {
            if (weapon == null)
            {
                return null;
            }
            if (!Weapons.ContainsKey(weapon))
            {
                Weapons.Add(weapon, new SAINWeaponInfo(weapon));
            }
            return Weapons[weapon];
        }

        public readonly Player Player;
        public SAINWeaponInfo Primary => Slots[EquipmentSlot.FirstPrimaryWeapon];
        public SAINWeaponInfo Secondary => Slots[EquipmentSlot.SecondPrimaryWeapon];
        public SAINWeaponInfo Holster => Slots[EquipmentSlot.Holster];

        private float getSightMod(float distance)
        {
            float gainVisionTime = 1f;
            if (distance > 50f)
            {
                if (Backpack != null)
                {
                    switch (Backpack.TemplateId)
                    {
                        case backpack_pilgrim:
                            gainVisionTime *= 0.875f;
                            break;

                        case backpack_raid:
                            gainVisionTime *= 0.925f;
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    gainVisionTime *= 1.1f;
                }

                if (Headwear != null)
                {
                    switch (Headwear.TemplateId)
                    {
                        case boonie_MILTEC:
                        case boonie_CHIMERA:
                        case boonie_DOORKICKER:
                            gainVisionTime *= 1.2f;
                            break;

                        case helmet_TAN_ULACH:
                            gainVisionTime *= 0.925f;
                            break;

                        case helmet_UNTAR_BLUE:
                            gainVisionTime *= 0.9f;
                            break;

                        default:
                            break;
                    }
                }
                if (FaceCover != null)
                {
                    gainVisionTime *= 1.05f;
                }
            }
            return gainVisionTime;
        }

        public float GetGainSightModifierFromGear(float distance)
        {
            if (_nextGetSightModTime < Time.time)
            {
                _nextGetSightModTime = Time.time + 1f;
                _sightMod = getSightMod(distance);
            }
            return _sightMod;
        }

        private float _nextGetSightModTime;
        private float _sightMod;

        private const string backpack_pilgrim = "59e763f286f7742ee57895da";
        private const string backpack_raid = "5df8a4d786f77412672a1e3b";
        private const string boonie_MILTEC = "5b4327aa5acfc400175496e0";
        private const string boonie_CHIMERA = "60b52e5bc7d8103275739d67";
        private const string boonie_DOORKICKER = "5d96141523f0ea1b7f2aacab";
        private const string helmet_TAN_ULACH = "5b40e2bc5acfc40016388216";
        private const string helmet_UNTAR_BLUE = "5aa7d03ae5b5b00016327db5";

        public Item Backpack { get; private set; }
        public Item Headwear { get; private set; }
        public Item FaceCover { get; private set; }

        public Dictionary<Weapon, SAINWeaponInfo> Weapons = new Dictionary<Weapon, SAINWeaponInfo>();
        public Dictionary<EquipmentSlot, SAINWeaponInfo> Slots = new Dictionary<EquipmentSlot, SAINWeaponInfo>();
    }
}
