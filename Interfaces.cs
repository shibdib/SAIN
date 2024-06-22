using BepInEx.Logging;
using EFT;
using SAIN.Attributes;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
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
        void onEnemyForgotten(Enemy enemy);
        void onEnemyKnown(Enemy enemy);
    }
}
