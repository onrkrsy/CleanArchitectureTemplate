using System.Text.RegularExpressions;

namespace Common.Extensions;

public static class StringExtensions
{
    public static bool IsValidEmail(this string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPhoneNumber(this string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var phoneRegex = new Regex(@"^\+?[1-9]\d{1,14}$");
        return phoneRegex.IsMatch(phoneNumber.Replace(" ", "").Replace("-", ""));
    }

    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
    }

    public static string Truncate(this string input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length <= maxLength)
            return input;

        return input.Substring(0, maxLength) + "...";
    }
}