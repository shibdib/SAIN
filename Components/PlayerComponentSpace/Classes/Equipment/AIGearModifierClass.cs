using EFT.InventoryLogic;
using SAIN.SAINComponent.Classes.Info;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.Classes.Equipment
{
    public class AIGearModifierClass : AIDataBase
    {
        public AIGearModifierClass(SAINAIData sAINAIData) : base(sAINAIData) {

        }

        public float StealthModifier(float distance) {
            if (_nextGetSightModTime < Time.time)
            {
                _nextGetSightModTime = Time.time + 1f;
                _sightMod = getSightMod(distance);
            }
            return _sightMod;
        }


        private float getSightMod(float distance) {
            float gainVisionTime = 1f;
            if (distance > 50f)  {
                Item backpack = GearInfo.GetItem(EquipmentSlot.Backpack);
                if (backpack != null) {
                    switch (backpack.TemplateId)
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

                Item headwear = GearInfo.GetItem(EquipmentSlot.Headwear);
                if (headwear != null) {
                    switch (headwear.TemplateId)
                    {
                        case boonie_MILTEC:
                        case boonie_CHIMERA:
                        case boonie_DOORKICKER:
                        case boonie_JACK_PYKE:
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

                Item faceCover = GearInfo.GetItem(EquipmentSlot.FaceCover);
                if (faceCover != null) {
                    gainVisionTime *= 1.05f;
                }
            }
            return gainVisionTime;
        }

        private float _nextGetSightModTime;
        private float _sightMod;

        private const string backpack_pilgrim = "59e763f286f7742ee57895da";
        private const string backpack_raid = "5df8a4d786f77412672a1e3b";
        private const string boonie_MILTEC = "5b4327aa5acfc400175496e0";
        private const string boonie_CHIMERA = "60b52e5bc7d8103275739d67";
        private const string boonie_DOORKICKER = "5d96141523f0ea1b7f2aacab";
        private const string boonie_JACK_PYKE = "618aef6d0a5a59657e5f55ee";
        private const string helmet_TAN_ULACH = "5b40e2bc5acfc40016388216";
        private const string helmet_UNTAR_BLUE = "5aa7d03ae5b5b00016327db5";

        private Item _backpack => GearInfo.GetItem(EquipmentSlot.Backpack);
        private Item _headwear => GearInfo.GetItem(EquipmentSlot.Headwear);
        private Item _facecover => GearInfo.GetItem(EquipmentSlot.FaceCover);

    }
}