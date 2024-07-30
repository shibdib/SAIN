namespace SAIN.SAINComponent.Classes.Mover
{
    public enum ESteerPriority
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