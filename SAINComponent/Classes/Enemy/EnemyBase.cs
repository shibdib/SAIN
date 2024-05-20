using EFT;
using SAIN.SAINComponent.BaseClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public abstract class EnemyBase
    {
        public EnemyBase(SAINEnemy enemy)
        {
            Enemy = enemy;
            SAIN = enemy.SAIN;
            BotOwner = enemy.SAIN.BotOwner;
            EnemyPerson = enemy.EnemyPerson;
            EnemyInfo = enemy.EnemyInfo;
            EnemyPlayer = enemy.EnemyPerson.Player;
            EnemyIPlayer = enemy.EnemyPerson.IPlayer;
            EnemyTransform = enemy.EnemyPerson.Transform;
        }

        public SAINEnemy Enemy { get; private set; }
        public SAINComponentClass SAIN { get; private set; }
        public EnemyInfo EnemyInfo { get; private set; }
        public Player EnemyPlayer { get; private set; }
        public IPlayer EnemyIPlayer { get; private set; }
        public BotOwner BotOwner { get; private set; }
        public SAINPersonClass EnemyPerson { get; private set; }
        public SAINPersonTransformClass EnemyTransform { get; private set; }
        public Vector3 EnemyPosition => EnemyTransform.Position;
        public Vector3 EnemyDirection => EnemyTransform.Direction(SAIN.Position);
        public bool IsCurrentEnemy => Enemy?.IsCurrentEnemy == true;
    }
}