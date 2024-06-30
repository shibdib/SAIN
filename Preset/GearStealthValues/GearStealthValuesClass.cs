using Newtonsoft.Json;
using SAIN.Helpers;
using System;
using System.Collections.Generic;

namespace SAIN.Preset.GearStealthValues
{
    public class GearStealthValuesClass
    {
        public Dictionary<EItemType, List<ItemStealthValue>> ItemStealthValues = new Dictionary<EItemType, List<ItemStealthValue>>();

        public GearStealthValuesClass(SAINPresetDefinition preset)
        {
            try
            {
                import(preset);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            initDefaults();
            Export(this, preset);
        }

        private void import(SAINPresetDefinition preset)
        {
            if (!preset.IsCustom)
            {
                return;
            }
            if (!JsonUtility.DoesFolderExist("Presets", preset.Name, "ItemStealthValues"))
            {
                return;
            }

            var list = new List<ItemStealthValue>();
            JsonUtility.Load.LoadStealthValues(list, "Presets", preset.Name, "ItemStealthValues");
            foreach (var type in EnumValues.GetEnum<EItemType>())
            {
                var itemList = getList(type);
                foreach (var item in list)
                {
                    if (item.ItemType != type)
                        continue;

                    Logger.LogDebug($"Adding {item.Name}");
                    addItem(item.Name, item.ItemType, item.ItemID, item.StealthValue, itemList);
                }
            }
        }

        public static void Export(GearStealthValuesClass stealthValues, SAINPresetDefinition preset)
        {
            if (!preset.IsCustom)
            {
                return;
            }

            JsonUtility.CreateFolder("Presets", preset.Name, "ItemStealthValues");
            JsonUtility.SaveObjectToJson(EnumValues.GetEnum<EItemType>(), "Possible Item Types For Stealth Modifiers", "Presets", preset.Name);

            foreach (var list in stealthValues.ItemStealthValues.Values)
            {
                foreach (var item in list)
                {
                    JsonUtility.SaveObjectToJson(item, item.Name, "Presets", preset.Name, "ItemStealthValues");
                }
            }
        }

        private void initDefaults()
        {
            var headWears = getList(EItemType.Headwear);
            addItem("MILTEC", EItemType.Headwear, boonie_MILTEC, 1.2f, headWears);
            addItem("CHIMERA", EItemType.Headwear, boonie_CHIMERA, 1.2f, headWears);
            addItem("DOORKICKER", EItemType.Headwear, boonie_DOORKICKER, 1.2f, headWears);
            addItem("JACK_PYKE", EItemType.Headwear, boonie_JACK_PYKE, 1.2f, headWears);
            addItem("TAN_ULACH", EItemType.Headwear, helmet_TAN_ULACH, 0.9f, headWears);
            addItem("UNTAR_BLUE", EItemType.Headwear, helmet_UNTAR_BLUE, 0.85f, headWears);

            var backPacks = getList(EItemType.BackPack);
            addItem("Pilgrim", EItemType.BackPack, backpack_pilgrim, 0.85f, backPacks);
            addItem("Raid", EItemType.BackPack, backpack_raid, 0.875f, backPacks);
        }

        private List<ItemStealthValue> getList(EItemType type)
        {
            if (!ItemStealthValues.TryGetValue(type, out var list))
            {
                list = new List<ItemStealthValue>();
                ItemStealthValues.Add(type, list);
            }
            return list;
        }

        private void addItem(string name, EItemType type, string id, float stealthValue, List<ItemStealthValue> list)
        {
            if (!doesItemExist(name, list))
            {
                list.Add(new ItemStealthValue
                {
                    Name = name,
                    ItemType = type,
                    ItemID = id,
                    StealthValue = stealthValue,
                });
            }
        }

        private bool doesItemExist(string name, List<ItemStealthValue> list)
        {
            foreach (var item in list)
            {
                if (item.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        private const string backpack_pilgrim = "59e763f286f7742ee57895da";
        private const string backpack_raid = "5df8a4d786f77412672a1e3b";
        private const string boonie_MILTEC = "5b4327aa5acfc400175496e0";
        private const string boonie_CHIMERA = "60b52e5bc7d8103275739d67";
        private const string boonie_DOORKICKER = "5d96141523f0ea1b7f2aacab";
        private const string boonie_JACK_PYKE = "618aef6d0a5a59657e5f55ee";
        private const string helmet_TAN_ULACH = "5b40e2bc5acfc40016388216";
        private const string helmet_UNTAR_BLUE = "5aa7d03ae5b5b00016327db5";
    }

    public class ItemStealthValue
    {
        [JsonConstructor]
        public ItemStealthValue()
        {
        }

        public string Name;
        public EItemType ItemType;
        public string ItemID;
        public float StealthValue;
    }

    public enum EItemType
    {
        Headwear,
        FaceCover,
        BackPack,
        EyeWear,
        ArmorVest,
        Rig,
    }
}