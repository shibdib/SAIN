using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GearStealthValues
{
    public class ItemStealthValue
    {
        //[JsonConstructor]
        //public ItemStealthValue()
        //{
        //}
        public string Name;
        public EEquipmentType EquipmentType;
        public string ItemID;
        public float StealthValue;
    }
}