using EFT.InventoryLogic;

namespace SAIN.Components.BotComponentSpace.Classes
{
    public class MagRefillClass
    {
        public bool canAccept(MagazineClass mag)
        {
            return this.magazineSlot.CanAccept(mag);
        }

        public Slot magazineSlot;
    }
}
