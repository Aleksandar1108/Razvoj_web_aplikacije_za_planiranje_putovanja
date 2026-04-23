namespace ActivitiesApi.Data.Entities;

public sealed class TravelPlanRowEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
