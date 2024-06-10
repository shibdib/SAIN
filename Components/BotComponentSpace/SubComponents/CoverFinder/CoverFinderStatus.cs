namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public enum CoverFinderStatus
    {
        None = 0,
        Idle = 1,
        SearchingColliders = 1,
        RecheckingPointsWithLimit = 2,
        RecheckingPointsNoLimit = 3,
    }
}