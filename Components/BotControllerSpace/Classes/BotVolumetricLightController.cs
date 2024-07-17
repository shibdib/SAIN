using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAIN.Components.BotControllerSpace.Classes
{
    // attach component to each volumentric light,
    // track the angle if its a spot light, check range,
    // create sphere collider trigger for players with a size of the light's range,
    // check if those players who enter are within light's arc,
    // if so do a infrequent raycast to see if LOS is blocked on them or not,
    // depending on range of the light, a short raycast should be have minimal performance impact (will need to be profiled)
    // if not, the player is in a light!
    public class BotVolumetricLightController
    {
    }
}
