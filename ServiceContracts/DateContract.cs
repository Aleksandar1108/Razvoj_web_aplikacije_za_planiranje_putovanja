using System.Globalization;

namespace ServiceContracts;

public static class DateContract
{
    public const string Format = "yyyy-MM-dd";

    public static string FromDateOnly(DateOnly value) => value.ToString(Format, CultureInfo.InvariantCulture);

    public static string? FromDateOnly(DateOnly? value) =>
        value?.ToString(Format, CultureInfo.InvariantCulture);

    public static DateOnly ToDateOnly(string value) =>
        DateOnly.ParseExact(value.Trim(), Format, CultureInfo.InvariantCulture);

    public static DateOnly? ToDateOnlyOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : ToDateOnly(value);

    public static DateOnly RequireDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Datum je obavezan.");
        return ToDateOnly(value);
    }
}
