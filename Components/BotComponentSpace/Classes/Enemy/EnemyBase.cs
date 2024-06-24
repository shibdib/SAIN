using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public abstract class EnemyBase
    {
        public EnemyBase(Enemy enemy)
        {
            Enemy = enemy;
            EnemyPerson = enemy.EnemyPerson;
        }

        public virtual void OnEnemyForgotten(Enemy enemy)
        {

        }

        protected Enemy Enemy { get; private set; }
        protected BotComponent Bot => Enemy.Bot;
        protected PlayerComponent PlayerComp => Enemy.Bot.PlayerComponent;
        protected BotOwner BotOwner => Enemy.BotOwner;
        protected EnemyInfo EnemyInfo => Enemy.EnemyInfo;

        protected PlayerComponent EnemyPlayerComp => EnemyPerson.PlayerComponent;
        protected PersonClass EnemyPerson { get; private set; }
        protected Player EnemyPlayer => EnemyPerson.Player;
        protected IPlayer EnemyIPlayer => EnemyPerson.IPlayer;
        protected PersonTransformClass EnemyTransform => EnemyPerson.Transform;
        protected Vector3 EnemyPosition => EnemyPerson.Transform.Position;
        protected Vector3 EnemyDirection => EnemyPerson.Transform.DirectionTo(Bot.Position);
    }
}