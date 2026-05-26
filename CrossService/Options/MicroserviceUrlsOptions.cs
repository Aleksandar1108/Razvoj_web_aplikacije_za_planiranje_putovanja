namespace CrossService.Options;

public sealed class MicroserviceUrlsOptions
{
    public const string SectionName = "MicroserviceUrls";

    public string Web1 { get; set; } = string.Empty;
    public string TravelPlansApi { get; set; } = string.Empty;
    public string SharingApi { get; set; } = string.Empty;
    public string ActivitiesApi { get; set; } = string.Empty;
}
