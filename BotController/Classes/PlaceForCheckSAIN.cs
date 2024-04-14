using UnityEngine;

namespace SAIN.BotController.Classes
{
    public class PlaceForCheckSAIN
    {
        public PlaceForCheckSAIN(PlaceForCheck place, Vector3 originalPosition)
        {
            PlaceForCheck = place;
            OriginalPosition = originalPosition;
        }

        public PlaceForCheck PlaceForCheck { get; private set; }
        public Vector3 OriginalPosition { get; private set; }
    }
}
