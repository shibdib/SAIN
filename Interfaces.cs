using BepInEx.Logging;
using EFT;
using SAIN.Attributes;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Enemy;
using System.Collections.Generic;

namespace SAIN
{
    public interface ISAINClass
    {
        void Init();
        void Update();
        void Dispose();
    }

    public interface ISAINEnemyClass : ISAINClass
    {
        void onEnemyForgotten(SAINEnemy enemy);
        void onEnemyKnown(SAINEnemy enemy);
    }
}
