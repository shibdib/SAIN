using EFT;
using SAIN.Components.PlayerComponentSpace;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public abstract class EnemyBase
    {
        public EnemyBase(SAINEnemy enemy)
        {
            Enemy = enemy;
            EnemyPerson = enemy.EnemyPerson;
        }

        protected SAINEnemy Enemy { get; private set; }
        protected BotComponent Bot => Enemy.Bot;
        protected BotOwner BotOwner => Enemy.BotOwner;
        protected EnemyInfo EnemyInfo => Enemy.EnemyInfo;

        protected PersonClass EnemyPerson { get; private set; }
        protected Player EnemyPlayer => EnemyPerson.Player;
        protected IPlayer EnemyIPlayer => EnemyPerson.IPlayer;
        protected PersonTransformClass EnemyTransform => EnemyPerson.Transform;
        protected Vector3 EnemyPosition => EnemyPerson.Transform.Position;
        protected Vector3 EnemyDirection => EnemyPerson.Transform.DirectionTo(Bot.Position);
    }
}