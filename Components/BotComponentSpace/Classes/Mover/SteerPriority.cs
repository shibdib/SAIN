namespace SAIN.SAINComponent.Classes.Mover
{
    public enum SteerPriority
    {
        None,
        Shooting,
        ManualShooting,
        Aiming,
        EnemyVisible,
        HeardThreat,
        EnemyLastKnownLong,
        RandomLook,
        LastHit,
        UnderFire,
        MoveDirection,
        Sprinting,
        EnemyLastKnown,
        Search,
        RunningPath,
    }
}