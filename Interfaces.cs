using BepInEx.Logging;
using EFT;
using SAIN.Attributes;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using System.Collections.Generic;

namespace SAIN
{
    public interface ISAINClass
    {
        void Init();
        void Update();
        void Dispose();
    }
}
