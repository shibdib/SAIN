namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public enum ECoverFailReason
    {
        None = 0,
        ColliderUsed = 1,
        ExcludedName = 2,
        NoPlaceToMove = 3,
        BadPosition = 4,
        BadPath = 5,
        NullOrBad = 6,
        Spotted = 7,
    }
}