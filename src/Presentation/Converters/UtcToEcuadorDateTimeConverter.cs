using System.Globalization;
using System.Windows.Data;

namespace SistemaAranceles.Presentation.Converters;

public sealed class UtcToEcuadorDateTimeConverter : IValueConverter
{
    private static readonly TimeZoneInfo EcuadorTimeZone = ResolveEcuadorTimeZone();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;

        if (value is DateTime dateTime)
        {
            var utcDateTime = dateTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                : dateTime.ToUniversalTime();

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, EcuadorTimeZone);
        }

        if (value is DateTimeOffset dateTimeOffset)
            return TimeZoneInfo.ConvertTime(dateTimeOffset, EcuadorTimeZone).DateTime;

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    private static TimeZoneInfo ResolveEcuadorTimeZone()
    {
        try
        {
            // Windows
            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
        catch
        {
            try
            {
                // Linux/macOS
                return TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    id: "Ecuador-UTC-5",
                    baseUtcOffset: TimeSpan.FromHours(-5),
                    displayName: "Ecuador (UTC-5)",
                    standardDisplayName: "Ecuador Standard Time");
            }
        }
    }
}