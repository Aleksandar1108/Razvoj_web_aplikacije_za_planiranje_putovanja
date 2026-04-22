namespace DestinationsApi.Data.Entities;

/// <summary>
/// Samo za proveru vlasništva i datuma plana; mapira postojeću tabelu TravelPlans.
/// </summary>
public sealed class TravelPlanRowEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
