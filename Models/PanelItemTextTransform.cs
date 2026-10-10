using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DravusSensorPanel.Models;

public static class PanelItemTextTransform {
    public const string NormalCaseStyle = "normal";
    public const string UppercaseCaseStyle = "UPPERCASE";
    public const string LowercaseCaseStyle = "lowercase";
    public const string SnakeCaseStyle = "snake_case";
    public const string CamelCaseStyle = "camelCase";
    public const string UpperCamelCaseStyle = "UpperCamelCase";

    private static readonly Regex AcronymBoundaryRegex = new("([A-Z]+)([A-Z][a-z])", RegexOptions.Compiled);
    private static readonly Regex WordBoundaryRegex = new("([a-z0-9])([A-Z])", RegexOptions.Compiled);
    private static readonly Regex SeparatorRegex = new("[^A-Za-z0-9]+", RegexOptions.Compiled);

    public static IReadOnlyList<string> CaseStyles { get; } = [
        NormalCaseStyle,
        UppercaseCaseStyle,
        LowercaseCaseStyle,
        SnakeCaseStyle,
        CamelCaseStyle,
        UpperCamelCaseStyle,
    ];

    public static string NormalizeCaseStyle(string? caseStyle) {
        if ( string.IsNullOrWhiteSpace(caseStyle) ) {
            return NormalCaseStyle;
        }

        return CaseStyles.Contains(caseStyle)
            ? caseStyle
            : NormalCaseStyle;
    }

    public static bool IsValidRegexPattern(string? regexPattern) {
        if ( string.IsNullOrWhiteSpace(regexPattern) ) {
            return true;
        }

        try {
            _ = new Regex(regexPattern);
            return true;
        }
        catch ( ArgumentException ) {
            return false;
        }
    }

    public static string Apply(string? value, string? caseStyle, string? regexPattern, string? regexReplacement) {
        string transformed = value ?? string.Empty;

        if ( !string.IsNullOrWhiteSpace(regexPattern) ) {
            try {
                transformed = Regex.Replace(transformed, regexPattern, regexReplacement ?? string.Empty);
            }
            catch ( ArgumentException ) {
                return transformed;
            }
        }

        return ApplyCaseStyle(transformed, NormalizeCaseStyle(caseStyle));
    }

    private static string ApplyCaseStyle(string value, string caseStyle) {
        return caseStyle switch {
            UppercaseCaseStyle => value.ToUpperInvariant(),
            LowercaseCaseStyle => value.ToLowerInvariant(),
            SnakeCaseStyle => string.Join("_", SplitWords(value)),
            CamelCaseStyle => ToCamelCase(value),
            UpperCamelCaseStyle => ToUpperCamelCase(value),
            _ => value,
        };
    }

    private static string ToCamelCase(string value) {
        string[] words = SplitWords(value);
        if ( words.Length == 0 ) {
            return string.Empty;
        }

        return words[0] + string.Concat(words.Skip(1).Select(Capitalize));
    }

    private static string ToUpperCamelCase(string value) {
        string[] words = SplitWords(value);
        return string.Concat(words.Select(Capitalize));
    }

    private static string[] SplitWords(string value) {
        if ( string.IsNullOrWhiteSpace(value) ) {
            return [];
        }

        string normalized = AcronymBoundaryRegex.Replace(value, "$1 $2");
        normalized = WordBoundaryRegex.Replace(normalized, "$1 $2");

        return SeparatorRegex.Split(normalized)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.ToLowerInvariant())
            .ToArray();
    }

    private static string Capitalize(string value) {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : char.ToUpperInvariant(value[0]) + value[1..];
    }
}
