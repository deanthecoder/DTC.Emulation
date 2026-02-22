// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using System.Globalization;
using DTC.Core.Extensions;

namespace DTC.Emulation.Rom;

/// <summary>
/// Provides consistent ROM display and file name formatting.
/// </summary>
public static class RomNameHelper
{
    private static readonly HashSet<string> LowercaseTitleWords =
    [
        "a",
        "an",
        "and",
        "as",
        "at",
        "by",
        "for",
        "in",
        "of",
        "on",
        "or",
        "the",
        "to",
        "vs"
    ];

    private static readonly string[] SlugSuffixesToStrip =
    [
        "arcadia software",
        "atari",
        "compo software",
        "firebird",
        "imageworks",
        "krisalis",
        "logotron",
        "mcm",
        "ocean software",
        "psygnosis",
        "sega",
        "us gold"
    ];

    private static readonly string[] TrailingLanguageOrRegionTokensToStrip =
    [
        "de",
        "es",
        "fr",
        "it",
        "jp",
        "uk",
        "us"
    ];

    public static string GetDisplayName(string nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(nameOrPath))
            return null;

        var fileName = Path.GetFileName(nameOrPath);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var name = Path.GetFileNameWithoutExtension(fileName);
        int i;
        while ((i = name.IndexOfAny(['(', '['])) > 0)
            name = name[..i];

        var wasSlugStyle = name.Contains('_', StringComparison.Ordinal);
        name = name.Replace('_', ' ');
        name = NormalizeSpacing(name);
        if (wasSlugStyle)
            name = StripSlugSuffixes(name);

        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return null;
        if (wasSlugStyle || ShouldTitleCasePlainName(name))
            name = ToReadableTitleCase(name);
        return name;
    }

    public static string GetSafeFileBaseName(string nameOrPath, string fallback)
    {
        var display = GetDisplayName(nameOrPath);
        var baseName = string.IsNullOrWhiteSpace(display) ? fallback : display;
        return baseName.ToSafeFileName();
    }

    public static string BuildWindowTitle(string baseTitle, string nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(baseTitle))
            baseTitle = "Emulator";

        var display = GetDisplayName(nameOrPath);
        if (string.IsNullOrWhiteSpace(display) ||
            string.Equals(display, baseTitle, StringComparison.OrdinalIgnoreCase))
            return baseTitle;

        return $"{baseTitle} - {display}";
    }

    private static string StripSlugSuffixes(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        var lowerName = name.ToLowerInvariant();
        foreach (var suffix in SlugSuffixesToStrip)
        {
            var suffixWithSeparator = $" {suffix}";
            if (!lowerName.EndsWith(suffixWithSeparator, StringComparison.Ordinal))
                continue;

            name = name[..^suffixWithSeparator.Length].TrimEnd();
            lowerName = name.ToLowerInvariant();
            break;
        }

        foreach (var token in TrailingLanguageOrRegionTokensToStrip)
        {
            var suffixWithSeparator = $" {token}";
            if (!lowerName.EndsWith(suffixWithSeparator, StringComparison.Ordinal))
                continue;

            name = name[..^suffixWithSeparator.Length].TrimEnd();
            break;
        }

        return name;
    }

    private static string NormalizeSpacing(string value)
    {
        value = value.Replace(" - ", " - ", StringComparison.Ordinal);
        while (value.Contains("  ", StringComparison.Ordinal))
            value = value.Replace("  ", " ", StringComparison.Ordinal);
        return value.Trim();
    }

    private static string ToReadableTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < tokens.Length; i++)
            tokens[i] = ToReadableToken(tokens[i], i, tokens.Length);
        return string.Join(' ', tokens);
    }

    private static string ToReadableToken(string token, int tokenIndex, int tokenCount)
    {
        if (string.IsNullOrWhiteSpace(token))
            return token;
        if (token.Contains('-', StringComparison.Ordinal))
        {
            var parts = token.Split('-', StringSplitOptions.None);
            for (var i = 0; i < parts.Length; i++)
                parts[i] = ToReadableHyphenPart(parts[i], tokenIndex, tokenCount, i, parts.Length);
            return string.Join('-', parts);
        }

        var lower = token.ToLowerInvariant();
        return lower switch
        {
            "ii" or "iii" or "iv" or "vi" or "vii" or "viii" or "ix" or "xi" or "xii" => lower.ToUpperInvariant(),
            _ when ShouldKeepLowercase(lower, tokenIndex, tokenCount) => lower,
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lower)
        };
    }

    private static string ToReadableHyphenPart(string part, int tokenIndex, int tokenCount, int partIndex, int partCount)
    {
        if (string.IsNullOrWhiteSpace(part))
            return part;

        var lower = part.ToLowerInvariant();
        if (lower is "ii" or "iii" or "iv" or "vi" or "vii" or "viii" or "ix" or "xi" or "xii")
            return lower.ToUpperInvariant();

        var isFirstWord = tokenIndex == 0 && partIndex == 0;
        var isLastWord = tokenIndex == tokenCount - 1 && partIndex == partCount - 1;
        if (!isFirstWord && !isLastWord && LowercaseTitleWords.Contains(lower))
            return lower;

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lower);
    }

    private static bool ShouldKeepLowercase(string lower, int tokenIndex, int tokenCount) =>
        tokenIndex > 0 &&
        tokenIndex < tokenCount - 1 &&
        LowercaseTitleWords.Contains(lower);

    private static bool ShouldTitleCasePlainName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Any(char.IsLetter) &&
        string.Equals(value, value.ToLowerInvariant(), StringComparison.Ordinal);
}
