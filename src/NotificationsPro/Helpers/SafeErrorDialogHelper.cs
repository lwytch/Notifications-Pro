namespace NotificationsPro.Helpers;

internal static class SafeErrorDialogHelper
{
    public static string BuildErrorMessage(string summary, Exception? exception = null, string? nextStep = null)
    {
        var sections = new List<string>
        {
            NormalizeSentence(summary)
        };

        var safeLabel = GetSafeExceptionLabel(exception);
        if (!string.IsNullOrWhiteSpace(safeLabel))
            sections.Add($"Details: {safeLabel}.");

        if (!string.IsNullOrWhiteSpace(nextStep))
            sections.Add(NormalizeSentence(nextStep));

        return string.Join("\n\n", sections);
    }

    internal static string GetSafeExceptionLabel(Exception? exception)
    {
        if (exception == null)
            return string.Empty;

        var typeName = exception.GetBaseException().GetType().Name.Trim();
        return string.IsNullOrWhiteSpace(typeName) ? "UnexpectedError" : typeName;
    }

    private static string NormalizeSentence(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return "Unexpected error.";

        return trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?')
            ? trimmed
            : $"{trimmed}.";
    }
}
