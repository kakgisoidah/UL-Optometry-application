// ════════════════════════════════════════════════════════════════════════
//  Converters/Converters.cs
//  All 13 IValueConverter implementations for UL Optometry.
//  Every converter is registered as a StaticResource in App.xaml so any
//  XAML page can reference them without redeclaring.
// ════════════════════════════════════════════════════════════════════════

using System.Globalization;
using UL_Optometry.Models;

namespace UL_Optometry.Converters;


// ── 1. InvertBoolConverter ────────────────────────────────────────────────
/// <summary>true → false, false → true. Used for IsEnabled=IsNotBusy bindings.</summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

// ── 2. IntToBoolConverter ─────────────────────────────────────────────────
/// <summary>int > 0 → true, 0 → false. Used for UnreadCount badge visibility.</summary>
public class IntToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i ? i > 0 : false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 3. NullToBoolConverter ────────────────────────────────────────────────
/// <summary>null → false, non-null → true.</summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 4. InvertNullToBoolConverter ──────────────────────────────────────────
/// <summary>null → true, non-null → false. Used for empty-state visibility.</summary>
public class InvertNullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 5. BoolToThicknessConverter ───────────────────────────────────────────
/// <summary>
/// true  → Thickness(0,0,0,2)  (tab active underline)
/// false → Thickness(0)
/// </summary>
public class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? new Thickness(0, 0, 0, 2) : new Thickness(0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 6. InitialsConverter ──────────────────────────────────────────────────
/// <summary>
/// "Sipho Dlamini" → "SD"
/// "John"          → "J"
/// Used for avatar circles.
/// </summary>
public class InitialsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}"
            : char.ToUpper(parts[0][0]).ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 7. BookingStatusToColorConverter ─────────────────────────────────────
/// <summary>Maps BookingStatus enum → background Color for status badges.</summary>
public class BookingStatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources is not ResourceDictionary res)
            return Colors.Gray;

        return value?.ToString() switch
        {
            "Pending" => res["WarningLight"] as Color ?? Colors.Orange,
            "Accepted" => res["InfoLight"] as Color ?? Colors.Blue,
            "InProgress" => res["PrimaryUltraLight"] as Color ?? Colors.Blue,
            "Completed" => res["SuccessLight"] as Color ?? Colors.Green,
            "Cancelled" => res["DangerLight"] as Color ?? Colors.Red,
            _ => res["Background"] as Color ?? Colors.Gray,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 8. BookingStatusToTextColorConverter ─────────────────────────────────
/// <summary>Maps BookingStatus enum → text Color for status badges.</summary>
public class BookingStatusToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources is not ResourceDictionary res)
            return Colors.Gray;

        return value?.ToString() switch
        {
            "Pending" => res["WarningDark"] as Color ?? Colors.Orange,
            "Accepted" => res["InfoDark"] as Color ?? Colors.Blue,
            "InProgress" => res["Primary"] as Color ?? Colors.Blue,
            "Completed" => res["SuccessDark"] as Color ?? Colors.Green,
            "Cancelled" => res["DangerDark"] as Color ?? Colors.Red,
            _ => res["Muted"] as Color ?? Colors.Gray,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 9. EncounterStatusToColorConverter ───────────────────────────────────
/// <summary>Maps EncounterStatus → background Color.</summary>
public class EncounterStatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources is not ResourceDictionary res)
            return Colors.Gray;

        return value?.ToString() switch
        {
            "Draft" => res["Background"] as Color ?? Colors.LightGray,
            "Submitted" => res["InfoLight"] as Color ?? Colors.Blue,
            "UnderReview" => res["WarningLight"] as Color ?? Colors.Orange,
            "Approved" => res["SuccessLight"] as Color ?? Colors.Green,
            "Revision" => res["DangerLight"] as Color ?? Colors.Red,
            _ => res["Background"] as Color ?? Colors.Gray,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 10. EncounterStatusToTextColorConverter ───────────────────────────────
/// <summary>Maps EncounterStatus → text Color for labels.</summary>
public class EncounterStatusToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources is not ResourceDictionary res)
            return Colors.Gray;

        return value?.ToString() switch
        {
            "Draft" => res["Muted"] as Color ?? Colors.Gray,
            "Submitted" => res["InfoDark"] as Color ?? Colors.Blue,
            "UnderReview" => res["WarningDark"] as Color ?? Colors.Orange,
            "Approved" => res["SuccessDark"] as Color ?? Colors.Green,
            "Revision" => res["DangerDark"] as Color ?? Colors.Red,
            _ => res["Muted"] as Color ?? Colors.Gray,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 11. HexToColorConverter ───────────────────────────────────────────────
/// <summary>
/// "#3B82F6" → Color. Used for PoE category progress bars where colour
/// comes from the model (PoeCategory.HexColor) at runtime.
/// </summary>
public class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
                return Color.FromArgb(hex);
        }
        catch { /* fall through */ }

        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 12. DateToDisplayStringConverter ─────────────────────────────────────
/// <summary>
/// DateTime → "14 May 2025"
/// Used for all encounter and booking date labels.
/// </summary>
public class DateToDisplayStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return dt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        if (value is DateOnly d)
            return d.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── 13. CalDayBackgroundConverter ────────────────────────────────────────
/// <summary>
/// For the booking calendar SelectDatePage.
/// Parameter = "selected" or "available" or "disabled"
/// selected  → Primary colour
/// available → AccentLight colour
/// disabled  → Background colour
/// </summary>
public class CalDayBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources is not ResourceDictionary res)
            return Colors.White;

        return parameter?.ToString() switch
        {
            "selected" => res["Primary"] as Color ?? Colors.DarkBlue,
            "available" => res["AccentLight"] as Color ?? Colors.LightBlue,
            "disabled" => res["Background"] as Color ?? Colors.WhiteSmoke,
            _ => Colors.White
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── AllTrueConverter ──────────────────────────────────────────────────────
// Used in SelectDatePage MultiBindings — all inputs must be true
public class AllTrueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType,
        object parameter, CultureInfo culture)
        => values.All(v => v is bool b && b);

    public object[] ConvertBack(object value, Type[] targetTypes,
        object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── BoolToSelectedBackground ──────────────────────────────────────────────
// true  → PrimaryUltraLight tint (selected card background)
// false → Card colour (white)
public class BoolToSelectedBackground : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        bool selected = value is bool b && b;
        return selected
            ? Color.FromArgb("#EFF6FF")   // PrimaryUltraLight
            : Color.FromArgb("#FFFFFF");   // Card
    }

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── BoolToOpacity ─────────────────────────────────────────────────────────
// true (isPast) → 0.3 opacity · false → 1.0
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => value is bool b && b ? 0.3 : 1.0;

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── EqualsToPrimaryConverter ──────────────────────────────────────────────
// Binding Path=BookingType, ConverterParameter=WalkIn
// → Primary colour if equal, Border colour if not
public class EqualsToPrimaryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        bool equal = value?.ToString() == parameter?.ToString();
        // Return a Color — caller must bind to Stroke
        return equal
            ? Color.FromArgb("#1E3A8A")   // Primary
            : Color.FromArgb("#E2E8F0");   // Border
    }
    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── EqualsToBackgroundConverter ───────────────────────────────────────────
// Selected booking type card gets light blue tint background
public class EqualsToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        bool equal = value?.ToString() == parameter?.ToString();
        return equal
            ? Color.FromArgb("#EFF6FF")   // PrimaryUltraLight
            : Color.FromArgb("#FFFFFF");   // Card
    }
    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── BoolToCreateWalkInLabel ───────────────────────────────────────────────
// ShowCreatePatient true  → "↑ Hide Walk-In Form"
//                  false → "+ Create Walk-In Patient"
public class BoolToCreateWalkInLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => value is bool b && b
            ? "↑ Hide Walk-In Form"
            : "+ Create Walk-In Patient";

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── BoolToSelectedBorderColor ─────────────────────────────────────────────
// true → Primary  ·  false → Border (grey)
public class BoolToSelectedBorderColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => value is bool b && b
            ? Color.FromArgb("#1E3A8A")   // Primary
            : Color.FromArgb("#E2E8F0");   // Border

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}