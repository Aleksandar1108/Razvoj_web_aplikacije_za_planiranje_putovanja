namespace CrossService.Access;

public enum TravelPlanAccessKind
{
    None = 0,
    Owner = 1,
    ShareView = 2,
    ShareEdit = 3,
    Admin = 4
}

public readonly record struct TravelPlanAccessResolution(TravelPlanAccessKind Kind)
{
    public bool IsAllowed => Kind != TravelPlanAccessKind.None;
    public bool CanMutate => Kind is TravelPlanAccessKind.Owner or TravelPlanAccessKind.ShareEdit or TravelPlanAccessKind.Admin;
    public bool IsAdminOverride => Kind == TravelPlanAccessKind.Admin;
}
