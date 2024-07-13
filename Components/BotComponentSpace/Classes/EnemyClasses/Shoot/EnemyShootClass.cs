namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyShootClass : EnemyBase, IBotEnemyClass
    {
        public EnemyShootTargets Targets { get; }

        public EnemyShootClass(Enemy enemy) : base(enemy)
        {
            Targets = new EnemyShootTargets(enemy);
        }

        public void Init()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle += OnEnemyKnownChanged;
            SubscribeToDispose(Dispose);
            Targets.Init();
        }

        public void Update()
        {
            Targets.Update();
        }

        public void Dispose()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle -= OnEnemyKnownChanged;
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
        }
    }
}